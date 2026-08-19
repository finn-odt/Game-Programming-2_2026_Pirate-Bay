using System.Collections;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.AddOns.Climbing;
using GameEvents;
using Opsive.UltimateCharacterController.AddOns.Swimming;
using Opsive.UltimateCharacterController.Traits;
using Player;
using SLTypes;
using TriInspector;
using UnityEngine;
using UnityEngine.InputSystem.Users;
using UnityServiceLocator;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

#if ENABLE_INPUT_SYSTEM 
[RequireComponent(typeof(PlayerInput))]
#endif
public class PlayerInputController : MonoBehaviour
{
    private IPlayer player;
    
    [Header("Look Sensitivity (Mouse)")]
    [SerializeField, Range(0f, 3f)] private float mouseSensitivityX = 1.8f;
    [SerializeField, Range(0f, 3f)] private float mouseSensitivityY = 0.85f;

    [Header("Look Sensitivity (Gamepad)")]
    [SerializeField, Range(0f, 3f)] private float gamepadSensitivityX = 0.5f;
    [SerializeField, Range(0f, 3f)] private float gamepadSensitivityY = 0.3f;
    
    //[Header("DEBUG")]
    //[SerializeField, LabelText("Fly Mode ?")] private bool dFlyMode;
    //[SerializeField, LabelText("Fly Speed:")] private float dFlySpeed;
    //private readonly InputAction dToggleFlyingMode = new("Toggle Flying Mode", InputActionType.Button);
    //private bool dIsFlying = false;

    private bool berserkModeActive = false;
    private float berserkFactor;

    private bool attackMode;
    
    // animation IDs
    private int _animIDAttack;

#if ENABLE_INPUT_SYSTEM 
    private PlayerInput _playerInput;
#endif
    private Animator _animator;
    private CharacterController _controller;
    private PlayerInputHandler _input;
    
