using System;
using System.Collections;
using System.Collections.Generic;
using GameEvents;
using Player;
using SLTypes;
using UnityEngine;
using UnityServiceLocator;
using Opsive.UltimateCharacterController.Traits;
using Attribute = Opsive.UltimateCharacterController.Traits.Attribute;

public class PlayerUnderWaterBehaviour : MonoBehaviour
{
    [SerializeField] private float maxUnderwaterTime = 10f;
    [SerializeField] private float damageTimeInterval = 1.2f;
    [SerializeField] private int damagePerInterval = 5;
    private float timeUnderWater = 0;
    private float timeSinceLastInterval = 0;
    
    private IPlayer player;
    
    private float waterSurfaceY;

    private bool playerUnderWater;
    private bool breathDangerZone;
    
    [SerializeField] private AttributeManager attributeManager;
    private Attribute breathAttribute;

    struct Breath
    {
        public float Value;
        public long Timestamp;
        public Breath(float v, long t)
        {
            Value = v;
            Timestamp = t;
        }
    }

    private const int _maxListCapacity = 4;
    private List<Breath> previousBreaths = new List<Breath>(_maxListCapacity);
    private int prevBreathIdx = 0;

    private float healthDamageBreathless = -1f;

    private void Awake()
    {
        if (attributeManager == null)
            attributeManager = GetComponent<AttributeManager>();
        
        breathAttribute = attributeManager.GetAttribute("Breath");

        if (breathAttribute == null)
        {
            Debug.LogError("Could not find Breath attribute on AttributeManager.");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ServiceLocator.ForSceneOf(this).Get(out player);
        
        
        // TODO: could be made more performant without this calculation, but is one time on start
        int count = 0;
        float totalY = 0;
        foreach (GameObject waterSurface in WaterSurfaceRegistry.WaterSurfaces)
        {
            // use waterSurface
            totalY += waterSurface.transform.position.y;
            count++;
        }

        waterSurfaceY = totalY / count;
    }
    
    public void SetBreath_UCC(float value)
    {
        if (breathAttribute == null)
            return;

        breathAttribute.Value = value;
    }
    
    public float GetBreath_UCC()
    {
        if (breathAttribute == null)
            return 0f;

        return breathAttribute.Value;
    }

    public void AddBreathSample(float breath)
    {
        if (breath <= 0)
            return;
        
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Breath b = new Breath(breath, timestamp);
        
        if (previousBreaths.Count >= _maxListCapacity)
        {
            previousBreaths[prevBreathIdx] = b;  // recycle oldest entry
            prevBreathIdx = (prevBreathIdx + 1) % _maxListCapacity;
        }
        else
        {
            previousBreaths.Add(b);  // add new ones until full capacity
        }
    }

    public float GetDecayPerSecond()
    {
        if (previousBreaths.Count < 2)
        {
            return 0f;
        }

        Breath oldest = previousBreaths[0];
        Breath newest = previousBreaths[previousBreaths.Count - 1];

        double elapsedTime = (newest.Timestamp - oldest.Timestamp) / 1000.0;  // convert from milliseconds to seconds

        if (elapsedTime <= 0)
        {
            return 0f;
        }

        float valueLost = oldest.Value - newest.Value;

        return valueLost / (float)elapsedTime;
    }

    // Update is called once per frame
    void Update()
    {
        float breath = GetBreath_UCC();
        AddBreathSample(breath);
        
        UIManager.Instance.DisplayAirVolume(breath / 100f);
        
        Camera activeCam = OcclusionCameraController.Instance.GetActiveCamera();

        if (activeCam != null && activeCam.transform.position.y <= waterSurfaceY + 0.15f)  // Camera under water
            GameEventManager.Raise(new CameraUnderWaterEvent(true));  // activate Under-Water-Effect only when Camera is under water
        else
            GameEventManager.Raise(new CameraUnderWaterEvent(false));

        if (breath > 0 && breath < 33f)
        {
            if (!breathDangerZone)
            {
                // in danger zone of breath-volume
                breathDangerZone = true;
                ApplyWaterDamage();
            }
        }
        else if(breath == 0 && breathDangerZone)
            ApplyWaterDamage(true);
        else
            breathDangerZone = false;
        
        /*SendEventIfNecessary();  // send PlayerSwimEvent depending on playerUnderWater(bool)
            
        float airVolumePercentage = (maxUnderwaterTime - timeUnderWater) / maxUnderwaterTime;
        float airVolumePercentageClamped = Mathf.Clamp(airVolumePercentage, 0f, 1f);
        
        SetUCCAir(airVolumePercentageClamped * 100f);  // [0;100]
        
        // don't send display-updates when the display already deactivated itself (>=1f)
        if(airVolumePercentage < 1.5f)
            UIManager.Instance.DisplayAirVolume(airVolumePercentageClamped);
        
        float playerHeadY = player.Position.y + player.Height / 2f;
        //Debug.Log($"playerHeadY: {playerHeadY}, waterSurfaceY: {waterSurfaceY}");
        if (playerHeadY > waterSurfaceY)
        {
            playerUnderWater = false;
            if(timeUnderWater > maxUnderwaterTime)
                timeUnderWater = maxUnderwaterTime;  // immediately breath again when coming out
            else if(timeUnderWater > 0)
                timeUnderWater -= Time.deltaTime;  // fill air back up when getting out of water
            else
                timeUnderWater = 0;
            
            timeSinceLastInterval = damageTimeInterval;  // initialize - for getting damage immediately after maxUnderwaterTime
            return;
        }

        // head below water surface
        playerUnderWater = true;
        timeUnderWater += Time.deltaTime;

        if (timeUnderWater > maxUnderwaterTime)  // too long underwater?
        {
            timeSinceLastInterval += Time.deltaTime;
            if (timeSinceLastInterval > damageTimeInterval)  // damage interval reached
            {
                timeSinceLastInterval = 0;
                GameEventManager.Raise(new PlayerDamageEvent(damagePerInterval, PlayerDamageEvent.DamagedBy.Water));  // player gets damage
            }
        }*/
    }

    private float GetEstimatedTimeLeftUntilDeath()
    {
        float breath = GetBreath_UCC();
        float decay = GetDecayPerSecond();
        if (decay <= 0) decay = 1;
        
        return breath / decay;
    }

    private void ApplyWaterDamage(bool kill = false)
    {
        if (!breathDangerZone)
            return;

        float delay = 0.25f;  // seconds
        float timeLeft = GetEstimatedTimeLeftUntilDeath();

        float healthDamage = player.Health / (timeLeft / delay);
        //Debug.Log("playerHealth / (timeLeft / delay)");
        //Debug.Log($"{player.Health} / ({timeLeft} / {delay}) = {healthDamage}");

        if (kill)
        {
            GameEventManager.Raise(new PlayerDamageEvent(player.Health, PlayerDamageEvent.DamagedBy.Water));
            breathDangerZone = false;
            return;
        }

        GameEventManager.Raise(new PlayerDamageEvent((int)healthDamage, PlayerDamageEvent.DamagedBy.Water));
        
        StartCoroutine(DamageCooldown(delay));  // delay between damage
    }

    private IEnumerator DamageCooldown(float delay)
    {
        yield return new WaitForSeconds(delay);
        ApplyWaterDamage();
    }

    private bool _tempPrevState;
    private void SendEventIfNecessary()
    {
        if (playerUnderWater && !_tempPrevState)
        {
            // player has gone into water
            GameEventManager.Raise(new PlayerSwimEvent(true));
        } else if (!playerUnderWater && _tempPrevState)
        {
            // player is emerging from water
            GameEventManager.Raise(new PlayerSwimEvent(false));
        }
    }
}
