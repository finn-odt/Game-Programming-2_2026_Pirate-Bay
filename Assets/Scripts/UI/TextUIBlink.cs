using System;
using System.Collections;
using System.Collections.Generic;
using GameEvents;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextUIBlink : MonoBehaviour
{
    private TextMeshProUGUI uiText;

    [SerializeField] private float intervalLength = 1f;
    private float timeSinceLastBlink = 0f;
    private int sign = 1;
    
    Coroutine blinkCoroutine;

    private void Start()
    {
        uiText = GetComponent<TextMeshProUGUI>();

        StartCoroutine(Blink());
    }

    private void OnDestroy()
    {
        if(blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);
    }

    private IEnumerator Blink()
    {
        while (blinkCoroutine != null)
        {
            Color c = uiText.color;
            c.a += sign * 0.1f;

            if (c.a >= 1 || c.a <= 0)
                sign *= -1;

            uiText.color = c;
            yield return null;
        }
    }
}
