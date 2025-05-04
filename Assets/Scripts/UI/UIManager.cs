using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

namespace CartoonZombieGame.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI zombiesRemainingText;
        
        [Header("Wave Notification")]
        [SerializeField] private GameObject waveNotificationPanel;
        [SerializeField] private TextMeshProUGUI waveNotificationText;
        [SerializeField] private TextMeshProUGUI nextWaveCountdownText;
        
        [Header("Game Over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverScoreText;
        [SerializeField] private TextMeshProUGUI gameOverWaveText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        
        [Header("Victory")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private TextMeshProUGUI victoryScoreText;
        [SerializeField] private Button victoryMainMenuButton;
        
        [Header("Pause Menu")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseMainMenuButton;
        [SerializeField] private Button quitButton;
        
        [Header("Main Menu")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button exitButton;
        
        [Header("Options Menu")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Button backButton;
        
        [Header("Credits")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button creditsBackButton;
        
        // References
        private CartoonZombieGame.Player.PlayerController playerController;
        private CartoonZombieGame.Managers.GameManager gameManager;
        
        // Coroutines
        private Coroutine waveMessageCoroutine;
        private Coroutine countdownCoroutine;
        
        private void Awake()
        {
            // Hide all panels initially except for the current scene's main panel
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                ShowMainMenu();
            }
            else
            {
                ShowHUD();
            }
        }
        
        private void Start()
        {
            // Find references if not set
            if (playerController == null)
            {
                playerController = FindObjectOfType<CartoonZombieGame.Player.PlayerController>();
            }
            
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<CartoonZombieGame.Managers.GameManager>();
            }
            
            // Set up button listeners
            SetupButtonListeners();
            
            // Subscribe to events
            SubscribeToEvents();
        }
        
        private void SetupButtonListeners()
        {
            // Game Over buttons
            if (restartButton != null)
                restartButton.onClick.AddListener(() => { gameManager?.RestartGame(); });
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(() => { gameManager?.QuitToMainMenu(); });
            
            // Victory buttons
            if (victoryMainMenuButton != null)
                victoryMainMenuButton.onClick.AddListener(() => { gameManager?.QuitToMainMenu(); });
            
            // Pause Menu buttons
            if (resumeButton != null)
                resumeButton.onClick.AddListener(() => { gameManager?.ResumeGame(); });
            
            if (pauseRestartButton != null)
                pauseRestartButton.onClick.AddListener(() => { gameManager?.RestartGame(); });
            
            if (pauseMainMenuButton != null)
                pauseMainMenuButton.onClick.AddListener(() => { gameManager?.QuitToMainMenu(); });
            
            if (quitButton != null)
                quitButton.onClick.AddListener(() => { gameManager?.QuitGame(); });
            
            // Main Menu buttons
            if (playButton != null)
                playButton.onClick.AddListener(() => { LoadGameScene(); });
            
            if (optionsButton != null)
                optionsButton.onClick.AddListener(() => { ShowOptionsMenu(); });
            
            if (creditsButton != null)
                creditsButton.onClick.AddListener(() => { ShowCredits(); });
            
            if (exitButton != null)
                exitButton.onClick.AddListener(() => { Application.Quit(); });
            
            // Options Menu buttons
            if (backButton != null)
                backButton.onClick.AddListener(() => { SaveOptions(); ShowMainMenu(); });
            
            // Credits buttons
            if (creditsBackButton != null)
                creditsBackButton.onClick.AddListener(() => { ShowMainMenu(); });
        }
        
        private void SubscribeToEvents()
        {
            if (playerController != null)
            {
                playerController.OnHealthChanged += UpdateHealthBar;
            }
            
            if (gameManager != null)
            {
                gameManager.OnWaveStart += UpdateWaveText;
                gameManager.OnScoreChanged += UpdateScoreText;
                gameManager.OnGameOver += HandleGameOver;
            }
        }
        
        // Update UI elements with game state
        private void UpdateHealthBar(int currentHealth, int maxHealth)
        {
            if (healthBar != null)
            {
                healthBar.value = (float)currentHealth / maxHealth;
            }
        }
        
        private void UpdateWaveText(int waveNumber)
        {
            if (waveText != null)
            {
                waveText.text = $"Wave: {waveNumber}";
            }
        }
        
        private void UpdateScoreText(int newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {newScore}";
            }
        }
        
        public void UpdateZombiesRemainingText(int remaining)
        {
            if (zombiesRemainingText != null)
            {
                zombiesRemainingText.text = $"Zombies: {remaining}";
            }
        }
        
        // Show/hide UI panels
        public void ShowHUD()
        {
            HideAllPanels();
            if (hudPanel != null) hudPanel.SetActive(true);
        }
        
        public void ShowWaveMessage(string message, float duration)
        {
            if (waveMessageCoroutine != null)
            {
                StopCoroutine(waveMessageCoroutine);
            }
            
            waveMessageCoroutine = StartCoroutine(ShowWaveMessageForDuration(message, duration));
        }
        
        private IEnumerator ShowWaveMessageForDuration(string message, float duration)
        {
            if (waveNotificationPanel != null && waveNotificationText != null)
            {
                waveNotificationText.text = message;
                waveNotificationPanel.SetActive(true);
                
                yield return new WaitForSeconds(duration);
                
                waveNotificationPanel.SetActive(false);
            }
            
            waveMessageCoroutine = null;
        }
        
        public void ShowNextWaveCountdown(float countdownTime)
        {
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
            }
            
            countdownCoroutine = StartCoroutine(CountdownToNextWave(countdownTime));
        }
        
        private IEnumerator CountdownToNextWave(float countdownTime)
        {
            if (nextWaveCountdownText != null)
            {
                nextWaveCountdownText.gameObject.SetActive(true);
                
                float timeRemaining = countdownTime;
                while (timeRemaining > 0)
                {
                    timeRemaining -= Time.deltaTime;
                    nextWaveCountdownText.text = $"Next Wave in: {Mathf.CeilToInt(timeRemaining)}";
                    yield return null;
                }
                
                nextWaveCountdownText.gameObject.SetActive(false);
            }
            
            countdownCoroutine = null;
        }
        
        public void ShowGameOverScreen(int score, int wave)
        {
            HideAllPanels();
            
            if (gameOverPanel != null)
            {
                if (gameOverScoreText != null)
                    gameOverScoreText.text = $"Final Score: {score}";
                
                if (gameOverWaveText != null)
                    gameOverWaveText.text = $"Waves Survived: {wave}";
                
                gameOverPanel.SetActive(true);
            }
        }
        
        public void ShowVictoryScreen(int score)
        {
            HideAllPanels();
            
            if (victoryPanel != null)
            {
                if (victoryScoreText != null)
                    victoryScoreText.text = $"Final Score: {score}";
                
                victoryPanel.SetActive(true);
            }
        }
        
        public void ShowPauseMenu()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }
        }
        
        public void HidePauseMenu()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
        }
        
        public void ShowMainMenu()
        {
            HideAllPanels();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }
        
        public void ShowOptionsMenu()
        {
            HideAllPanels();
            if (optionsPanel != null) optionsPanel.SetActive(true);
            
            // Load current settings
            LoadOptions();
        }
        
        public void ShowCredits()
        {
            HideAllPanels();
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }
        
        private void HideAllPanels()
        {
            if (hudPanel != null) hudPanel.SetActive(false);
            if (waveNotificationPanel != null) waveNotificationPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            
            if (nextWaveCountdownText != null) nextWaveCountdownText.gameObject.SetActive(false);
        }
        
        // Game state handlers
        private void HandleGameOver(int finalScore, int finalWave)
        {
            // Check if player won (reached max waves) or lost
            if (gameManager != null && finalWave >= gameManager.CurrentWave)
            {
                ShowVictoryScreen(finalScore);
            }
            else
            {
                ShowGameOverScreen(finalScore, finalWave);
            }
        }
        
        // Scene management
        private void LoadGameScene()
        {
            SceneManager.LoadScene("GameScene");
        }
        
        // Options handling
        private void LoadOptions()
        {
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
            
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            
            if (qualityDropdown != null)
                qualityDropdown.value = PlayerPrefs.GetInt("QualityLevel", 2);
        }
        
        private void SaveOptions()
        {
            if (musicVolumeSlider != null)
                PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
            
            if (sfxVolumeSlider != null)
                PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
            
            if (fullscreenToggle != null)
                PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
            
            if (qualityDropdown != null)
                PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);
            
            // Apply settings
            Screen.fullScreen = fullscreenToggle != null && fullscreenToggle.isOn;
            if (qualityDropdown != null)
                QualitySettings.SetQualityLevel(qualityDropdown.value);
            
            PlayerPrefs.Save();
        }
        
        // Input handling
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (gameManager != null)
                {
                    if (gameManager.IsGamePaused)
                    {
                        gameManager.ResumeGame();
                    }
                    else if (!gameManager.IsGameOver)
                    {
                        gameManager.PauseGame();
                    }
                }
            }
        }
    }
}