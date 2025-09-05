// ==================== MeteorController3D.cs ====================
// Arquivo: MeteorController3D.cs
// Anexe APENAS este script ao GameObject "Meteor"

using UnityEngine;

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