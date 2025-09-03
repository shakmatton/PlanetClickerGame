// ==================== GameManager3D.cs ====================
// Anexe este script a um GameObject vazio chamado "GameManager"

using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

        // Initialize meteor spawn points around the planet
        InitializeMeteorSpawnPoints();
    }

    void Start()
    {
        // Get components
        clickSystem = GetComponent<ClickSystem3D>();
        meteorController = GetComponent<MeteorController3D>();
        powerUpManager = GetComponent<PowerUpManager3D>();
        timerController = GetComponent<TimerController3D>();

        // Start planet rotation
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
        // Create spawn points around the planet in 3D space
        meteorSpawnPoints = new Vector3[]
        {
            new Vector3(15, 5, 0),    // Right-top
            new Vector3(-15, 5, 0),   // Left-top
            new Vector3(0, 15, 5),    // Top-front
            new Vector3(0, 15, -5),   // Top-back
            new Vector3(10, 10, 10),  // Diagonal
            new Vector3(-10, 10, -10) // Diagonal opposite
        };
    }

    public void StartWave()
    {
        if (gameRunning) return;

        gameRunning = true;
        Debug.Log($"Starting 3D Wave {currentWave}");

        // Play music
        if (musicSource && finalCountdownClip)
        {
            musicSource.clip = finalCountdownClip;
            musicSource.loop = true;
            musicSource.Play();
        }

        // Choose random spawn point for this wave
        Vector3 spawnPoint = meteorSpawnPoints[Random.Range(0, meteorSpawnPoints.Length)];

        // Start timer and meteor
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

        // 3D Explosion effect
        if (explosionSound)
            AudioSource.PlayClipAtPoint(explosionSound, planet.transform.position);

        if (explosionPrefab)
        {
            GameObject explosion = Instantiate(explosionPrefab, planet.transform.position, Quaternion.identity);
            // Make explosion face camera
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

// ==================== ClickSystem3D.cs ====================
// Anexe este script ao objeto Planet



public class ClickSystem3D : MonoBehaviour
{
    [Header("3D Click Effects")]
    public GameObject clickEffectPrefab;
    public ParticleSystem clickParticles;
    public AudioClip clickSound;

    private AudioSource audioSource;
    private Renderer planetRenderer;
    private Color originalColor;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        planetRenderer = GetComponent<Renderer>();

        if (planetRenderer)
            originalColor = planetRenderer.material.color;

        // Add 3D collider for clicking
        if (!GetComponent<Collider>())
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
        }
    }

    void OnMouseDown()
    {
        if (!GameManager3D.Instance.gameRunning) return;
        OnPlanetClicked();
    }

    public void OnPlanetClicked()
    {
        GameManager3D.Instance.AddClicks(1);

        // Play click sound
        if (audioSource && clickSound)
            audioSource.PlayOneShot(clickSound);

        // Visual feedback - planet flash
        StartCoroutine(PlanetClickFlash());

        // Particle effect
        if (clickParticles)
            clickParticles.Play();

        // 3D Click effect at click position
        Create3DClickEffect();

        Debug.Log($"3D Planet clicked! Total clicks: {GameManager3D.Instance.totalClicks}");
    }

    System.Collections.IEnumerator PlanetClickFlash()
    {
        if (planetRenderer)
        {
            planetRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            planetRenderer.material.color = originalColor;
        }
    }

    void Create3DClickEffect()
    {
        if (clickEffectPrefab)
        {
            // Get click position in 3D world
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 effectPos = hit.point;
                GameObject effect = Instantiate(clickEffectPrefab, effectPos, Quaternion.identity);

                // Make effect face camera
                effect.transform.LookAt(Camera.main.transform);

                Destroy(effect, 1f);
            }
        }
    }
}

// ==================== MeteorController3D.cs ====================
// Anexe este script ao objeto Meteor



