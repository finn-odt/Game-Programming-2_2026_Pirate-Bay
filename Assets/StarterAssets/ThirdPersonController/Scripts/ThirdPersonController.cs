using System.Collections;
using GameEvents;
using Player;
using SLTypes;
using TriInspector;
using UnityEngine;
using UnityServiceLocator;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        private IPlayer player;
        
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioSource AudioFootsteps;
        public AudioSource LandingAudio;
        public AudioSource AudioFoley;
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;


        [Header("Look Sensitivity (Mouse)")]
        [SerializeField, Range(0f, 3f)] private float mouseSensitivityX = 1.8f;
        [SerializeField, Range(0f, 3f)] private float mouseSensitivityY = 0.85f;

        [Header("Look Sensitivity (Gamepad)")]
        [SerializeField, Range(0f, 3f)] private float gamepadSensitivityX = 0.5f;
        [SerializeField, Range(0f, 3f)] private float gamepadSensitivityY = 0.3f;
        
        
        // action for opening/closing the inventory [bindings are set in Awake()]
        [Header("DEBUG")]
        [SerializeField, LabelText("Fly Mode ?")] private bool dFlyMode;
        [SerializeField, LabelText("Fly Speed:")] private float dFlySpeed;
        private readonly InputAction dToggleFlyingMode = new("Toggle Flying Mode", InputActionType.Button);
        private bool dIsFlying = false;

        private bool berserkModeActive = false;
        private float berserkFactor;

        private bool attackMode;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDAttack;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        
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
        
        void OnEnable()
        {
            GameEventManager.AddListener<SensitivityChangeEvent>(OnSensitivityChange);
            
            GameEventManager.AddListener<GameStateChangedEvent>(OnGameStateChange);
            
            GameEventManager.AddListener<PlayerBerserkEvent>(OnBerserkMode);
            
            GameEventManager.AddListener<PlayerSwordAttackEvent>(OnSwordAttack);

            if (dFlyMode)
            {
                dToggleFlyingMode.Enable();
                dToggleFlyingMode.performed += OnToggleFlying;
                dToggleFlyingMode.canceled += OnToggleFlying;
            }
        }

        void OnDisable()
        {
            GameEventManager.RemoveListener<SensitivityChangeEvent>(OnSensitivityChange);
            
            GameEventManager.RemoveListener<GameStateChangedEvent>(OnGameStateChange);
            
            GameEventManager.RemoveListener<PlayerBerserkEvent>(OnBerserkMode);
            
            GameEventManager.RemoveListener<PlayerSwordAttackEvent>(OnSwordAttack);
            
            if (dFlyMode)
            {
                dToggleFlyingMode.performed -= OnToggleFlying;
                dToggleFlyingMode.canceled -= OnToggleFlying;
                dToggleFlyingMode.Disable();
            }
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
            
            berserkModeActive = true;
            berserkFactor = e.factor;
            StartCoroutine(StopBerserkMode(e.duration));
        }

        private IEnumerator StopBerserkMode(float delay)
        {
            yield return new WaitForSeconds(delay);
            berserkModeActive = false;
        }

        private void OnToggleFlying(InputAction.CallbackContext context)
        {
            if (!dFlyMode)
                return;
            
            // Check if button is pressed or released
            dIsFlying = context.phase == InputActionPhase.Performed;
        }

        private void OnGameStateChange(GameStateChangedEvent e)
        {
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


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            if (dFlyMode)
            {
                dToggleFlyingMode.AddBinding("<Keyboard>/f");
                dToggleFlyingMode.AddBinding("<Gamepad>/buttonWest");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            
            // get Player Data
            ServiceLocator.Global.Get(out player);
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            if (!_haveIControlOverPlayer)
                return;

            if (dIsFlying) 
            {
                HandleFlying();
            }
            else
            {   
                JumpAndGravity();
            }
            Move();
            
            GroundedCheck();
            Interact();
            UseHands();
        }

        private void LateUpdate()
        {
            if (!_haveIControlOverPlayer)
                return;
            
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDAttack = Animator.StringToHash("Attack");
        }

        private void HandleFlying()
        {
            if (!dIsFlying)
                return;

            // Disable gravity while flying
            _verticalVelocity = 0f;

            // Apply upward movement based on input
            Vector3 flyDirection = Vector3.up; // Can extend to allow full 3D movement
            _controller.Move(dFlySpeed * Time.deltaTime * flyDirection);
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            if (Grounded)
            {
                player.CurrentGrounds.Clear();

                // Cast downwards using a small capsule or multiple raycasts
                RaycastHit[] hits = Physics.SphereCastAll(
                    transform.position, 
                    0.75f, 
                    Vector3.down, 
                    2f, 
                    GroundLayers
                );

                foreach (var hit in hits)
                {
                    player.CurrentGrounds.Add(hit.collider.gameObject);
                }
            }

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                // Don't multiply mouse input by Time.deltaTime
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

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

                _cinemachineTargetYaw += _input.look.x * sensitivityX * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * sensitivityY * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
            
            // -- BERSERK MODE --
            if (berserkModeActive)
                targetSpeed *= berserkFactor;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero)
                targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = _input.move == Vector2.zero ? 0.0f : _speed;
            //float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            
                Debug.Log("Animator is updated");
                // attack - animator override
                if (attackMode)
                {
                    Debug.Log("Attack Animator is updated");
                    _animator.SetTrigger(_animIDAttack);
                    attackMode = false;
                }
            }
        }

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

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    float height = JumpHeight;
                    
                    // -- BERSERK MODE --
                    if (berserkModeActive)
                        height *= berserkFactor;
                    
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(height * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {

                if (AudioFootsteps != null)
                    AudioFootsteps.Play();
                if (AudioFoley != null)
                    AudioFoley.Play();
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (LandingAudio != null)
                    LandingAudio.Play();

            }
        }
    }
}