    private bool _haveIControlOverPlayer = false;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
			return false;
#endif
        }
    }

    private void OnInputUserChange(InputUser user, InputUserChange change, InputDevice device)
    {
        if (change != InputUserChange.ControlSchemeChanged)
            return;

        Debug.Log($"Current input control scheme: {_playerInput.currentControlScheme}");

        GameEventManager.Raise(new BroadcastInputControlSchemeEvent(_playerInput.currentControlScheme));
        //_playerInput.currentControlScheme == "Gamepad"
        //_playerInput.currentControlScheme == "Keyboard&Mouse"
    }
    
    void OnEnable()
    {
        GameEventManager.AddListener<SensitivityChangeEvent>(OnSensitivityChange);
        
        GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
        
        GameEventManager.AddListener<PlayerBerserkEvent>(OnBerserkMode);
        
        GameEventManager.AddListener<PlayerSwordAttackEvent>(OnSwordAttack);
        
        GameEventManager.AddListener<PlayerSwimEvent>(ToggleSwimming);
        
        InputUser.onChange += OnInputUserChange;

        /*if (dFlyMode)
        {
            dToggleFlyingMode.Enable();
            dToggleFlyingMode.performed += OnToggleFlying;
            dToggleFlyingMode.canceled += OnToggleFlying;
        }*/
    }

    void OnDisable()
    {
        GameEventManager.RemoveListener<SensitivityChangeEvent>(OnSensitivityChange);
        
        GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
        
        GameEventManager.RemoveListener<PlayerBerserkEvent>(OnBerserkMode);
        
        GameEventManager.RemoveListener<PlayerSwordAttackEvent>(OnSwordAttack);

        GameEventManager.RemoveListener<PlayerSwimEvent>(ToggleSwimming);
            
        InputUser.onChange -= OnInputUserChange;
        
        /*if (dFlyMode)
        {
            dToggleFlyingMode.performed -= OnToggleFlying;
            dToggleFlyingMode.canceled -= OnToggleFlying;
            dToggleFlyingMode.Disable();
        }*/
    }
    
    private void Awake()
    {
        /*if (locomotion == null)
            locomotion = GetComponent<UltimateCharacterLocomotion>();

        // load abilities of player to trigger them when needed
        ladderClimbAbility = locomotion.GetAbility<LadderClimb>();
        swimAbility = locomotion.GetAbility<Swim>();
        diveAbility = locomotion.GetAbility<Dive>();
        drownAbility = locomotion.GetAbility<Drown>();
        
        if(swimAbility != null)
            Debug.LogWarning($"SwimAbility initialized");
        if(diveAbility != null)
            Debug.LogWarning($"DiveAbility initialized");
        if(drownAbility != null)
            Debug.LogWarning($"DrownAbility initialized");
            */
    }
    
    /*private void StartAbility(Ability ability)
    {
        if (ability == null)
            return;

        if(locomotion.TryStartAbility(ability))
            Debug.LogWarning($"Ability '{ability.GetType()}' started");
        else
            Debug.LogWarning($"Ability '{ability.GetType()}' could not be started!");
    }

    private void StopAbility(Ability ability)
    {
        if (ability == null)
            return;

        if(locomotion.TryStopAbility(ability))
            Debug.LogWarning($"Ability '{ability.GetType()}' stopped");
        else
            Debug.LogWarning($"Ability '{ability.GetType()}' could not be stopped!");
    }*/

    private void ToggleSwimming(PlayerSwimEvent e)
    {
        /*Debug.LogWarning($"ToggleSwimming here: isInWater={e.isInWater}");

        if (swimAbility == null)
        {
            Debug.LogError("Swim ability is null.");
            return;
        }

        if(swimAbility.TryStartStopSwim(e.isInWater))
            Debug.LogWarning("SwimAbility was successfully started/stopped");
            */
    }

    /// <summary>
    /// This method is called by SwordUseBehaviour
    /// which also handles cooldown time.
    /// </summary>
    /// <param name="e"></param>
    private void OnSwordAttack(PlayerSwordAttackEvent e)
    {
        Debug.Log("Attack Mode activated");
        attackMode = true;
    }

    private void OnBerserkMode(PlayerBerserkEvent e)
    {
        if (berserkModeActive)
            return;
        
        // TODO: has no effect anymore, as speed of UCC has to be modified
        berserkModeActive = true;
        berserkFactor = e.factor;
        StartCoroutine(StopBerserkMode(e.duration));
    }

    private IEnumerator StopBerserkMode(float delay)
    {
        yield return new WaitForSeconds(delay);
        berserkModeActive = false;
    }

    /*private void OnToggleFlying(InputAction.CallbackContext context)
    {
        if (!dFlyMode)
            return;
        
        // Check if button is pressed or released
        dIsFlying = context.phase == InputActionPhase.Performed;
    }*/

    private void OnGameStateChange(GameStateChangedEvent e)
    {
        // TODO: UCC would need this to
        switch (e.newState)
        {
            case GameStateChangedEvent.GameState.Intro:
                _haveIControlOverPlayer = false;
                break;
            case GameStateChangedEvent.GameState.Play:
                _haveIControlOverPlayer = true;
                break;
            case GameStateChangedEvent.GameState.Inventory:
                _haveIControlOverPlayer = false;
                break;
            case GameStateChangedEvent.GameState.Paused:
                _haveIControlOverPlayer = false;
                break;
            case GameStateChangedEvent.GameState.Lost:
                _haveIControlOverPlayer = false;
                break;
            case GameStateChangedEvent.GameState.Won:
                _haveIControlOverPlayer = false;
                break;
        }
    }

    private void OnSensitivityChange(SensitivityChangeEvent e)
    {
        if (e.newSensitivityX < 0 || e.newSensitivityX > 3f ||
            e.newSensitivityY < 0 || e.newSensitivityY > 3f)
            return;

        if (e.mouseOrGamepad)  // gamepad
        {
            gamepadSensitivityX = e.newSensitivityX;
            gamepadSensitivityY = e.newSensitivityY;
        }
        else
        {  // mouse
            mouseSensitivityX = e.newSensitivityX;
            mouseSensitivityY = e.newSensitivityY;
        }
    }

    /*private void Awake()
    {
        if (dFlyMode)
        {
            dToggleFlyingMode.AddBinding("<Keyboard>/f");
            dToggleFlyingMode.AddBinding("<Gamepad>/buttonWest");
        }
    }*/

    private void Start()
    {
        _input = GetComponent<PlayerInputHandler>();
#if ENABLE_INPUT_SYSTEM 
        _playerInput = GetComponent<PlayerInput>();
#else
		Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

        AssignAnimationIDs();
        
        // get Player Data
        ServiceLocator.ForSceneOf(this).Get(out player);
        
        // for UI
        GameEventManager.Raise(new BroadcastInputControlSchemeEvent(_playerInput.currentControlScheme));
    }

    private void Update()
    {
        if (!_haveIControlOverPlayer)
            return;

        /*if (dIsFlying) 
        {
            HandleFlying();
        }*/
        
        Interact();
        UseHands();
    }

    private void AssignAnimationIDs()
    {
        _animIDAttack = Animator.StringToHash("Attack");
    }

    /*private void HandleFlying()
    {
        if (!dIsFlying)
            return;

        // Disable gravity while flying
        _verticalVelocity = 0f;

        // Apply upward movement based on input
        Vector3 flyDirection = Vector3.up; // Can extend to allow full 3D movement
        _controller.Move(dFlySpeed * Time.deltaTime * flyDirection);
    }*/

    /*private void CameraRotation()
    {
        float sensitivityX = IsCurrentDeviceMouse ? mouseSensitivityX : gamepadSensitivityX;
        float sensitivityY = IsCurrentDeviceMouse ? mouseSensitivityY : gamepadSensitivityY;
        
        // -- BERSERK MODE --
        if (berserkModeActive)
        {
            // add strength when berserkFactor > 1, else subtract
            int sign = berserkFactor >= 1 ? 1 : -1;
            sensitivityX += sign * berserkFactor / 100f;
            sensitivityY += sign * berserkFactor / 100f;
        }
    }*/

    /*private void Move()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
        
        // -- BERSERK MODE --
        if (berserkModeActive)
            targetSpeed *= berserkFactor;
        
    }*/

    private void Interact()
    {
        if (_input.interact) {
            Debug.Log("Interact Input");
            GameEventManager.Raise(new PlayerInteractionRequestEvent());

            _input.interact = false;  // reset for repressing
        }
    }

    private void UseHands()
    {
        if (_input.useLeftHand)
        {
            Debug.Log("LEFT HAND IS USED");
            GameEventManager.Raise(new PlayerUseHandRequestEvent(true));
            _input.useLeftHand = false;
        }
        if(_input.useRightHand)
        {
            attackMode = true;  // TODO: REMOVE !!!
            Debug.Log("RIGHT HAND IS USED");
            GameEventManager.Raise(new PlayerUseHandRequestEvent(false));
            _input.useRightHand = false;
        }
    }

    /*private void JumpAndGravity()
    {
        // modify jumpheight
        // -- BERSERK MODE --
        if (berserkModeActive)
            height *= berserkFactor;
    }*/

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}