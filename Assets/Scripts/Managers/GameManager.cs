using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace CartoonZombieGame.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private int startWave = 1;
        [SerializeField] private float timeBetweenWaves = 10f;
        [SerializeField] private int zombiesPerWave = 5;
        [SerializeField] private float zombieSpawnInterval = 2f;
        [SerializeField] private int maxWaves = 10;
        [SerializeField] private int pointsPerZombieKill = 10;
        [SerializeField] private int pointsPerWave = 50;

        [Header("Spawning")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject[] zombiePrefabs;

        [Header("References")]
        [SerializeField] private CartoonZombieGame.Player.PlayerController playerController;
        [SerializeField] private UIManager uiManager;

        // State tracking
        private int currentWave;
        private int remainingZombies;
        private int totalScore;
        private bool isSpawningWave;
        private bool isGameOver;
        private bool isGamePaused;
        private float waveStartTime;

        // Events
        public delegate void WaveStartDelegate(int waveNumber);
        public event WaveStartDelegate OnWaveStart;

        public delegate void WaveEndDelegate(int waveNumber);
        public event WaveEndDelegate OnWaveEnd;

        public delegate void GameOverDelegate(int finalScore, int finalWave);
        public event GameOverDelegate OnGameOver;

        public delegate void ScoreChangedDelegate(int newScore);
        public event ScoreChangedDelegate OnScoreChanged;

        // Singleton instance
        private static GameManager _instance;
        public static GameManager Instance => _instance;

        // Properties
        public int CurrentWave => currentWave;
        public int RemainingZombies => remainingZombies;
        public int TotalScore => totalScore;
        public float WaveTimer => isSpawningWave ? 0 : timeBetweenWaves - (Time.time - waveStartTime);
        public bool IsGameOver => isGameOver;
        public bool IsGamePaused => isGamePaused;

        private void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize
            currentWave = 0;
            totalScore = 0;
            isGameOver = false;
            isGamePaused = false;
        }

        private void Start()
        {
            // Find player if not set
            if (playerController == null)
            {
                playerController = FindObjectOfType<CartoonZombieGame.Player.PlayerController>();
            }

            // Find UI manager if not set
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            // Subscribe to player death event
            if (playerController != null)
            {
                playerController.OnPlayerDeath += HandlePlayerDeath;
            }

            // Start game with first wave
            StartCoroutine(StartNextWave());
        }

        private IEnumerator StartNextWave()
        {
            currentWave++;
            
            // Check if game is complete
            if (currentWave > maxWaves)
            {
                GameVictory();
                yield break;
            }

            // Calculate number of zombies for this wave
            int zombiesToSpawn = zombiesPerWave + (currentWave - 1) * 2;
            remainingZombies = zombiesToSpawn;

            // Trigger wave start event
            OnWaveStart?.Invoke(currentWave);
            
            if (uiManager != null)
            {
                uiManager.ShowWaveMessage($"Wave {currentWave}", 3f);
            }

            isSpawningWave = true;
            yield return new WaitForSeconds(2f); // Short delay before starting spawning

            // Spawn zombies
            for (int i = 0; i < zombiesToSpawn; i++)
            {
                if (!isGameOver)
                {
                    SpawnZombie();
                    yield return new WaitForSeconds(zombieSpawnInterval);
                }
                else
                {
                    break;
                }
            }

            isSpawningWave = false;
        }

        private void SpawnZombie()
        {
            if (zombiePrefabs.Length == 0 || spawnPoints.Length == 0)
                return;

            // Select random spawn point
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            // Select zombie type based on wave difficulty
            int zombieIndex = 0; // Default to normal zombie
            
            float rand = Random.value;
            if (currentWave >= 3 && rand < 0.1f + (currentWave * 0.03f))
            {
                // Tank zombie - rare but becomes more common in later waves
                zombieIndex = 2; // Assuming Tank is index 2
            }
            else if (currentWave >= 2 && rand < 0.3f + (currentWave * 0.05f))
            {
                // Runner zombie - becomes more common in later waves
                zombieIndex = 1; // Assuming Runner is index 1
            }
            else if (currentWave >= 4 && rand < 0.1f + (currentWave * 0.02f))
            {
                // Exploder zombie - rare but becomes more common in later waves
                zombieIndex = 3; // Assuming Exploder is index 3
            }

            // Clamp to available prefabs
            zombieIndex = Mathf.Min(zombieIndex, zombiePrefabs.Length - 1);

            // Instantiate zombie
            GameObject zombie = Instantiate(zombiePrefabs[zombieIndex], spawnPoint.position, spawnPoint.rotation);
            
            // Hook up the zombie death to our tracking
            ZombieController zombieController = zombie.GetComponent<ZombieController>();
            if (zombieController != null)
            {
                // Ideally we should have an event on ZombieController for this
                // For this example, we're using SendMessage in ZombieController.Die()
                // zombieController.OnZombieDeath += ZombieKilled;
            }
        }

        // This function should be called when a zombie is killed
        public void ZombieKilled()
        {
            remainingZombies--;
            
            // Award points
            AddScore(pointsPerZombieKill);
            
            // Check if wave is complete
            if (remainingZombies <= 0 && !isSpawningWave)
            {
                // Award bonus points for completing the wave
                AddScore(pointsPerWave * currentWave);
                
                // Trigger wave end event
                OnWaveEnd?.Invoke(currentWave);
                
                // Prepare for next wave
                waveStartTime = Time.time;
                
                // Show countdown to next wave
                if (uiManager != null && currentWave < maxWaves)
                {
                    uiManager.ShowNextWaveCountdown(timeBetweenWaves);
                }
                
                // Start next wave after delay
                Invoke("StartNextWaveDelayed", timeBetweenWaves);
            }
        }

        private void StartNextWaveDelayed()
        {
            StartCoroutine(StartNextWave());
        }

        private void AddScore(int points)
        {
            totalScore += points;
            OnScoreChanged?.Invoke(totalScore);
        }

        private void HandlePlayerDeath()
        {
            if (!isGameOver)
            {
                GameOver();
            }
        }

        private void GameOver()
        {
            isGameOver = true;
            
            // Cancel any pending wave starts
            CancelInvoke("StartNextWaveDelayed");
            
            // Trigger game over event
            OnGameOver?.Invoke(totalScore, currentWave);
            
            // Show game over UI
            if (uiManager != null)
            {
                uiManager.ShowGameOverScreen(totalScore, currentWave);
            }
        }

        private void GameVictory()
        {
            isGameOver = true;
            
            // Award bonus points for completing all waves
            AddScore(pointsPerWave * maxWaves * 2);
            
            // Trigger game over event with victory flag
            OnGameOver?.Invoke(totalScore, currentWave);
            
            // Show victory UI
            if (uiManager != null)
            {
                uiManager.ShowVictoryScreen(totalScore);
            }
        }

        public void PauseGame()
        {
            if (!isGameOver)
            {
                isGamePaused = true;
                Time.timeScale = 0f;
                
                // Show pause menu
                if (uiManager != null)
                {
                    uiManager.ShowPauseMenu();
                }
            }
        }

        public void ResumeGame()
        {
            isGamePaused = false;
            Time.timeScale = 1f;
            
            // Hide pause menu
            if (uiManager != null)
            {
                uiManager.HidePauseMenu();
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Auto-pause when game loses focus
            if (!hasFocus && !isGamePaused && !isGameOver)
            {
                PauseGame();
            }
        }
    }

    // The UIManager class would be in a separate file in a real project
    // This is just a placeholder for reference
    public class UIManager : MonoBehaviour
    {
        public void ShowWaveMessage(string message, float duration) { }
        public void ShowNextWaveCountdown(float countdownTime) { }
        public void ShowGameOverScreen(int score, int wave) { }
        public void ShowVictoryScreen(int score) { }
        public void ShowPauseMenu() { }
        public void HidePauseMenu() { }
    }
}