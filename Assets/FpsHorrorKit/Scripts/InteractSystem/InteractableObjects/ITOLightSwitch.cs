namespace FpsHorrorKit
{
    using System.Collections;
    using UnityEngine;

    public class ITOLightSwitch : MonoBehaviour, IInteractable
    {
        [Header("Light Settings")]
        public GameObject _light;
        public Transform lightSwitchButton;
        public AudioSource switcherAudioSource;
        public bool lightActiveOnAwake = false;

        [Header("Rotation Settings")]
        public float onRotationAngle;
        public float offRotationAngle;
        public float rotationSpeed = 200f;

        [Header("Interact UI")]
        [SerializeField] private string interactText = "Light Open/Close [E]";

        bool isFinished = true;

        private void Awake()
        {
            if (lightActiveOnAwake)
            {
                _light.SetActive(true);
                lightSwitchButton.localEulerAngles = new Vector3(onRotationAngle, 0, 0);
            }
            else
            {
                _light.SetActive(false);
                lightSwitchButton.localEulerAngles = new Vector3(offRotationAngle, 0, 0);
            }
        }

        public void ForceSwitchOff()
        {
            if (!_light.activeSelf || !isFinished) return;
            
            HorrorLightManager manager = Object.FindFirstObjectByType<HorrorLightManager>();
            if (manager != null) manager.SetMasterPower(false);

            _light.SetActive(false);
            StartCoroutine(RotateSwitcher(offRotationAngle, onRotationAngle));
        }

        public void ForceSwitchOn()
        {
            if (_light.activeSelf || !isFinished) return;

            HorrorLightManager manager = Object.FindFirstObjectByType<HorrorLightManager>();
            if (manager != null) manager.SetMasterPower(true);

            _light.SetActive(true);
            StartCoroutine(RotateSwitcher(onRotationAngle, offRotationAngle));
        }

        public void Interact()
        {
            if (!isFinished) return;

            HorrorLightManager manager = Object.FindFirstObjectByType<HorrorLightManager>();
            if (manager != null) manager.SetMasterPower(true);

            _light.SetActive(!_light.activeSelf);
            float targetRotation = _light.activeSelf ? onRotationAngle : offRotationAngle;
            float initialRotation = _light.activeSelf ? offRotationAngle : onRotationAngle;

            StartCoroutine(RotateSwitcher(targetRotation, initialRotation));
        }

        IEnumerator RotateSwitcher(float targetRotation, float initialRotation)
        {
            isFinished = false;
            if (switcherAudioSource != null) switcherAudioSource.Play();

            float distance = Mathf.Abs(targetRotation - initialRotation);
            float currentRotation = initialRotation;
            float multiple = targetRotation > initialRotation ? 1 : -1;

            while (distance > 0.1f)
            {
                float step = rotationSpeed * Time.deltaTime;
                distance -= step;
                currentRotation += step * multiple;
                lightSwitchButton.localEulerAngles = new Vector3(currentRotation, 0, 0);
                yield return null;
            }
            lightSwitchButton.localEulerAngles = new Vector3(targetRotation, 0, 0);
            isFinished = true;
        }

        public void Highlight() => PlayerInteract.Instance.ChangeInteractText(interactText);
        public void HoldInteract() { }
        public void UnHighlight() { }
    }
}