using GameEvents;
using Player;
using SLTypes;
using UnityEngine;
using UnityServiceLocator;

public class PlayerUnderWaterBehaviour : MonoBehaviour
{
    [SerializeField] private float maxUnderwaterTime = 10f;
    [SerializeField] private float damageTimeInterval = 1.2f;
    [SerializeField] private int damagePerInterval = 5;
    private float timeUnderWater = 0;
    private float timeSinceLastInterval = 0;
    
    private IPlayer player;
    
    private float waterSurfaceY;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ServiceLocator.Global.Get(out player);
        
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

    // Update is called once per frame
    void Update()
    {
        UIManager.Instance.DisplayAirVolume(Mathf.Clamp((maxUnderwaterTime - timeUnderWater) / maxUnderwaterTime, 0f, 1f));
        
        float playerHeadY = player.Position.y + player.Height / 2f;
        //Debug.Log($"playerHeadY: {playerHeadY}, waterSurfaceY: {waterSurfaceY}");
        if (playerHeadY > waterSurfaceY)
        {
            if(timeUnderWater > maxUnderwaterTime)
                timeUnderWater = maxUnderwaterTime;  // immediately breath again when coming out
            else if(timeUnderWater > 0)
                timeUnderWater -= Time.deltaTime;  // fill air back up when getting out of water
            else
                timeUnderWater = 0;
            
            timeSinceLastInterval = damageTimeInterval;  // for getting damage immediately after maxUnderwaterTime
            return;
        }

        // head below water surface
        timeUnderWater += Time.deltaTime;

        if (timeUnderWater > maxUnderwaterTime)  // too long underwater?
        {
            timeSinceLastInterval += Time.deltaTime;
            if (timeSinceLastInterval > damageTimeInterval)  // damage interval reached
            {
                timeSinceLastInterval = 0;
                GameEventManager.Raise(new PlayerDamageEvent(damagePerInterval, PlayerDamageEvent.DamagedBy.Water));  // player gets damage
            }
        }
    }
}
