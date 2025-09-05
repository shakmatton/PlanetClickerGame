// ==================== GameManager3D.cs ====================
// Arquivo: GameManager3D.cs
// Anexe APENAS este script ao GameObject "GameManager"

using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        gameOverPanel.SetActive(true);
        Debug.Log("3D GAME OVER! Planet destroyed!");
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
}