public class MeteorController3D : MonoBehaviour
{
    [Header("3D Movement Settings")]
    public float moveSpeed = 3f;
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("3D Visual Effects")]
    public ParticleSystem trailEffect;
    public ParticleSystem fireEffect;
    public GameObject impactPrefab;

    [Header("Audio")]
    public AudioSource meteorAudio;
    public AudioClip flybySound;

    private bool isMoving = false;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float journeyLength;
    private float journeyTime = 0f;

    void Start()
    {
        targetPosition = Vector3.zero; // Planet position
        ResetMeteor();
    }

    public void StartMeteorAttack(Vector3 spawnPoint)
    {
        startPosition = spawnPoint;
        transform.position = startPosition;

        journeyLength = Vector3.Distance(startPosition, targetPosition);
        journeyTime = 0f;
        isMoving = true;

        // Make meteor face target
        transform.LookAt(targetPosition);

        // Start effects
        if (trailEffect) trailEffect.Play();
        if (fireEffect) fireEffect.Play();

        // Play flyby sound
        if (meteorAudio && flybySound)
        {
            meteorAudio.clip = flybySound;
            meteorAudio.Play();
        }

        Debug.Log($"3D Meteor attack started from {spawnPoint}!");
    }

    public void ResetMeteor()
    {
        isMoving = false;
        journeyTime = 0f;

        if (trailEffect) trailEffect.Stop();
        if (fireEffect) fireEffect.Stop();
        if (meteorAudio) meteorAudio.Stop();

        // Hide meteor off-screen
        transform.position = new Vector3(100, 100, 100);
    }

    void Update()
    {
        if (isMoving)
        {
            journeyTime += Time.deltaTime;
            float journeyFraction = (journeyTime * moveSpeed) / journeyLength;

            // Use curve for dynamic speed
            float curveValue = speedCurve.Evaluate(journeyFraction);

            // Move meteor
            transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);

            // Add rotation for more dynamic look
            transform.Rotate(Vector3.forward, 50f * Time.deltaTime);

            // Check if reached target
            if (journeyFraction >= 1f)
            {
                isMoving = false;
                OnMeteorReachPlanet();
            }
        }
    }

    void OnMeteorReachPlanet()
    {
        // This is handled by the timer, but we can add impact effects here
        if (impactPrefab)
        {
            Instantiate(impactPrefab, targetPosition, Quaternion.identity);
        }
    }

    public void DestroyMeteor()
    {
        isMoving = false;

        // Create 3D destruction effect
        if (GameManager3D.Instance.explosionPrefab)
        {
            GameObject explosion = Instantiate(GameManager3D.Instance.explosionPrefab, transform.position, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * 2f; // Bigger explosion
        }

        // Stop all effects
        if (trailEffect) trailEffect.Stop();
        if (fireEffect) fireEffect.Stop();
        if (meteorAudio) meteorAudio.Stop();

        gameObject.SetActive(false);
        Debug.Log("3D Meteor destroyed by laser!");

        Invoke("ReactivateMeteor", 2f);
    }

    public void DeflectMeteor()
    {
        isMoving = false;

        // 3D deflection - bounce back with spin
        Vector3 deflectDirection = (startPosition - targetPosition).normalized;
        Vector3 deflectTarget = targetPosition + deflectDirection * 10f;

        StartCoroutine(Deflect3D(deflectTarget));
        Debug.Log("3D Meteor deflected by shield!");
    }

    public void DivertMeteor()
    {
        isMoving = false;

        // 3D wormhole effect - spiral away
        StartCoroutine(WormholeEffect3D());
        Debug.Log("3D Meteor diverted by wormhole!");
    }

    System.Collections.IEnumerator Deflect3D(Vector3 deflectTarget)
    {
        float deflectTime = 1f;
        Vector3 startPos = transform.position;

        for (float t = 0; t < deflectTime; t += Time.deltaTime)
        {
            float progress = t / deflectTime;
            transform.position = Vector3.Lerp(startPos, deflectTarget, progress);
            transform.Rotate(Vector3.up, 360f * Time.deltaTime);
            yield return null;
        }

        ResetMeteor();
    }

    System.Collections.IEnumerator WormholeEffect3D()
    {
        float effectTime = 2f;
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;

        for (float t = 0; t < effectTime; t += Time.deltaTime)
        {
            float progress = t / effectTime;

            // Spiral motion
            float angle = progress * 720f; // 2 full rotations
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * (1f - progress),
                Mathf.Sin(angle * Mathf.Deg2Rad) * (1f - progress),
                progress * 5f
            );

            transform.position = startPos + offset;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
            transform.Rotate(Vector3.forward, 720f * Time.deltaTime);

            yield return null;
        }

        ResetMeteor();
        transform.localScale = startScale; // Reset scale
    }

    void ReactivateMeteor()
    {
        gameObject.SetActive(true);
    }
}

