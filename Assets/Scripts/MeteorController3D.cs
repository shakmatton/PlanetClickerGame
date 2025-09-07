using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// MeteorController3D com comportamento completo:
/// - StartMeteorAttack/Update/Reset
/// - DestroyMeteor()
/// - PassThrough() (wormhole)
/// - DeflectMeteor() (desvia/afasta)
/// - DivertMeteor() (efeito alternativo)
/// - MarkAsGigaMeteor()
/// - Usa reflection com GameManager apenas para notificações (sem assumir métodos obrigatórios)
/// </summary>
public class MeteorController3D : MonoBehaviour
{
    [Header("Movement")]
    public float baseMoveSpeed = 3f;
    [Tooltip("Multiply baseMoveSpeed by this (use 0.5 for 50% speed).")]
    public float speedMultiplier = 0.5f;
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("References")]
    public Transform planetTransform;
    public GameObject impactPrefab;
    public GameObject explosionPrefab;

    [Header("Visual / Audio")]
    public ParticleSystem trailEffect;
    public ParticleSystem fireEffect;
    public AudioSource meteorAudio;
    public AudioClip flybySound;

    // runtime state
    Vector3 startPosition;
    Vector3 targetPosition;
    float journeyLength;
    float journeyTime;
    float effectiveSpeed;
    Vector3 originalScale;
    bool isMoving = false;

    // flags de estado (public para outros scripts setarem)
    public bool isNeutralized = false;
    public bool isPassingThrough = false;
    public bool isGigaMeteor = false;
    public bool lastChanceUsed = false;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void Start()
    {
        if (planetTransform == null)
        {
            var gm = FindGameManagerInstance();
            if (gm != null)
            {
                var planetObj = GetFieldOrPropValue(gm, "planet");
                if (planetObj is GameObject go) planetTransform = go.transform;
                else if (planetObj is Transform t) planetTransform = t;
            }
        }

        targetPosition = (planetTransform != null) ? planetTransform.position : Vector3.zero;
        transform.position = new Vector3(1000f, 1000f, 1000f);
    }

    public void StartMeteorAttack(Vector3 spawnPoint)
    {
        isNeutralized = false;
        isPassingThrough = false;
        startPosition = spawnPoint;
        transform.position = startPosition;

        int wave = GetCurrentWave();

        // escala por onda
        if (wave <= 1) transform.localScale = originalScale;
        else if (wave == 2) transform.localScale = originalScale * 1.3f;
        else
        {
            if (planetTransform != null)
            {
                Vector3 pScale = planetTransform.localScale;
                float planetMax = Mathf.Max(Mathf.Abs(pScale.x), Mathf.Abs(pScale.y), Mathf.Abs(pScale.z));
                float meteorMax = Mathf.Max(Mathf.Abs(originalScale.x), Mathf.Abs(originalScale.y), Mathf.Abs(originalScale.z));
                if (meteorMax < 1e-4f) meteorMax = 1f;
                float factor = (planetMax / meteorMax) * 0.9f;
                transform.localScale = originalScale * factor;
            }
            else transform.localScale = originalScale * 3f;
        }

        targetPosition = (planetTransform != null) ? planetTransform.position : Vector3.zero;
        journeyLength = Vector3.Distance(startPosition, targetPosition);
        journeyTime = 0f;
        effectiveSpeed = baseMoveSpeed * speedMultiplier;
        isMoving = true;

        transform.LookAt(targetPosition);
        if (trailEffect) trailEffect.Play();
        if (fireEffect) fireEffect.Play();
        if (meteorAudio && flybySound) { meteorAudio.clip = flybySound; meteorAudio.Play(); }

        Debug.Log($"[Meteor] Start wave {wave} pos={spawnPoint} scale={transform.localScale} speed={effectiveSpeed}");
    }

    void Update()
    {
        if (!isMoving) return;

        journeyTime += Time.deltaTime;
        float journeyFraction = (journeyTime * effectiveSpeed) / Mathf.Max(journeyLength, 0.0001f);
        float curveVal = speedCurve.Evaluate(Mathf.Clamp01(journeyFraction));
        transform.position = Vector3.Lerp(startPosition, targetPosition, curveVal);
        transform.Rotate(Vector3.forward, 30f * Time.deltaTime);

        if (journeyFraction >= 1f)
        {
            isMoving = false;
            OnMeteorReachPlanet();
        }
    }

    void OnMeteorReachPlanet()
    {
        if (isNeutralized)
        {
            ResetMeteor();
            return;
        }

        if (isPassingThrough)
        {
            ResetMeteor();
            return;
        }

        if (isGigaMeteor && lastChanceUsed)
        {
            Debug.Log("[Meteor] Giga meteor reached planet; last chance used -> planet destroyed (evac visuals).");
            DoPlanetDestruction();
            return;
        }

        Debug.Log("[Meteor] Impact -> planet destroyed.");
        DoPlanetDestruction();
    }

    void DoPlanetDestruction()
    {
        // spawn explosion imediatamente (se houver)
        if (explosionPrefab)
            Instantiate(explosionPrefab, targetPosition, Quaternion.identity);

        // pare audio/efeitos
        if (meteorAudio) meteorAudio.Stop();
        if (trailEffect) trailEffect.Stop();
        if (fireEffect) fireEffect.Stop();

        // Notifica GameManager de forma segura
        InvokeGameManagerMethodSafe("OnGameOver");
        InvokeGameManagerMethodSafe("OnPlanetDestroyed");

        // Se o meteor foi parented em algum momento (ex.: ao "embutir" no planeta), desempanha-o,
        // para evitar que operações sobre o planeta arraste o meteor.
        try
        {
            if (transform.parent != null)
                transform.parent = null;
        }
        catch { }

        // Agora limpamos o meteor: damos um pequeno delay para a explosão aparecer, depois resetamos/hidamos o meteor.
        StartCoroutine(HideMeteorAfterImpact());
    }

    // Coroutine que espera a explosão terminar e reseta o meteor para fora da cena
    IEnumerator HideMeteorAfterImpact()
    {
        // espera curta para ver o efeito (ajuste se quiser)
        yield return new WaitForSeconds(0.6f);

        // garante que não há coroutines/conflicts em execução
        StopAllCoroutines();

        // reseta estado interno
        isMoving = false;
        isNeutralized = true;
        isPassingThrough = false;

        // posiciona meteor fora da cena e restaura escala original
        transform.position = new Vector3(1000f, 1000f, 1000f);
        transform.localScale = originalScale;

        // para qualquer som restante
        if (meteorAudio) meteorAudio.Stop();

        // notifica GameManager que a onda terminou (opcional)
        InvokeGameManagerMethodSafe("OnMeteorNeutralized");
        InvokeGameManagerMethodSafe("OnWaveSuccess");
        yield break;
    }


    /// <summary>
    /// Destrói meteor (laser)
    /// </summary>
    public void DestroyMeteor()
    {
        if (isNeutralized) return;
        isNeutralized = true;
        isMoving = false;

        if (explosionPrefab) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        if (trailEffect) trailEffect.Stop();
        if (fireEffect) fireEffect.Stop();
        if (meteorAudio) meteorAudio.Stop();

        // Notifica GameManager que a onda foi neutralizada (nomes de método variados)
        InvokeGameManagerMethodSafe("OnMeteorNeutralized");
        InvokeGameManagerMethodSafe("OnWaveSuccess");
        InvokeGameManagerMethodSafe("OnWaveCleared");

        transform.position = new Vector3(1000f, 1000f, 1000f);
        transform.localScale = originalScale;
        Debug.Log("[Meteor] Destroyed by power-up.");
    }

    /// <summary>
    /// Marca para passar através (wormhole)
    /// </summary>
    public void PassThrough()
    {
        isPassingThrough = true;
        StartCoroutine(PassThroughCoroutine());
    }

    IEnumerator PassThroughCoroutine()
    {
        yield return new WaitForSeconds(0.6f);

        if (trailEffect) trailEffect.Stop();
        if (fireEffect) fireEffect.Stop();
        if (meteorAudio) meteorAudio.Stop();

        transform.position = new Vector3(1000f, 1000f, 1000f);
        transform.localScale = originalScale;
        isPassingThrough = false;
        isMoving = false;

        InvokeGameManagerMethodSafe("OnMeteorNeutralized");
        InvokeGameManagerMethodSafe("OnWaveSuccess");
    }

    /// <summary>
    /// Desvia o meteor (efeito de shield/deflect): anima movimento curvo para longe e reseta.
    /// </summary>
    public void DeflectMeteor()
    {
        StopAllCoroutines();
        StartCoroutine(DeflectCoroutine());
    }

    IEnumerator DeflectCoroutine()
    {
        // calcula direção de deflexão para "passar ao lado" do planeta
        Vector3 dir = (startPosition - targetPosition).normalized;
        Vector3 deflectTarget = targetPosition + dir * 10f + Vector3.up * 2f;

        float t = 0f;
        float duration = 1.0f;
        Vector3 startPos = transform.position;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, deflectTarget, t / duration);
            transform.Rotate(Vector3.up, 360f * Time.deltaTime);
            yield return null;
        }

        ResetMeteor();
    }

    /// <summary>
    /// DivertMeteor: alternativa para wormhole/desvio — anima o meteor reduzindo escala e afastando.
    /// </summary>
    public void DivertMeteor()
    {
        StopAllCoroutines();
        StartCoroutine(DivertCoroutine());
    }

    IEnumerator DivertCoroutine()
    {
        float duration = 1.2f;
        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position + (transform.forward * 8f) + Vector3.up * 2f;
        Vector3 startScale = transform.localScale;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float p = t / duration;
            transform.position = Vector3.Lerp(startPos, endPos, p);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
            transform.Rotate(Vector3.forward, 720f * Time.deltaTime);
            yield return null;
        }

        ResetMeteor();
    }

    public void MarkAsGigaMeteor() => isGigaMeteor = true;

    public void ResetMeteor()
    {
        isMoving = false;
        isNeutralized = false;
        isPassingThrough = false;
        journeyTime = 0f;
        transform.position = new Vector3(1000f, 1000f, 1000f);
        transform.localScale = originalScale;
        if (trailEffect) trailEffect.Stop();
        if (fireEffect) fireEffect.Stop();
        if (meteorAudio) meteorAudio.Stop();
    }

    // ---------- Reflection helpers ----------
    object FindGameManagerInstance()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetType("GameManager3D");
                if (t == null) continue;
                var f = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if (f != null)
                {
                    var inst = f.GetValue(null);
                    if (inst != null) return inst;
                }
                var p = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (p != null)
                {
                    var inst = p.GetValue(null);
                    if (inst != null) return inst;
                }
            }
            catch { }
        }
        return null;
    }

    int GetCurrentWave()
    {
        var gm = FindGameManagerInstance();
        if (gm == null) return 1;
        var val = GetFieldOrPropValue(gm, "currentWave") ?? GetFieldOrPropValue(gm, "wave") ?? 1;
        if (val is int i) return i;
        try { return Convert.ToInt32(val); } catch { return 1; }
    }

    object GetFieldOrPropValue(object obj, string name)
    {
        if (obj == null) return null;
        Type t = obj.GetType();
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) return f.GetValue(obj);
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null) return p.GetValue(obj);
        return null;
    }

    void InvokeGameManagerMethodSafe(string methodName)
    {
        var gm = FindGameManagerInstance();
        if (gm == null) return;

        Type t = gm.GetType();
        var m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (m != null)
        {
            try
            {
                m.Invoke(gm, null);
                Debug.Log($"[Meteor] Invoked GameManager method '{methodName}' via reflection.");
                return;
            }
            catch (TargetInvocationException tie)
            {
                // mostra a exceção real disparada dentro do método alvo
                Debug.LogWarning($"[Meteor] InvocationTargetException invoking '{methodName}': {tie.InnerException?.Message}\n{tie.InnerException?.StackTrace}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Meteor] Failed invoking '{methodName}': {ex.Message}");
            }
        }
    }
}