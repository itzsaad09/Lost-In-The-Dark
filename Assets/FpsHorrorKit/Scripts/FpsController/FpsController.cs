using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace FpsHorrorKit
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))] 
    public class FpsController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 4.0f;
        public float sprintSpeed = 7.0f;
        public float rotationSpeed = 1.0f;
        public float accelerationRate = 10.0f;
        public float decelerationRate = 10f;

        [Header("Jump Settings")]
        public float jumpHeight = 2f;
        public float gravity = -20f;
        public float jumpCooldown = 0.2f;

        [Header("Grounded Settings")]
        public float groundedOffset = .85f;
        public float groundedRadius = 0.3f;
        public LayerMask groundLayers;

        [Header("Camera Settings")]
        public CinemachineCamera virtualCamera;
        public float maxCameraPitch = 70f;
        public float minCameraPitch = -70f;

        [Header("Headbob Settings")]
        public CinemachineBasicMultiChannelPerlin headBob;
        public float headBobAcceleration = 10f;
        public float idleBobAmp = .5f;
        public float idleBobFreq = 1f;
        public float walkBobAmp = 3f;
        public float walkBobFreq = 1f;
        public float sprintBobAmp = 4f;
        public float sprintBobFreq = 3f;

        [Header("Audio Settings")]
        public AudioClip walkClip;
        public AudioClip sprintClip;
        public AudioClip jumpClip;
        [Range(0, 1)] public float volume = 0.5f;

        [Header("Interact Settings")]
        public bool isInteracting = false; // Status used by other scripts to freeze player

        private CharacterController characterController;
        private FpsAssetsInputs _input;
        private AudioSource audioSource; 

        private Vector3 velocity;
        private bool isGrounded;
        private float jumpCooldownTimer;
        private float cameraPitch;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            _input = GetComponent<FpsAssetsInputs>();
            audioSource = GetComponent<AudioSource>();
            
            audioSource.playOnAwake = false;
            audioSource.loop = true; 
        }

        private void Update()
        {
            HandleMovement();
            HandleGravity();
            HandleJumping();
            GroundedCheck();
            HandleMovementAudio(); 
        }

        private void LateUpdate()
        {
            HandleRotation();
        }

        private void HandleMovement()
        {
            if (isInteracting)
            {
                _input.move = Vector2.zero;
                velocity.x = 0;
                velocity.z = 0;
                // Keep headbob at idle settings during interaction
                headBob.AmplitudeGain = Mathf.Lerp(headBob.AmplitudeGain, idleBobAmp, Time.deltaTime * headBobAcceleration);
                headBob.FrequencyGain = Mathf.Lerp(headBob.FrequencyGain, idleBobFreq, Time.deltaTime * headBobAcceleration);
                return;
            }

            HeadBob();
            Vector2 input = _input.move;
            Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
            float targetSpeed = _input.sprint ? sprintSpeed : walkSpeed;

            if (moveDirection != Vector3.zero)
            {
                velocity.x = Mathf.Lerp(velocity.x, targetSpeed * moveDirection.x, Time.deltaTime * accelerationRate);
                velocity.z = Mathf.Lerp(velocity.z, targetSpeed * moveDirection.z, Time.deltaTime * accelerationRate);
            }
            else
            {
                velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime * decelerationRate);
                velocity.z = Mathf.Lerp(velocity.z, 0, Time.deltaTime * decelerationRate);
            }

            characterController.Move(new Vector3(velocity.x, 0, velocity.z) * Time.deltaTime);
        }

        private void HandleMovementAudio()
        {
            if (!isGrounded || isInteracting || _input.move.magnitude < 0.1f)
            {
                if (audioSource.isPlaying) audioSource.Pause();
                return;
            }

            AudioClip targetClip = _input.sprint ? sprintClip : walkClip;

            if (audioSource.clip != targetClip)
            {
                audioSource.clip = targetClip;
                audioSource.Play();
            }
            else if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }

            audioSource.volume = volume;
        }

        private void HandleRotation()
        {
            if (isInteracting) return;
            Vector2 lookInput = _input.look;
            cameraPitch += lookInput.y * rotationSpeed;
            cameraPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);
            virtualCamera.transform.localEulerAngles = new Vector3(cameraPitch, 0, 0);
            transform.Rotate(Vector3.up * lookInput.x * rotationSpeed);
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
            isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
        }

        private void HandleGravity()
        {
            if (isGrounded && velocity.y < 0) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(Vector3.up * velocity.y * Time.deltaTime);
        }

        private void HandleJumping()
        {
            if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;

            if (isGrounded)
            {
                if (_input.jump && jumpCooldownTimer <= 0)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    jumpCooldownTimer = jumpCooldown;
                    if (jumpClip != null) audioSource.PlayOneShot(jumpClip, volume);
                }
            }
            else _input.jump = false;
        }

        private void HeadBob()
        {
            float moveMagnitude = _input.move.magnitude;
            float targetAmp = moveMagnitude > 0 ? (_input.sprint ? sprintBobAmp : walkBobAmp) : idleBobAmp;
            float targetFreq = moveMagnitude > 0 ? (_input.sprint ? sprintBobFreq : walkBobFreq) : idleBobFreq;
            headBob.AmplitudeGain = Mathf.Lerp(headBob.AmplitudeGain, targetAmp, Time.deltaTime * headBobAcceleration);
            headBob.FrequencyGain = Mathf.Lerp(headBob.FrequencyGain, targetFreq, Time.deltaTime * headBobAcceleration);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrounded ? new Color(0, 1, 0, 0.35f) : new Color(1, 0, 0, 0.35f);
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z), groundedRadius);
        }
    }
}