// ==================== PlanetRotation.cs ====================
// Anexe este script ao Planet para rotação constante



public class PlanetRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Vector3 rotationSpeed = new Vector3(0, 15, 0); // Y-axis rotation
    public bool randomizeRotation = true;

    private bool isRotating = false;

    void Start()
    {
        if (randomizeRotation)
        {
            // Add some randomness to rotation
            rotationSpeed += new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(-10f, 10f),
                Random.Range(-5f, 5f)
            );
        }
    }

    public void StartRotation()
    {
        isRotating = true;
    }

    public void StopRotation()
    {
        isRotating = false;
    }

    void Update()
    {
        if (isRotating)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}

// ==================== TimerController3D.cs ====================
// Anexe este script ao GameManager



public class TimerController3D : MonoBehaviour
{
    private float currentTime;
    private bool timerRunning = false;

    public void StartTimer(float duration)
    {
        currentTime = duration;
        timerRunning = true;
        StartCoroutine(TimerCoroutine3D());
    }

    public void StopTimer()
    {
        timerRunning = false;
        StopAllCoroutines();
    }

    IEnumerator TimerCoroutine3D()
    {
        while (currentTime > 0 && timerRunning)
        {
            UpdateTimerDisplay3D();
            yield return new WaitForSeconds(1f);
            currentTime--;
        }

        if (timerRunning)
        {
            GameManager3D.Instance.OnTimerEnd();
        }
    }

    void UpdateTimerDisplay3D()
    {
        if (GameManager3D.Instance.timerText)
        {
            GameManager3D.Instance.timerText.text = Mathf.Ceil(currentTime).ToString();

            // Dramatic color changes for 3D
            if (currentTime <= 3)
            {
                GameManager3D.Instance.timerText.color = Color.red;
                GameManager3D.Instance.timerText.fontSize = 80; // Bigger for urgency
            }
            else if (currentTime <= 5)
            {
                GameManager3D.Instance.timerText.color = Color.yellow;
                GameManager3D.Instance.timerText.fontSize = 70;
            }
            else if (currentTime <= 10)
            {
                GameManager3D.Instance.timerText.color = Color.magenta;
                GameManager3D.Instance.timerText.fontSize = 60;
            }
            else
            {
                GameManager3D.Instance.timerText.color = Color.white;
                GameManager3D.Instance.timerText.fontSize = 50;
            }
        }
    }
}

// ==================== PowerUpManager3D.cs ====================
// Anexe este script ao GameManager



[System.Serializable]
public class PowerUp3D
{
    public string name;
    public int cost;
    public bool purchased;
    public Button buyButton;
    public string description;
    public GameObject visualEffect; // 3D visual effect when active
}

public class PowerUpManager3D : MonoBehaviour
{
    [Header("3D Power-ups")]
    public PowerUp3D[] powerUps = new PowerUp3D[4];

    [Header("3D Power-up Effects")]
    public GameObject shieldEffect; // 3D shield around planet
    public GameObject laserEffect;  // 3D laser beam effect
    public GameObject wormholeEffect; // 3D portal effect

    void Start()
    {
        InitializePowerUps3D();
        SetupShopButtons();
        UpdateShop();
    }

    void InitializePowerUps3D()
    {
        powerUps[0] = new PowerUp3D { name = "Click Multiplier", cost = 10, description = "5x click power" };
        powerUps[1] = new PowerUp3D { name = "Shield", cost = 50, description = "3D Energy Shield", visualEffect = shieldEffect };
        powerUps[2] = new PowerUp3D { name = "Laser", cost = 300, description = "Orbital Laser Platform", visualEffect = laserEffect };
        powerUps[3] = new PowerUp3D { name = "Wormhole", cost = 2000, description = "Spatial Rift Generator", visualEffect = wormholeEffect };
    }

    void SetupShopButtons()
    {
        Button[] shopButtons = FindObjectsByType<Button>();

        for (int i = 0; i < powerUps.Length && i < shopButtons.Length; i++)
        {
            powerUps[i].buyButton = shopButtons[i];
            int index = i;
            powerUps[i].buyButton.onClick.AddListener(() => BuyPowerUp3D(index));
        }
    }

