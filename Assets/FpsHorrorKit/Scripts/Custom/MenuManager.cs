using UnityEngine;
using UnityEngine.SceneManagement;

namespace FpsHorrorKit
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        public GameObject startMenu;
        public GameObject resumeMenu;
        public GameObject gameOverUI;
        public GameObject levelUpUI; // Reference for the new LevelUp UI

        private static bool isFirstTime = true; 
        private bool isPaused = false;

        private void Start()
        {
            if (isFirstTime)
            {
                ShowStartMenu();
            }
            else
            {
                StartGame();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Prevent pausing if other menus are active
                if (startMenu != null && startMenu.activeSelf) return; 
                if (gameOverUI != null && gameOverUI.activeSelf) return; 
                if (levelUpUI != null && levelUpUI.activeSelf) return; 

                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }

        public void ShowStartMenu()
        {
            startMenu.SetActive(true);
            resumeMenu.SetActive(false);
            if (gameOverUI != null) gameOverUI.SetActive(false);
            if (levelUpUI != null) levelUpUI.SetActive(false); // Ensure LevelUp is hidden
            
            Time.timeScale = 0f;
            AudioListener.pause = true; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void StartGame()
        {
            isFirstTime = false; 
            startMenu.SetActive(false);
            resumeMenu.SetActive(false);
            if (levelUpUI != null) levelUpUI.SetActive(false);
            
            Time.timeScale = 1f;
            AudioListener.pause = false; 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // --- Level Up Logic ---
        public void ShowLevelUpUI()
        {
            if (levelUpUI != null)
            {
                levelUpUI.SetActive(true);
                Time.timeScale = 0f; // Freeze game
                AudioListener.pause = true; 
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void LoadNextLevel(string sceneName)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(sceneName);
        }
        // ----------------------

        public void PauseGame()
        {
            isPaused = true;
            resumeMenu.SetActive(true);
            Time.timeScale = 0f;
            AudioListener.pause = true; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResumeGame()
        {
            isPaused = false;
            resumeMenu.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false; 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void RestartFromZero()
        {
            isFirstTime = false; 
            Time.timeScale = 1f;
            AudioListener.pause = false; 
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}