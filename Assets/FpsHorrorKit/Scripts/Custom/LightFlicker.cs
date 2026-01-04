using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HorrorLightManager : MonoBehaviour
{
    [Header("Main Switch Reference")]
    public FpsHorrorKit.ITOLightSwitch mainSwitch; 

    [Header("Targeting")]
    public Transform lightGroupParent; 
    public Transform player;
    public Transform enemy;

    [Header("Flicker Settings")]
    public float minFlickerSpeed = 0.02f;
    public float maxFlickerSpeed = 0.15f;
    [Range(0, 1)] public float stabilityChance = 0.85f;

    [Header("Audio - Spark/Flicker")]
    public AudioClip flickerClip; 
    [Range(0, 1)] public float flickerVolume = 0.5f;

    [Header("Audio - Random Blackout Event")]
    public AudioClip blackoutEventClip; 
    [Range(0, 1)] public float blackoutEventVolume = 1.0f;
    private AudioSource globalAudioSource;

    [Header("Audio - Rising Tension")]
    public AudioSource tensionSource; 
    public float tensionMaxDistance = 25f;
    public float tensionMinDistance = 5f;

    [Header("Horror Mechanics - Distance")]
    public float panicDistance = 6f; 

    [Header("Horror Mechanics - Random Trip")]
    public float randomBlackoutChance = 0.1f;
    public float minTimeBetweenBlackouts = 25f;
    public float autoResetDelay = 5f; 

    private bool masterPower = true; 
    private bool lightsAreOut = false;
    private bool isRandomBlackoutActive = false;
    private float nextPossibleBlackoutTime;
    private List<LightData> managedLights = new List<LightData>();

    private class LightData {
        public Light lightComponent;
        public AudioSource audioSource;
        public float nextActionTime;
    }

    void Start()
    {
        globalAudioSource = gameObject.AddComponent<AudioSource>();
        globalAudioSource.spatialBlend = 0f; 

        if (lightGroupParent == null) return;
        Light[] lights = lightGroupParent.GetComponentsInChildren<Light>(true);
        foreach (Light l in lights)
        {
            if (!l.gameObject.activeInHierarchy) continue;
            LightData data = new LightData { lightComponent = l };
            if (flickerClip != null)
            {
                AudioSource source = l.gameObject.AddComponent<AudioSource>();
                source.clip = flickerClip;
                source.spatialBlend = 1.0f; 
                source.playOnAwake = false;
                source.volume = flickerVolume;
                data.audioSource = source;
            }
            managedLights.Add(data);
        }
        nextPossibleBlackoutTime = Time.time + minTimeBetweenBlackouts;
    }

    public void SetMasterPower(bool state)
    {
        masterPower = state;
        if (!masterPower) SetLightsState(false);
        else if (Vector3.Distance(player.position, enemy.position) >= panicDistance) SetLightsState(true);
    }

    void Update()
    {
        if (player == null || enemy == null) return;
        float dist = Vector3.Distance(player.position, enemy.position);

        if (tensionSource != null)
        {
            float volumeT = 1 - Mathf.InverseLerp(tensionMinDistance, tensionMaxDistance, dist);
            tensionSource.volume = volumeT;
            if (tensionSource.isActiveAndEnabled && !tensionSource.isPlaying && volumeT > 0) tensionSource.Play();
        }

        if (!masterPower) return;

        HandleBlackoutStates(dist);

        if (!lightsAreOut) HandleGlobalFlicker();
    }

    void HandleBlackoutStates(float dist)
    {
        if (dist < panicDistance)
        {
            if (!lightsAreOut) SetLightsState(false);
            isRandomBlackoutActive = false; 
        }
        else if (!isRandomBlackoutActive && Time.time > nextPossibleBlackoutTime)
        {
            if (Random.value < (randomBlackoutChance * Time.deltaTime))
            {
                StartCoroutine(TemporaryPowerTrip());
            }
        }
        else if (dist >= panicDistance && lightsAreOut && !isRandomBlackoutActive)
        {
            SetLightsState(true);
        }
    }

    IEnumerator TemporaryPowerTrip()
    {
        isRandomBlackoutActive = true;
        
        if (blackoutEventClip != null)
            globalAudioSource.PlayOneShot(blackoutEventClip, blackoutEventVolume);

        if (mainSwitch != null) mainSwitch.ForceSwitchOff();
        else SetMasterPower(false);

        yield return new WaitForSeconds(autoResetDelay);

        if (Vector3.Distance(player.position, enemy.position) >= panicDistance)
        {
            if (mainSwitch != null) mainSwitch.ForceSwitchOn();
            else SetMasterPower(true);
        }

        isRandomBlackoutActive = false;
        nextPossibleBlackoutTime = Time.time + minTimeBetweenBlackouts;
    }

    void HandleGlobalFlicker()
    {
        foreach (var data in managedLights)
        {
            if (Time.time >= data.nextActionTime)
            {
                data.lightComponent.enabled = !data.lightComponent.enabled;
                
                if (data.lightComponent.enabled && data.audioSource != null && data.audioSource.isActiveAndEnabled)
                {
                    data.audioSource.PlayOneShot(flickerClip);
                }
                else if (!data.lightComponent.enabled && data.audioSource != null)
                {
                    // Instant stop when flicker turns light off
                    data.audioSource.Stop(); 
                }

                float wait = Random.Range(minFlickerSpeed, maxFlickerSpeed);
                if (data.lightComponent.enabled && Random.value < stabilityChance) wait = Random.Range(0.5f, 3.0f);
                data.nextActionTime = Time.time + wait;
            }
        }
    }

    void SetLightsState(bool state)
    {
        lightsAreOut = !state;
        foreach (var data in managedLights)
        {
            data.lightComponent.enabled = state;
            if (data.audioSource != null) 
            {
                data.audioSource.mute = !state;
                if (!state) data.audioSource.Stop(); // INSTANT OFF: Stops any playing sound immediately
            }
        }
    }
}