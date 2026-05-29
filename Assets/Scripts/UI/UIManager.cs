using System.Collections;
using GameEvents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI coins;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image airVolumeFull, airVolumeEmpty; // has parent that can be (de)activated for showing

    [SerializeField] private GameObject interactionIndicator;

    [SerializeField] private Canvas gameCanvas, pauseCanvas;

    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Slider mouseXSensitivitySlider, mouseYSensitivitySlider,
        gamepadXSensitivitySlider, gamepadYSensitivitySlider;
    
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

    void Start()
    {
        coins.text = "0";
        mouseXSensitivitySlider.value = 1.8f;
        mouseYSensitivitySlider.value = 0.85f;
        gamepadXSensitivitySlider.value = 0.5f;
        gamepadYSensitivitySlider.value = 0.3f;
        interactionIndicator.SetActive(false);
        
        //Add listener for when the value of the Dropdown changes
        difficultyDropdown.onValueChanged.AddListener(delegate {
            OnDifficultyDropdownChange(difficultyDropdown);
        });
        //Add listener for mouse sensitivity slider changes
        mouseXSensitivitySlider.onValueChanged.AddListener(delegate {
            OnSensitivityChange(mouseXSensitivitySlider, mouseYSensitivitySlider, false);
        });
        mouseYSensitivitySlider.onValueChanged.AddListener(delegate {
            OnSensitivityChange(mouseXSensitivitySlider, mouseYSensitivitySlider, false);
        });
        //Add listener for gamepad sensitivity slider changes
        gamepadXSensitivitySlider.onValueChanged.AddListener(_ =>
        {
            OnSensitivityChange(gamepadXSensitivitySlider, gamepadYSensitivitySlider, true);
        });
        gamepadYSensitivitySlider.onValueChanged.AddListener(_ =>
        {
            OnSensitivityChange(gamepadXSensitivitySlider, gamepadYSensitivitySlider, true);
        });
    }

    void Update()
    {
        DisplayCoinAmount();
        DisplayHealth();
    }
    
    void OnEnable()
    {
        GameEventManager.AddListener<InteractionPossibleEvent>(OnInteractionEnter);
        GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
    }

    void OnDisable()
    {
        GameEventManager.RemoveListener<InteractionPossibleEvent>(OnInteractionEnter);
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
    }

    private void OnGameStateChange(GameStateChangedEvent e)
    {
        if (e.newState == GameStateChangedEvent.GameState.Play)
        {
            gameCanvas.gameObject.SetActive(true);
            pauseCanvas.gameObject.SetActive(false);
        } else if (e.newState == GameStateChangedEvent.GameState.Paused)
        {
            gameCanvas.gameObject.SetActive(false);
            pauseCanvas.gameObject.SetActive(true);
            // close conversation if necessary
            GameEventManager.Raise(new ConversationUIEvent("", false));
        } else if (e.newState == GameStateChangedEvent.GameState.Inventory)
        {
            gameCanvas.gameObject.SetActive(true);
            // close conversation if necessary
            GameEventManager.Raise(new ConversationUIEvent("", false));
            pauseCanvas.gameObject.SetActive(false);
        }
    }
    
    private void DisplayCoinAmount()
    {
        coins.text = collectedCoins.RuntimeValue.ToString();
    }
    
    private void DisplayHealth()
    {
        healthSlider.value = healthPoints.RuntimeValue / 100f;  // percentage
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
            airVolumeFull.transform.parent.gameObject.SetActive(false);
        }
        else if (percentage <= 0)  // EMPTY: blink red-white
        {
            Debug.Log(timeSinceAirEmpty);
            timeSinceAirEmpty += Time.deltaTime;
            _lastAirVolumeBlink += Time.deltaTime;
            airVolumeFull.transform.parent.gameObject.SetActive(true);
            airVolumeFull.fillAmount = 0;
            airVolumeEmpty.fillAmount = 1;

            // let it blink
            int millisecs = (int)(_lastAirVolumeBlink * 1000);
            Debug.Log(millisecs);
            if (millisecs >= 300)
            {
                if(airVolumeEmpty.color == Color.white)
                    airVolumeEmpty.color = Color.red;
                else
                    airVolumeEmpty.color = Color.white;
                _lastAirVolumeBlink = 0;
            }
        }
        else  // Show Progress
        {
            timeSinceAirEmpty = 0;
            airVolumeEmpty.color = Color.white;
            // draw bubbles with percentage
            airVolumeFull.transform.parent.gameObject.SetActive(true);
            airVolumeFull.fillAmount = percentage;
            airVolumeEmpty.fillAmount = 1 - percentage;
        }
    }
    
    private Vector2 GetScreenCoordinatesOfPlayer(Vector3 position, Vector3 offset)
    {
        Vector3 worldPos = position + new Vector3(0, 0.5f, 0);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = gameCanvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            gameCanvas.worldCamera,
            out Vector2 localPos
        );

        return localPos;
    }

    private void OnInteractionEnter(InteractionPossibleEvent e)
    {
        if(e.interactionPossible)
        {
            // place indicator over interactable object
            interactionIndicator.transform.localPosition = GetScreenCoordinatesOfPlayer(e.interactable.transform.position, new Vector3(0, 0, 0));
            // activate indicator
            interactionIndicator.SetActive(true);
        } else
        {
            // deactivate indicator
            interactionIndicator.SetActive(false);
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
}
