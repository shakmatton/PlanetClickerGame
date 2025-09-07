using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// PowerUpController - sem APIs obsoletas.
/// - ApplyMultiplier(): tenta ajustar GameManager.totalClicks por reflexão
/// - UseLaser(), UseWormhole(), UseLastChance()
/// - Usamos Resources.FindObjectsOfTypeAll<T>() para localizar meteoros (compatível com todas as versões)
/// </summary>
public class PowerUpController : MonoBehaviour
{
    [Header("Multiplier")]
    public int multiplierPerPurchase = 5;
    public int multiplierPurchases = 0;

    [Header("Wormhole")]
    public GameObject wormholeVisualPrefab;
    public float wormholeDuration = 2.5f;

    [Header("Last Chance")]
    public GameObject evacShipPrefab;
    public int evacCount = 8;
    public float evacSpawnRadius = 1.5f;

    Transform planetTransform;

    void Start()
    {
        var gm = FindGameManagerInstance();
        if (gm != null)
        {
            var planetObj = GetFieldOrPropValue(gm, "planet");
            if (planetObj is GameObject go) planetTransform = go.transform;
            else if (planetObj is Transform t) planetTransform = t;
        }
    }

    public void ApplyMultiplier()
    {
        multiplierPurchases++;
        int add = multiplierPerPurchase;

        var gm = FindGameManagerInstance();
        if (gm != null)
        {
            var fld = gm.GetType().GetField("totalClicks", BindingFlags.Public | BindingFlags.Instance);
            if (fld != null)
            {
                object cur = fld.GetValue(gm);
                if (cur is int)
                {
                    int v = (int)cur;
                    v += add;
                    fld.SetValue(gm, v);
                    TryInvokeMethod(gm, "UpdateUI");
                    Debug.Log($"[PowerUp] Multiplier applied: +{add} clicks (via GameManager.totalClicks).");
                    return;
                }
            }
            var prop = gm.GetType().GetProperty("totalClicks", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                try
                {
                    object cur = prop.GetValue(gm);
                    int v = Convert.ToInt32(cur);
                    v += add;
                    prop.SetValue(gm, v);
                    TryInvokeMethod(gm, "UpdateUI");
                    Debug.Log($"[PowerUp] Multiplier applied (prop): +{add} clicks.");
                    return;
                }
                catch { }
            }
        }

        Debug.LogWarning("[PowerUp] Could not apply multiplier to GameManager; no compatible field/property found.");
    }

    public void UseLaser()
    {
        var mc = FindActiveMeteor();
        if (mc == null)
        {
            Debug.LogWarning("[PowerUp] UseLaser: no active meteor found.");
            return;
        }
        mc.DestroyMeteor();
    }

    public void UseWormhole()
    {
        var mc = FindActiveMeteor();
        if (mc == null)
        {
            Debug.LogWarning("[PowerUp] UseWormhole: no active meteor found.");
            return;
        }

        if (wormholeVisualPrefab != null && planetTransform != null)
        {
            Instantiate(wormholeVisualPrefab, planetTransform.position, Quaternion.identity);
        }

        StartCoroutine(WormholeSequence(mc));
    }

    IEnumerator WormholeSequence(MeteorController3D mc)
    {
        GameObject planet = (planetTransform != null) ? planetTransform.gameObject : null;
        if (planet != null) planet.SetActive(false);

        mc.PassThrough();

        yield return new WaitForSeconds(wormholeDuration);

        if (planet != null) planet.SetActive(true);

        Debug.Log("[PowerUp] Wormhole sequence complete.");
    }

    public void UseLastChance()
    {
        var gm = FindGameManagerInstance();
        GameObject planetObj = null;
        if (gm != null)
        {
            var p = GetFieldOrPropValue(gm, "planet");
            if (p is GameObject go) planetObj = go;
            else if (p is Transform t) planetObj = t.gameObject;
        }

        if (planetObj == null)
        {
            var planetGO = GameObject.Find("Planet");
            if (planetGO != null) planetObj = planetGO;
        }

        if (planetObj == null)
        {
            Debug.LogWarning("[PowerUp] UseLastChance: planet not found to spawn evac ships.");
            return;
        }

        for (int i = 0; i < evacCount; i++)
        {
            Vector3 offset = UnityEngine.Random.onUnitSphere * evacSpawnRadius;
            offset.y = Math.Abs(offset.y);
            Vector3 spawnPos = planetObj.transform.position + offset;
            if (evacShipPrefab != null)
            {
                GameObject s = Instantiate(evacShipPrefab, spawnPos, Quaternion.identity);
                if (s.GetComponent<EvacShip>() == null) s.AddComponent<EvacShip>();
            }
        }

        var mc = FindActiveMeteor();
        if (mc != null)
        {
            mc.lastChanceUsed = true;
            Debug.Log("[PowerUp] Last chance used; marked meteor.lastChanceUsed = true.");
        }
    }

    // -----------------------
    // Helpers
    // -----------------------
    MeteorController3D FindActiveMeteor()
    {
        // 1) prefer meteor referenced by GameManager if available
        var gm = FindGameManagerInstance();
        if (gm != null)
        {
            var mobj = GetFieldOrPropValue(gm, "meteor");
            if (mobj is GameObject go)
            {
                var mc = go.GetComponent<MeteorController3D>();
                if (mc != null) return mc;
            }
            else if (mobj is Component comp)
            {
                var mc = comp.GetComponent<MeteorController3D>();
                if (mc != null) return mc;
            }
        }

        // 2) fallback robust: Resources.FindObjectsOfTypeAll<T>() (compatível e sem warning)
        try
        {
            var arr = Resources.FindObjectsOfTypeAll(typeof(MeteorController3D));
            if (arr != null && arr.Length > 0)
            {
                foreach (var o in arr)
                {
                    if (o is MeteorController3D mc)
                    {
                        // prefer instance in scene and active, but accept any
                        if (mc.gameObject.scene.IsValid() && mc.gameObject.activeInHierarchy) return mc;
                    }
                }
                // if none active in scene, return first found
                foreach (var o in arr)
                {
                    if (o is MeteorController3D mc) return mc;
                }
            }
        }
        catch { }

        return null;
    }

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

    void TryInvokeMethod(object target, string methodName)
    {
        if (target == null) return;
        var m = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (m != null)
        {
            try { m.Invoke(target, null); }
            catch (Exception ex) { Debug.LogWarning($"[PowerUp] Failed to invoke {methodName} on GameManager: {ex.Message}"); }
        }
    }
}
