using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextUIBlink : MonoBehaviour
{
    private TextMeshProUGUI uiText;

    [SerializeField] private float blinkSpeed = 1f;

    private Coroutine blinkCoroutine;
    private float direction = -1f;

    private void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        blinkCoroutine = StartCoroutine(Blink());
    }

    private void OnDisable()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }

    private IEnumerator Blink()
    {
        while (true)
        {
            Color color = uiText.color;

            color.a += direction * blinkSpeed * Time.unscaledDeltaTime;

            if (color.a <= 0f)
            {
                color.a = 0f;
                direction = 1f;
            }
            else if (color.a >= 1f)
            {
                color.a = 1f;
                direction = -1f;
            }

            uiText.color = color;

            yield return null;
        }
    }
}