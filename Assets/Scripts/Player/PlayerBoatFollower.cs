using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerBoatFollower : MonoBehaviour
    {
        private CharacterController _controller;
        private MovingShipBehaviour _currentBoat;

        private Vector3 previousBoatPosition;
        private Quaternion previousBoatRotation;
        
        private bool _hitBoatThisFrame = true;

        private void Awake()
        {
            _controller = GetComponentInChildren<CharacterController>();
        }

        private void Start()
        {
            if (_currentBoat == null)
                return;

            previousBoatPosition = _currentBoat.transform.position;
            previousBoatRotation = _currentBoat.transform.rotation;
        }

        private void Update()
        {
            if(_currentBoat == null)
                return;
            
            if (!_hitBoatThisFrame)
            {
                _currentBoat = null;
            }
            _hitBoatThisFrame = false;
        }

        private void LateUpdate()
        {
            if (_currentBoat == null)
                return;

            Vector3 deltaPosition = _currentBoat.transform.position - previousBoatPosition;
            // calculate rotation offset for player
            Vector3 pivot = previousBoatPosition; // use previous position as rotation pivot
            Quaternion deltaRotation =
                _currentBoat.transform.rotation * Quaternion.Inverse(previousBoatRotation); // Current = Delta * Prev
            Vector3 playerOffsetFromPivot = transform.position - pivot;
            Vector3 rotatedPlayerOffset = deltaRotation * playerOffsetFromPivot;
            Vector3 targetPlayerPosition = _currentBoat.transform.position + rotatedPlayerOffset;
            Vector3 platformDelta = targetPlayerPosition - transform.position;

            // move by delta position and rotation that was applied
            _controller.Move(platformDelta);

            previousBoatPosition = _currentBoat.transform.position;
            previousBoatRotation = _currentBoat.transform.rotation;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            MovingShipBehaviour boat = hit.collider.GetComponentInParent<MovingShipBehaviour>();

            if (boat == null)
                return;
            
            _hitBoatThisFrame = true;

            if (boat == _currentBoat)
                return;

            _currentBoat = boat;
            previousBoatPosition = _currentBoat.transform.position;
            previousBoatRotation = _currentBoat.transform.rotation;

            Debug.Log("Entered boat");
        }
    }
}


/*using UnityEngine;

public class PlayerBoatFollower : MonoBehaviour
{
    private Transform playerRoot;
    private CharacterController controller;
    public MovingShipBehaviour currentBoat;

    void Start()
    {
        controller = GetComponentInChildren<CharacterController>();
        playerRoot = transform;
    }

    void LateUpdate()
    {
        /*if (currentBoat == null)
            return;

        Vector3 boatPivot = currentBoat.transform.position;

        Vector3 oldPlayerPos = playerRoot.position;
        Vector3 relative = oldPlayerPos - boatPivot;

        Vector3 rotatedRelative = currentBoat.DeltaRotation * relative;
        Vector3 rotatedPlayerPos = boatPivot + rotatedRelative;

        Vector3 platformDelta = rotatedPlayerPos - oldPlayerPos;

        controller.Move(currentBoat.DeltaPosition + platformDelta);

        // Optional: rotate player with the boat yaw
        //playerRoot.rotation = currentBoat.DeltaRotation * playerRoot.rotation;
    }
}
        */
