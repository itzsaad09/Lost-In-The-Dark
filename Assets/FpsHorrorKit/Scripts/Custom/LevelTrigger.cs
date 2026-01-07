using UnityEngine;
using System.Reflection;
using System;

public class LevelTrigger : MonoBehaviour
{
    [Header("Settings")]
    public GameObject targetDoor;      // Drag "OutDoor_2" here
    public MonoBehaviour menuManager;  // Drag the object with MenuManager script here

    private Component doorScript;
    private FieldInfo isLockedField;

    void Start()
    {
        FindDoorScript();
    }

    void FindDoorScript()
    {
        if (targetDoor == null) return;

        // Searches OutDoor_2 and its children for the script
        MonoBehaviour[] scripts = targetDoor.GetComponentsInChildren<MonoBehaviour>();

        foreach (var script in scripts)
        {
            // We look for the exact script name and variable from your file
            if (script.GetType().Name == "DoorSystem")
            {
                // Note: using lowercase "isLocked" to match your DoorSystem.cs
                var field = script.GetType().GetField("isLocked");
                if (field != null)
                {
                    doorScript = script;
                    isLockedField = field;
                    Debug.Log("<color=green>LevelTrigger:</color> Successfully linked to DoorSystem!");
                    return;
                }
            }
        }
        Debug.LogError("<color=red>LevelTrigger:</color> Could not find DoorSystem or 'isLocked' variable.");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for Player tag
        if (other.CompareTag("Player"))
        {
            if (doorScript != null && isLockedField != null)
            {
                // Get the current value of isLocked
                bool isLockedValue = (bool)isLockedField.GetValue(doorScript);
                
                if (!isLockedValue)
                {
                    CallMenuFunction();
                }
                else
                {
                    Debug.Log("LevelTrigger: Player entered, but door is still locked.");
                }
            }
        }
    }

    private void CallMenuFunction()
    {
        if (menuManager == null) 
        {
            Debug.LogError("LevelTrigger: MenuManager slot is empty!");
            return;
        }

        // Use reflection to call the UI function
        MethodInfo method = menuManager.GetType().GetMethod("ShowLevelUpUI");
        if (method != null)
        {
            method.Invoke(menuManager, null);
            
            // Standard Freeze Logic
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            this.enabled = false;
        }
        else
        {
             Debug.LogError("LevelTrigger: Function 'ShowLevelUpUI' not found on MenuManager.");
        }
    }
}