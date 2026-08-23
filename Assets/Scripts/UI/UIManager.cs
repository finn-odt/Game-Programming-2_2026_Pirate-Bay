using System;
using System.Collections;
using GameEvents;
using Opsive.UltimateCharacterController.Traits;
using Systems.SceneManagement;
using Attribute = Opsive.UltimateCharacterController.Traits.Attribute;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Serializable]
    public struct HealthSlider
    {
        public GameObject parent;
        public Image slider;
    }

    [Serializable]
    public struct GameCanvas
    {
        public Canvas canvas;
        public TextMeshProUGUI coins;
        public HealthSlider healthSlider;
        public Image airVolumeFull;  // has parent that can be (de)activated for showing
        public Image airVolumeEmpty;  // has parent that can be (de)activated for showing
        public GameObject interactionIndicator;
    }

    [Serializable]
    public struct MenuCanvas
    {
        public Canvas canvas;
        public TMP_Dropdown difficultyDropdown;
        //public Slider mouseXSensitivitySlider;
        //public Slider mouseYSensitivitySlider;
        //public Slider gamepadXSensitivitySlider;
        //public Slider gamepadYSensitivitySlider;
    }

    [Serializable]
    public struct GameOverCanvas
    {
        public Canvas canvas;
        public TextMeshProUGUI killedByFillIn;
    }

    [Serializable]
    public struct WinningCanvas
    {
        public Canvas canvas;
    }

    public GameCanvas gameCanvas;
    public MenuCanvas menuCanvas;
    public GameOverCanvas gameOverCanvas;
    public WinningCanvas winningCanvas;

    private float healthTargetFillAmount = 1f, healthFillUpdateStep = 0.8f;
    private float healthFillAmount => gameCanvas.healthSlider.slider.fillAmount;
    
    [Header("Scriptable Objects")]
    // Scriptable Object for Player
    public IntegerSO collectedCoins;
    public IntegerSO healthPoints;
    
    private void Awake()
    {
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

    void Start()
    {
        gameCanvas.coins.text = "0";
        //menuCanvas.mouseXSensitivitySlider.value = 1.8f;
        //menuCanvas.mouseYSensitivitySlider.value = 0.85f;
        //menuCanvas.gamepadXSensitivitySlider.value = 0.5f;
        //menuCanvas.gamepadYSensitivitySlider.value = 0.3f;
        gameCanvas.interactionIndicator.SetActive(false);
        
        //Add listener for when the value of the Dropdown changes
        menuCanvas.difficultyDropdown.onValueChanged.AddListener(delegate {
            OnDifficultyDropdownChange(menuCanvas.difficultyDropdown);
        });
        /*
        //Add listener for mouse sensitivity slider changes
        menuCanvas.mouseXSensitivitySlider.onValueChanged.AddListener(delegate {
            OnSensitivityChange(menuCanvas.mouseXSensitivitySlider, menuCanvas.mouseYSensitivitySlider, false);
        });
        menuCanvas.mouseYSensitivitySlider.onValueChanged.AddListener(delegate {
            OnSensitivityChange(menuCanvas.mouseXSensitivitySlider, menuCanvas.mouseYSensitivitySlider, false);
        });
        //Add listener for gamepad sensitivity slider changes
        menuCanvas.gamepadXSensitivitySlider.onValueChanged.AddListener(_ =>
        {
            OnSensitivityChange(menuCanvas.gamepadXSensitivitySlider, menuCanvas.gamepadYSensitivitySlider, true);
        });
        menuCanvas.gamepadYSensitivitySlider.onValueChanged.AddListener(_ =>
        {
            OnSensitivityChange(menuCanvas.gamepadXSensitivitySlider, menuCanvas.gamepadYSensitivitySlider, true);
        })
        */
    }

    void Update()
    {
        DisplayCoinAmount();
        UpdateHealthFill();
    }
    
    void OnEnable()
    {
        GameEventManager.AddListener<InteractionPossibleEvent>(OnInteractionEnter);
        GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
        GameEventManager.AddListener<GameOverEvent>(OnGameOver);
    }

    void OnDisable()
    {
        GameEventManager.RemoveListener<InteractionPossibleEvent>(OnInteractionEnter);
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
        GameEventManager.RemoveListener<GameOverEvent>(OnGameOver);
    }

    private void OnGameOver(GameOverEvent e)
    {
        string killer = "";
        switch (e.killer)
        {
            case GameOverEvent.Killer.Unknown:
                killer = "Unknown";
                break;
            case GameOverEvent.Killer.Explosion:
                killer = "an Explosion";
                break;
            case GameOverEvent.Killer.Falling:
                killer = "falling";
                break;
            case GameOverEvent.Killer.Drowned:
                killer = "Drowning";
                break;
            case GameOverEvent.Killer.Npc:
                killer = "an Enemy";
                break;
            case GameOverEvent.Killer.Shark:
                killer = "a Shark";
                break;
            case GameOverEvent.Killer.Zombie:
                killer = "a Zombie";
                break;
        }
        gameOverCanvas.killedByFillIn.text = killer;
    }

    private void OnGameStateChange(GameStateChangedEvent e)
    {
        if (e.newState == GameStateChangedEvent.GameState.Play)
        {
            gameOverCanvas.canvas.gameObject.SetActive(false);
            menuCanvas.canvas.gameObject.SetActive(false);
            winningCanvas.canvas.gameObject.SetActive(false);
            
            gameCanvas.canvas.gameObject.SetActive(true);
        }
        else if (e.newState == GameStateChangedEvent.GameState.Paused)
        {
            gameOverCanvas.canvas.gameObject.SetActive(false);
            gameCanvas.canvas.gameObject.SetActive(false);
            winningCanvas.canvas.gameObject.SetActive(false);
            // close conversation if necessary
            GameEventManager.Raise(new ConversationUIEvent("", false));
            
            menuCanvas.canvas.gameObject.SetActive(true);
        }
        else if (e.newState == GameStateChangedEvent.GameState.Inventory)
        {
            gameOverCanvas.canvas.gameObject.SetActive(false);
            menuCanvas.canvas.gameObject.SetActive(false);
            winningCanvas.canvas.gameObject.SetActive(false);
            // close conversation if necessary
            GameEventManager.Raise(new ConversationUIEvent("", false));
            
            gameCanvas.canvas.gameObject.SetActive(true);
        }
        else if(e.newState == GameStateChangedEvent.GameState.Lost)
        {
            gameCanvas.canvas.gameObject.SetActive(false);
            menuCanvas.canvas.gameObject.SetActive(false);
            winningCanvas.canvas.gameObject.SetActive(false);
            // close conversation if necessary
            GameEventManager.Raise(new ConversationUIEvent("", false));
            
            gameOverCanvas.canvas.gameObject.SetActive(true);
        }
        else if(e.newState == GameStateChangedEvent.GameState.Won)
        {
            gameCanvas.canvas.gameObject.SetActive(false);
            menuCanvas.canvas.gameObject.SetActive(false);
            gameOverCanvas.canvas.gameObject.SetActive(false);
            // close conversation if necessary
            GameEventManager.Raise(new ConversationUIEvent("", false));
            
            winningCanvas.canvas.gameObject.SetActive(true);
        }
    }
    
    private void DisplayCoinAmount()
    {
        gameCanvas.coins.text = collectedCoins.RuntimeValue.ToString();
    }

    private void UpdateHealthFill()
    {
        if (healthTargetFillAmount != healthFillAmount)
        {
            int dif = Math.Sign(healthTargetFillAmount - healthFillAmount);
            
            float potentialResult = healthFillAmount + dif * healthFillUpdateStep * Time.deltaTime;
            int dif2 = Math.Sign(healthTargetFillAmount - potentialResult);
            
            // if the sign swapped (we would overshoot the target) -> stop action
            if (dif != dif2)
                gameCanvas.healthSlider.slider.fillAmount = healthTargetFillAmount;
            else
                gameCanvas.healthSlider.slider.fillAmount = potentialResult;
        }
    }
    
    public void DisplayHealth(float healthPercentage)
    {
        if(healthPercentage > 1f)
            healthPercentage /= 100f;

        if (healthPercentage > 1f)
            return;
        
        healthTargetFillAmount = healthPercentage;
    }

    private float timeSinceAirEmpty = 0;
    private float _lastAirVolumeBlink = 0;
    public void DisplayAirVolume(float percentage)
    {
        // called by Update() -> Time.deltaTime can be used
        if (percentage >= 1f)  // FULL: no show
        {
            timeSinceAirEmpty = 0;
            // do not draw, as air is full [no UI clutter]
            gameCanvas.airVolumeFull.transform.parent.gameObject.SetActive(false);
        }
        else if (percentage <= 0)  // EMPTY: blink red-white
        {
            timeSinceAirEmpty += Time.deltaTime;
            _lastAirVolumeBlink += Time.deltaTime;
            gameCanvas.airVolumeFull.transform.parent.gameObject.SetActive(true);
            gameCanvas.airVolumeFull.fillAmount = 0;
            gameCanvas.airVolumeEmpty.fillAmount = 1;

            // let it blink
            int millisecs = (int)(_lastAirVolumeBlink * 1000);
            if (millisecs >= 300)
            {
                if(gameCanvas.airVolumeEmpty.color == Color.white)
                    gameCanvas.airVolumeEmpty.color = Color.red;
                else
                    gameCanvas.airVolumeEmpty.color = Color.white;
                _lastAirVolumeBlink = 0;
            }
        }
        else  // Show Progress
        {
            timeSinceAirEmpty = 0;
            gameCanvas.airVolumeEmpty.color = Color.white;
            // draw bubbles with percentage
            gameCanvas.airVolumeFull.transform.parent.gameObject.SetActive(true);
            gameCanvas.airVolumeFull.fillAmount = percentage;
            gameCanvas.airVolumeEmpty.fillAmount = 1 - percentage;
        }
    }
    
    public Vector2 GetScreenCoordinatesOfObject(Vector3 position, Vector3 offset)
    {
        Vector3 worldPos = position + new Vector3(0, 0.5f, 0);
        Vector3 screenPos = OcclusionCameraController.Instance.GameplayCamera.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = gameCanvas.canvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            gameCanvas.canvas.worldCamera,
            out Vector2 localPos
        );

        return localPos;
    }

    private void OnInteractionEnter(InteractionPossibleEvent e)
    {
        Debug.Log($"Interaction Indicator Changed: {e.interactionPossible}");
        if(e.interactionPossible)
        {
            // place indicator over interactable object
            gameCanvas.interactionIndicator.transform.localPosition = GetScreenCoordinatesOfObject(e.interactable.transform.position, new Vector3(0, 0, 0));
            // activate indicator
            gameCanvas.interactionIndicator.SetActive(true);
        } else
        {
            // deactivate indicator
            gameCanvas.interactionIndicator.SetActive(false);
        }
    }
    
    public void OnDifficultyDropdownChange(TMP_Dropdown change)
    {
        GameDifficulty dif = GameDifficulty.Easy;  // default: easy
        
        // compute enum difficulty from string
        switch (change.value)
        {
            case 1:  // NORMAL
                dif = GameDifficulty.Normal;
                break;
            case 2:  // HARD
                dif = GameDifficulty.Hard;
                break;
        }
        
        GameEventManager.Raise(new GameDifficultyChangedEvent(dif));
    }
    
    private void OnSensitivityChange(Slider sliderX, Slider sliderY, bool mouseOrGamepad)
    {
        GameEventManager.Raise(new SensitivityChangeEvent(mouseOrGamepad, sliderX.value, sliderY.value));
    }
    
    public void BackToMainMenu()
    {
        SceneLoader.Instance.LoadSceneGroup(0);  // title screen has ID=0
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
