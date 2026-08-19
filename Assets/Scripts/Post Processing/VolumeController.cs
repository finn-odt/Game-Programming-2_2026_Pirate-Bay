using System;
using System.Collections;
using System.Collections.Generic;
using GameEvents;
using UnityEngine;
using UnityEngine.Rendering;

public class VolumeController : MonoBehaviour
{
    
    public static VolumeController Instance { get; private set; }

    private void Awake()
    {
        // Singleton-Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void Start()
    {
        if(waterEffectVolume)
            waterEffectVolume.enabled = false;
        if(gameOverEffectVolume)
            gameOverEffectVolume.enabled = false;
    }

    private void OnEnable()
    {
        GameEventManager.AddListener<GameOverEvent>(OnGameOver);
        GameEventManager.AddListener<CameraUnderWaterEvent>(OnWaterMode);
    }

    private void OnDisable()
    {
        GameEventManager.RemoveListener<GameOverEvent>(OnGameOver);
        GameEventManager.RemoveListener<CameraUnderWaterEvent>(OnWaterMode);
    }

    private void OnGameOver(GameOverEvent e)
    {
        DisableEffect(waterEffectVolume);
        
        EnableEffect(gameOverEffectVolume);
        StartCoroutine(FadeInVolume(gameOverEffectVolume, 1));
    }

    private IEnumerator FadeInVolume(Volume effect, float duration)
    {
        effect.weight = 0f;
        while (effect.weight < 1f)
        {
            effect.weight += Time.deltaTime / duration;
            yield return null;
        }
    }

    private void OnWaterMode(CameraUnderWaterEvent e)
    {
        if (gameOverEffectVolume.enabled)
            return;
        
        if(e.isUnderWater)
            EnableEffect(waterEffectVolume);
        else
            DisableEffect(waterEffectVolume);
    }

    [SerializeField] public Volume waterEffectVolume;
    [SerializeField] public Volume gameOverEffectVolume;
    
    public void EnableEffect(Volume effect)
    {
        effect.enabled = true;
    }
    
    public void DisableEffect(Volume effect)
    {
        effect.enabled = false;
    }
}
