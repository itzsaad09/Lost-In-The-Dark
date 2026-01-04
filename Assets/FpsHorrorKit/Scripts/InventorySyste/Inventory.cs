using System.Collections.Generic;
using UnityEngine;

namespace FpsHorrorKit
{
    [RequireComponent(typeof(AudioSource))]
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }

        [SerializeField] GameObject inventoryPanel;
        [SerializeField] private int inventorySize = 20;

        [Header("Inventory Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip itemAddedSound; // Played on successful pickup
        [SerializeField] private AudioClip inventoryFullSound;
        [Range(0, 1)] public float volume = 0.7f;

        private AudioSource audioSource;
        private Dictionary<Item, int> items = new Dictionary<Item, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // Set to 2D for clear UI sounds
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleInventory();
            }
        }

        private void ToggleInventory()
        {
            bool isOpening = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isOpening);

            if (isOpening)
            {
                PlaySound(openSound);
                InteractCameraSettings.Instance?.ShowCursor();
            }
            else
            {
                PlaySound(closeSound);
                InteractCameraSettings.Instance?.HideCursor();
            }
        }

        public bool AddItem(Item item, int quantity = 1)
        {
            // Check if non-stackable item fits
            if (!item.isStackable)
            {
                if (items.Count >= inventorySize)
                {
                    OnInventoryFull();
                    return false;
                }
                
                items[item] = 1; 
                OnItemAddedSuccess();
                return true;
            }

            // Handle Stackable items
            if (items.ContainsKey(item))
            {
                if (items[item] + quantity <= item.maxStackSize)
                {
                    items[item] += quantity;
                    OnItemAddedSuccess();
                    return true;
                }
                else
                {
                    OnInventoryFull();
                    return false;
                }
            }

            // Check if new stackable item fits
            if (items.Count >= inventorySize)
            {
                OnInventoryFull();
                return false;
            }

            items[item] = quantity;
            OnItemAddedSuccess();
            return true;
        }

        private void OnItemAddedSuccess()
        {
            PlaySound(itemAddedSound);
        }

        private void OnInventoryFull()
        {
            Debug.Log("Inventory is full!");
            PlaySound(inventoryFullSound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        public bool RemoveItem(Item item, int quantity = 1)
        {
            if (!items.ContainsKey(item)) return false;

            if (items[item] <= quantity)
            {
                items.Remove(item);
            }
            else
            {
                items[item] -= quantity;
            }
            return true;
        }

        public Dictionary<Item, int> GetItems() => items;
    }
}