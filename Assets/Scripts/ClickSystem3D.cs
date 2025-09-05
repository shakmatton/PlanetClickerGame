// ==================== ClickSystem3D.cs ====================
// Arquivo: ClickSystem3D.cs
// Anexe APENAS este script ao GameObject "Planet"

using UnityEngine;

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
            if (planetRenderer && planetRenderer.material.HasProperty("_Color"))
                originalColor = planetRenderer.material.color;
            else
                originalColor = Color.blue; // fallback


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