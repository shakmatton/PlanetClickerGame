using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class ShopItemData
{
    public string id;
    public string displayName;
    public int cost = 50;
    public GameObject effectPrefab;    // prefab visual/efeito aplicado quando comprar
    public int amount = 1;             // quantidade (se aplicável)
    // OPTIONAL: icon, description, cooldown, etc.
}

[Serializable]
public class ShopItemUI
{
    public ShopItemData data;
    public Button buyButton;               // referência do botão (arrastar no Inspector)
    public TextMeshProUGUI nameText;       // texto do nome do item
    public TextMeshProUGUI priceText;      // texto do preço
    public Image iconImage;                // opcional
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("UI")]
    public RectTransform shopPanel;                // painel que fica à esquerda (opcional)
    public Transform itemsParent;                  // parent que contém os item UIs (Vertical Layout)
    public List<ShopItemUI> items = new List<ShopItemUI>();

    [Header("Fallback (if no GameManager present)")]
    public int localClicks = 0;                    // fallback click count if no GameManager
    public bool pollEveryFrame = true;             // se true: atualiza shop por polling

    // internal cache
    int cachedClicks = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // setup button listeners and texts
        for (int i = 0; i < items.Count; i++)
        {
            var ui = items[i];
            if (ui == null || ui.data == null) continue;

            if (ui.nameText != null) ui.nameText.text = ui.data.displayName;
            if (ui.priceText != null) ui.priceText.text = ui.data.cost.ToString();

            if (ui.buyButton != null)
            {
                int idx = i;
                ui.buyButton.onClick.RemoveAllListeners();
                ui.buyButton.onClick.AddListener(() => OnBuyButtonClicked(idx));
            }
        }

        // initial update
        UpdateShopUI(true);
    }

    void Update()
    {
        if (!pollEveryFrame) return;

        int current = GetCurrentClicks();
        if (current != cachedClicks)
        {
            cachedClicks = current;
            UpdateShopUI(false);
        }
    }

    // Public API: if you want another script to notify clicks changed, call this
    public void NotifyClicksChanged()
    {
        cachedClicks = GetCurrentClicks();
        UpdateShopUI(false);
    }

    // Get clicks value from GameManager3D if present, with reflection fallback to several likely field names.
    int GetCurrentClicks()
    {
        // Try to use GameManager3D.Instance if present
        var gmType = FindGameManagerInstance();
        if (gmType != null)
        {
            object gmInstance = gmType;
            // try common field names
            Type t = gmInstance.GetType();
            FieldInfo f;
            f = t.GetField("totalClicks", BindingFlags.Public | BindingFlags.Instance);
            if (f == null) f = t.GetField("clicks", BindingFlags.Public | BindingFlags.Instance);
            if (f == null) f = t.GetField("Clicks", BindingFlags.Public | BindingFlags.Instance);
            if (f != null)
            {
                try
                {
                    object val = f.GetValue(gmInstance);
                    if (val is int) return (int)val;
                    if (val is long) return (int)(long)val;
                }
                catch { }
            }

            // try property names
            PropertyInfo p = t.GetProperty("totalClicks", BindingFlags.Public | BindingFlags.Instance)
                ?? t.GetProperty("clicks", BindingFlags.Public | BindingFlags.Instance)
                ?? t.GetProperty("Clicks", BindingFlags.Public | BindingFlags.Instance);
            if (p != null)
            {
                try
                {
                    object val = p.GetValue(gmInstance);
                    if (val is int) return (int)val;
                    if (val is long) return (int)(long)val;
                }
                catch { }
            }
        }

        // fallback to localClicks
        return localClicks;
    }

    // Update UI values and interactability
    void UpdateShopUI(bool forceUpdateAll)
    {
        int clicks = GetCurrentClicks();
        for (int i = 0; i < items.Count; i++)
        {
            var ui = items[i];
            if (ui == null || ui.data == null) continue;

            if (ui.priceText != null) ui.priceText.text = ui.data.cost.ToString();

            bool affordable = clicks >= ui.data.cost;
            if (ui.buyButton != null) ui.buyButton.interactable = affordable;

            // Optionally update visuals: disable greyscale, change text color, etc.
        }
    }

    // Called when player taps buy
    void OnBuyButtonClicked(int index)
    {
        if (index < 0 || index >= items.Count) return;
        var item = items[index].data;
        if (item == null) return;

        // Try to spend clicks via GameManager method TrySpendClicks(int) if it exists
        bool spent = TrySpendClicksViaGameManager(item.cost);
        if (!spent)
        {
            // Fallback: spend local clicks
            if (localClicks >= item.cost)
            {
                localClicks -= item.cost;
                spent = true;
            }
        }

        if (!spent)
        {
            Debug.Log("ShopManager: not enough clicks to buy " + item.displayName);
            return;
        }

        // Apply purchased effect
        ApplyPurchaseEffect(item);
        // Refresh UI
        UpdateShopUI(true);
        // If GameManager exists, let it know (optional) by calling NotifyClicksChanged there or just depend on GameManager's UI
    }

    // Attempts to find GameManager3D.Instance via reflection (safe)
    object FindGameManagerInstance()
    {
        // First try the known type name directly (if compiled)
        var gmType = Type.GetType("GameManager3D");
        if (gmType != null)
        {
            // static Instance field?
            FieldInfo instField = gmType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instField != null)
            {
                var inst = instField.GetValue(null);
                if (inst != null) return inst;
            }

            // property Instance?
            PropertyInfo instProp = gmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instProp != null)
            {
                var inst = instProp.GetValue(null);
                if (inst != null) return inst;
            }
        }

        // If not found by direct type, try searching all loaded assemblies for a type named GameManager3D
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t = asm.GetType("GameManager3D");
                if (t == null) continue;
                FieldInfo instField = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instField != null)
                {
                    var inst = instField.GetValue(null);
                    if (inst != null) return inst;
                }
                PropertyInfo instProp = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instProp != null)
                {
                    var inst = instProp.GetValue(null);
                    if (inst != null) return inst;
                }
            }
            catch { }
        }

        return null;
    }

    // Tries to invoke a method TrySpendClicks(int) on GameManager if present
    bool TrySpendClicksViaGameManager(int amount)
    {
        object gm = FindGameManagerInstance();
        if (gm == null) return false;

        Type t = gm.GetType();
        MethodInfo mi = t.GetMethod("TrySpendClicks", BindingFlags.Public | BindingFlags.Instance);
        if (mi != null)
        {
            try
            {
                object result = mi.Invoke(gm, new object[] { amount });
                if (result is bool) return (bool)result;
            }
            catch (Exception ex) { Debug.LogWarning("ShopManager: exception invoking TrySpendClicks: " + ex.Message); }
        }

        // fallback: try to deduct a public int field (dangerous, but try common names)
        FieldInfo f = t.GetField("totalClicks", BindingFlags.Public | BindingFlags.Instance)
                    ?? t.GetField("clicks", BindingFlags.Public | BindingFlags.Instance);
        if (f != null)
        {
            try
            {
                object val = f.GetValue(gm);
                if (val is int)
                {
                    int cur = (int)val;
                    if (cur >= amount)
                    {
                        f.SetValue(gm, cur - amount);
                        return true;
                    }
                }
            }
            catch { }
        }

        return false;
    }

    void ApplyPurchaseEffect(ShopItemData item)
    {
        // If there's an effectPrefab, instantiate it parented to planet if available (visual feedback)
        if (item.effectPrefab != null)
        {
            // try to parent to GameManager3D.Instance.planet if available
            object gm = FindGameManagerInstance();
            Transform parent = null;
            if (gm != null)
            {
                Type t = gm.GetType();
                FieldInfo planetField = t.GetField("planet", BindingFlags.Public | BindingFlags.Instance)
                    ?? t.GetField("Planet", BindingFlags.Public | BindingFlags.Instance);
                if (planetField != null)
                {
                    object planetObj = planetField.GetValue(gm);
                    if (planetObj is GameObject)
                    {
                        parent = ((GameObject)planetObj).transform;
                    }
                }
            }

            if (parent != null)
                Instantiate(item.effectPrefab, parent);
            else
                Instantiate(item.effectPrefab);
        }

        // Optionally: call other managers (PowerUpManager3D) — not implemented here to keep it generic.
        Debug.Log($"ShopManager: purchased {item.displayName} x{item.amount}");
    }
}