    private T[] FindObjectsByType<T>()
    {
        throw new System.NotImplementedException();
    }

    public void BuyPowerUp3D(int index)
    {
        if (index >= powerUps.Length) return;

        PowerUp3D powerUp = powerUps[index];

        if (GameManager3D.Instance.totalClicks >= powerUp.cost && !powerUp.purchased)
        {
            GameManager3D.Instance.totalClicks -= powerUp.cost;
            GameManager3D.Instance.UpdateUI();

            powerUp.purchased = true;
            ApplyPowerUpEffect3D(index);
            UpdateShop();

            Debug.Log($"3D Power-up purchased: {powerUp.name}");
        }
    }

    void ApplyPowerUpEffect3D(int index)
    {
        switch (index)
        {
            case 0: // Click Multiplier
                GameManager3D.Instance.clickMultiplier = 5;
                break;

            case 1: // 3D Shield
                if (powerUps[1].visualEffect)
                {
                    GameObject shield = Instantiate(powerUps[1].visualEffect, GameManager3D.Instance.planet.transform);
                    shield.transform.localPosition = Vector3.zero;
                    shield.transform.localScale = Vector3.one * 1.2f;
                }
                Debug.Log("3D Energy Shield activated!");
                break;

            case 2: // 3D Laser
                if (powerUps[2].visualEffect)
                {
                    GameObject laser = Instantiate(powerUps[2].visualEffect, GameManager3D.Instance.planet.transform);
                    laser.transform.localPosition = Vector3.up * 3f;
                }
                Debug.Log("3D Orbital Laser Platform online!");
                break;

            case 3: // 3D Wormhole
                if (powerUps[3].visualEffect)
                {
                    GameObject wormhole = Instantiate(powerUps[3].visualEffect, GameManager3D.Instance.planet.transform);
                    wormhole.transform.localPosition = Vector3.forward * 5f;
                }
                Debug.Log("3D Spatial Rift Generator ready!");
                break;
        }
    }

    public bool CheckDefenses(int waveNumber)
    {
        if (waveNumber <= 2 && powerUps[1].purchased) // 3D Shield
        {
            Debug.Log("3D Shield deflected meteor!");
            GameManager3D.Instance.meteor.GetComponent<MeteorController3D>().DeflectMeteor();
            return true;
        }

        if (waveNumber <= 3 && powerUps[2].purchased) // 3D Laser
        {
            GameManager3D.Instance.meteor.GetComponent<MeteorController3D>().DestroyMeteor();
            return true;
        }

        if (waveNumber <= 4 && powerUps[3].purchased) // 3D Wormhole
        {
            GameManager3D.Instance.meteor.GetComponent<MeteorController3D>().DivertMeteor();
            return true;
        }

        return false;
    }

    public void UpdateShop()
    {
        for (int i = 0; i < powerUps.Length; i++)
        {
            if (powerUps[i].buyButton)
            {
                bool canAfford = GameManager3D.Instance.totalClicks >= powerUps[i].cost;
                bool notPurchased = !powerUps[i].purchased;

                powerUps[i].buyButton.interactable = canAfford && notPurchased;

                Text buttonText = powerUps[i].buyButton.GetComponentInChildren<Text>();
                if (buttonText)
                {
                    if (powerUps[i].purchased)
                        buttonText.text = "ATIVADO";
                    else
                        buttonText.text = $"{powerUps[i].name}\n{powerUps[i].cost} cliques\n{powerUps[i].description}";
                }
            }
        }
    }

    public void ResetPowerUps()
    {
        // Destroy visual effects
        for (int i = 1; i < powerUps.Length; i++)
        {
            if (powerUps[i].visualEffect)
            {
                GameObject[] effects = GameObject.FindGameObjectsWithTag("PowerUpEffect");
                foreach (GameObject effect in effects)
                {
                    Destroy(effect);
                }
            }
        }

        // Reset purchased status
        for (int i = 0; i < powerUps.Length; i++)
        {
            powerUps[i].purchased = false;
        }

        GameManager3D.Instance.clickMultiplier = 1;
        UpdateShop();
    }
}