// ==================== GameManager3D.cs ====================
// Arquivo: GameManager3D.cs
// Anexe APENAS este script ao GameObject "GameManager"

using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class GameManager3D : MonoBehaviour
{
    [Header("3D Game Objects")]
    public GameObject planet;
    public GameObject meteor;
    public GameObject explosionPrefab;
    public Camera mainCamera;

    [Header("UI Elements")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI clickCounterText;
    public TextMeshProUGUI waveText;
    public GameObject gameOverPanel;
    public GameObject shopPanel;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip finalCountdownClip;
    public AudioClip explosionSound;
    public AudioClip clickSound;

    [Header("3D Game Settings")]
    public float waveTimer = 20f;
    public Vector3[] meteorSpawnPoints;

    // Game State
    public static GameManager3D Instance;
    public int totalClicks = 0;
    public int clickMultiplier = 1;
    public int currentWave = 1;
    public bool gameRunning = false;

    // Components
    private ClickSystem3D clickSystem;
    private MeteorController3D meteorController;
    private PowerUpManager3D powerUpManager;
    private TimerController3D timerController;

    void Awake()
    {
        Instance = this;
        InitializeMeteorSpawnPoints();
    }

    void Start()
    {
        if (planet)
            clickSystem = planet.GetComponent<ClickSystem3D>();

        if (meteor)
            meteorController = meteor.GetComponent<MeteorController3D>();

        powerUpManager = GetComponent<PowerUpManager3D>();
        timerController = GetComponent<TimerController3D>();

        if (planet)
        {
            PlanetRotation planetRot = planet.GetComponent<PlanetRotation>();
            if (planetRot) planetRot.StartRotation();
        }

        UpdateUI();
        Invoke("StartWave", 2f);
    }


    void InitializeMeteorSpawnPoints()
    {
        meteorSpawnPoints = new Vector3[]
        {
            new Vector3(15, 5, 0),
            new Vector3(-15, 5, 0),
            new Vector3(0, 15, 5),
            new Vector3(0, 15, -5),
            new Vector3(10, 10, 10),
            new Vector3(-10, 10, -10)
        };
    }

    public void StartWave()
    {
        if (gameRunning) return;

        gameRunning = true;
        Debug.Log($"Starting 3D Wave {currentWave}");

        if (musicSource && finalCountdownClip)
        {
            musicSource.clip = finalCountdownClip;
            musicSource.loop = true;
            musicSource.Play();
        }

        Vector3 spawnPoint = meteorSpawnPoints[Random.Range(0, meteorSpawnPoints.Length)];

        timerController.StartTimer(waveTimer);
        meteorController.StartMeteorAttack(spawnPoint);

        // onde você chama meteorController.StartMeteorAttack(spawnPoint)
        meteorController.StartMeteorAttack(spawnPoint);

        // Exemplo: se currentWave >= 4 => marca como giga meteor
        if (meteorController != null && GameManager3D.Instance != null)
        {
            if (GameManager3D.Instance.currentWave >= 4)
            {
                meteorController.MarkAsGigaMeteor();
            }
        }

    }

    public void OnTimerEnd()
    {
        bool defended = powerUpManager.CheckDefenses(currentWave);

        if (defended)
        {
            OnWaveSuccess();
        }
        else
        {
            OnGameOver();
        }
    }

    public void OnWaveSuccess()
    {
        gameRunning = false;
        currentWave++;
        Debug.Log($"3D Wave {currentWave - 1} survived!");
        powerUpManager.ResetPowerUps();
        Invoke("StartWave", 3f);
    }

    public void OnGameOver()
    {
        gameRunning = false;

        if (explosionSound)
            AudioSource.PlayClipAtPoint(explosionSound, planet.transform.position);

        if (explosionPrefab)
        {
            GameObject explosion = Instantiate(explosionPrefab, planet.transform.position, Quaternion.identity);
            explosion.transform.LookAt(mainCamera.transform);
        }        

        // exemplo dentro do seu OnGameOver()
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameManager3D.OnGameOver(): gameOverPanel não está atribuído no Inspector.");
        }


        Debug.Log("3D GAME OVER! Planet destroyed!");
    }

    [System.Obsolete]
    public void OnMeteorNeutralized()
    {
        Debug.Log("GameManager3D: OnMeteorNeutralized() called.");

        // 1) Try to stop the timer (if your project uses TimerController3D)
        TimerController3D timer = null;
        // first try to get a TimerController attached to this GameManager
        timer = GetComponent<TimerController3D>();
        if (timer == null)
        {
            // fallback: find any TimerController in scene
            TimerController3D timerController3D = FindObjectOfType<TimerController3D>();
            timer = timerController3D;
        }
        if (timer != null)
        {
            try { timer.StopTimer(); }
            catch { Debug.LogWarning("GameManager3D: Failed to StopTimer() on TimerController3D."); }
        }

        // 2) Reset or hide the meteor (if reference exists)
        if (meteor != null)
        {
            var mc = meteor.GetComponent<MeteorController3D>();
            if (mc != null)
            {
                // ensure meteor is reset/hidden
                mc.ResetMeteor();
            }
            else
            {
                // if no script, just move meteor far away
                meteor.transform.position = new Vector3(1000f, 1000f, 1000f);
            }
        }

        // 3) Mark wave as survived and schedule next wave.
        // We try to reuse existing fields/methods if present, otherwise do safe defaults.

        // If you have a "gameRunning" flag, set it false (safe-guard)
        try
        {
            var gameRunningField = this.GetType().GetField("gameRunning", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (gameRunningField != null) gameRunningField.SetValue(this, false);
        }
        catch { }

        // Try to increment currentWave if it exists
        try
        {
            var waveField = this.GetType().GetField("currentWave", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (waveField != null)
            {
                int cur = (int)waveField.GetValue(this);
                waveField.SetValue(this, cur + 1);
            }
        }
        catch { }

        // Try to call a ResetPowerUps() if present (optional)
        try
        {
            var resetMethod = this.GetType().GetMethod("ResetPowerUps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (resetMethod != null) resetMethod.Invoke(this, null);
        }
        catch { }

        // Update UI if you have UpdateUI() method
        try
        {
            var updateMethod = this.GetType().GetMethod("UpdateUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (updateMethod != null) updateMethod.Invoke(this, null);
        }
        catch { }

        // Finally, schedule StartWave() after a short delay if it exists; otherwise log message.
        StartCoroutine(InvokeStartWaveDelayed());
    }

    // helper coroutine to call StartWave() after short delay (to give effects time to play)
    private IEnumerator InvokeStartWaveDelayed()
    {
        yield return new WaitForSeconds(1.5f); // small delay before next wave

        // Look for StartWave method on this instance (public or private)
        var startMethod = this.GetType().GetMethod("StartWave", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (startMethod != null)
        {
            try
            {
                startMethod.Invoke(this, null);
                Debug.Log("GameManager3D: StartWave invoked after neutralization.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("GameManager3D: Failed to invoke StartWave(): " + ex.Message);
            }
        }
        else
        {
            Debug.Log("GameManager3D: StartWave() not found; please ensure your GameManager has a StartWave method to begin next wave.");
        }
    }


    public void RestartGame()
    {
        totalClicks = 0;
        clickMultiplier = 1;
        currentWave = 1;
        gameRunning = false;

        gameOverPanel.SetActive(false);
        powerUpManager.ResetPowerUps();
        meteorController.ResetMeteor();

        UpdateUI();
        Invoke("StartWave", 2f);
    }

    public void AddClicks(int amount)
    {
        totalClicks += amount * clickMultiplier;
        UpdateUI();
        powerUpManager.UpdateShop();
    }

    public void UpdateUI()
    {
        if (clickCounterText)
            clickCounterText.text = $"Cliques: {totalClicks}";

        if (waveText)
            waveText.text = $"Onda: {currentWave}";
    }

    public bool TrySpendClicks(int amount)
    {
        if (totalClicks >= amount)
        {
            totalClicks -= amount;
            UpdateUI(); // se existir
            return true;
        }
        return false;
    }
}