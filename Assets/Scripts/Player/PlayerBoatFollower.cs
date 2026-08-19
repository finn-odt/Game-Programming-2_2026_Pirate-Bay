using Opsive.UltimateCharacterController.Character;
using UnityEngine;

namespace Player
{
    public class PlayerBoatFollower : MonoBehaviour
    {
        [SerializeField] private LayerMask boatLayers = ~0;
        [SerializeField] private float castDistance = 0.35f;
        [SerializeField] private float castRadius = 0.25f;

        private UltimateCharacterLocomotion _locomotion;  // use SetMovingPlatform() for MoveWithObject-Ability
        private MovingShipBehaviour _currentBoat;

        private void Awake()
        {
            _locomotion = GetComponent<UltimateCharacterLocomotion>();
        }

        private void LateUpdate()
        {
            MovingShipBehaviour detectedBoat = DetectBoatBelow();

            if (detectedBoat != _currentBoat)
            {
                _currentBoat = detectedBoat;
                if (_currentBoat != null)
                {
                    // SET TARGET
                    _locomotion.SetMovingPlatform(_currentBoat.transform);
                }
                else
                {
                    // RESET
                    _locomotion.SetMovingPlatform(null);
                }
            }
        }

        private MovingShipBehaviour DetectBoatBelow()
        {
            if (_locomotion == null)
                return null;

            Vector3 up = _locomotion.Up;
            Vector3 origin = transform.position + up * 0.1f;
            Vector3 direction = -up;

            if (Physics.SphereCast(
                    origin,
                    castRadius,
                    direction,
                    out RaycastHit hit,
                    castDistance,
                    boatLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<MovingShipBehaviour>();
            }

            return null;
        }

        private void OnDisable()
        {
            if (_locomotion != null)
            {
                _locomotion.SetMovingPlatform(null);
            }

            _currentBoat = null;
        }

        private void OnDestroy()
        {
            if (_locomotion != null)
            {
                _locomotion.SetMovingPlatform(null);
            }

            _currentBoat = null;
        }
    }
}