namespace FpsHorrorKit
{
    using UnityEngine;

    public class DragToOpenSystem : MonoBehaviour, IInteractable
    {
        public enum DoorDirection { left, right, front, back }

        [Header("Rotation Settings")]
        [Tooltip("Sensitivity of the mouse movement")]
        [SerializeField] private float rotationSpeed = 5f;

        [Tooltip("The angle of the door when closed (e.g., 0)")]
        [SerializeField] private float minAngle = 0f;

        [Tooltip("The angle of the door when fully open (e.g., 90)")]
        [SerializeField] private float maxAngle = 90f;

        [Tooltip("Direction used to determine player side relative to the door")]
        [SerializeField] private DoorDirection doorDirection = DoorDirection.left;

        [Header("Collider Settings")]
        [SerializeField] private bool colliderDisabledDuringInteraction = false;

        [Header("Audio Settings")]
        [Tooltip("Reference to the AudioSource attached to the door")]
        public AudioSource doorAudioSource;

        [Header("Interaction UI")]
        [SerializeField] private Sprite interactImageUi;

        private float currentAngle = 0f;
        private float initialAngle;
        private Vector3 initialForward;
        private Collider _collider;
        private Transform player;

        void Start()
        {
            _collider = GetComponent<Collider>();
            player = GameObject.FindGameObjectWithTag("Player").transform;

            initialAngle = transform.localEulerAngles.y;

            // Initialize the audio source settings if it exists
            if (doorAudioSource != null)
            {
                doorAudioSource.playOnAwake = false;
                doorAudioSource.loop = true;
            }

            switch (doorDirection)
            {
                case DoorDirection.left:
                    initialForward = -transform.right;
                    break;
                case DoorDirection.right:
                    initialForward = transform.right;
                    break;
                case DoorDirection.front:
                    initialForward = transform.forward;
                    break;
                case DoorDirection.back:
                    initialForward = -transform.forward;
                    break;
            }
        }

        public void Interact() { }

        public void HoldInteract()
        {
            if (colliderDisabledDuringInteraction && _collider != null)
            {
                _collider.enabled = false;
            }

            float mouseX = Input.GetAxis("Mouse X");

            float sideMultiplier = 1f;
            if (player != null)
            {
                Vector3 doorToPlayer = player.position - transform.position;
                float dot = Vector3.Dot(initialForward, doorToPlayer);
                sideMultiplier = (dot > 0) ? -1f : 1f;
            }

            float previousAngle = currentAngle;
            currentAngle += mouseX * rotationSpeed * sideMultiplier;
            currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);

            // Audio Logic: Uses the referenced doorAudioSource
            if (doorAudioSource != null)
            {
                // If the door is moving and not at its limits
                if (Mathf.Abs(currentAngle - previousAngle) > 0.001f)
                {
                    if (!doorAudioSource.isPlaying)
                    {
                        doorAudioSource.Play();
                    }
                }
                else
                {
                    if (doorAudioSource.isPlaying)
                    {
                        doorAudioSource.Stop();
                    }
                }
            }

            float targetAngle = initialAngle + currentAngle;
            transform.localEulerAngles = new Vector3(0, targetAngle, 0);
        }

        public void Highlight()
        {
            PlayerInteract.Instance.ChangeInteractImage(interactImageUi);
        }

        public void UnHighlight()
        {
            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (doorAudioSource != null && doorAudioSource.isPlaying)
            {
                doorAudioSource.Stop();
            }
        }
    }
}