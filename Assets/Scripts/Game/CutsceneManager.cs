using System.Collections;
using Systems.SceneManagement;
using TMPro;
using TriInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class CutsceneManager : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector director;

    [Header("Scene Loading")]
    [SceneGroup]
    [SerializeField] private int nextSceneGroupIndex = 2;

    [Header("UI")] 
    [SerializeField] private CanvasGroup skipText;
    [SerializeField, Unit("sec")] private float timeOfFullVisibilityForSkipText = 4f;
    [SerializeField, Unit("sec")] private float fadeInTime = 1.75f;
    [SerializeField, Range(0f, 1f)] private float restVisibilityWhenFadedOut = 0.2f;
    private float timeOfVisibilityStart = -1f;
    private bool fadedOut = false;
    
    private bool hasFinished;
    private bool isLoading;

    private void Awake()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();

        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;
    }

    private void OnEnable()
    {
        if (director != null)
            director.stopped += OnDirectorStopped;
    }

    private void Start()
    {
        director.time = 0;
        director.Play();
        
        skipText.alpha = 0f;
        StartCoroutine(FadeSkipText(1, 1));  // fade in
    }

    private IEnumerator FadeSkipText(int sign, float percentage)
    {
        while (sign > 0 ? skipText.alpha < percentage * 1f : skipText.alpha > (1f - percentage))
        {
            skipText.alpha += (1 / fadeInTime) * Time.deltaTime * sign;
            yield return new WaitForEndOfFrame();
        }

        if(sign > 0)
            timeOfVisibilityStart = Time.time;  // start full visibility timer
    }

    private void Update()
    {
        if (hasFinished)
        {
            LoadNextScene();
            return;
        }

        if (timeOfVisibilityStart > 0 && (Time.time - timeOfVisibilityStart) > timeOfFullVisibilityForSkipText && !fadedOut)
        {
            fadedOut = true;
            StartCoroutine(FadeSkipText(-1, (1 - restVisibilityWhenFadedOut)));  // fade out to 20% visibility
        }

        if (Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            skipText.alpha = 1f;  // show in full
            LoadNextScene();
        }
    }

    private void OnDisable()
    {
        if (director != null)
            director.stopped -= OnDirectorStopped;
    }

    private void OnDirectorStopped(PlayableDirector stoppedDirector)
    {
        if (stoppedDirector != director || hasFinished)
            return;

        hasFinished = true;
    }

    private void LoadNextScene()
    {
        if (isLoading)
            return;

        isLoading = true;
        SceneLoader.Instance.LoadSceneGroup(nextSceneGroupIndex);
    }
}