using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PowerUpManager3D - gerencia powerups 3D e a UI dos botões.
/// - SetupShopButtons: usa Resources.FindObjectsOfTypeAll<Button>() e filtra botões da cena (compatível).
/// - Chama efeitos no MeteorController3D: DeflectMeteor() / DestroyMeteor() / DivertMeteor()
/// </summary>

[System.Serializable]
public class PowerUp3D
{
    public string name;
    public int cost;
    public bool purchased;
    public Button buyButton;
    public string description;
    public GameObject visualEffect;
}

public class PowerUpManager3D : MonoBehaviour
{
    [Header("3D Power-ups")]
    public PowerUp3D[] powerUps = new PowerUp3D[4];

    [Header("3D Power-up Effects")]
    public GameObject shieldEffect;
    public GameObject laserEffect;
    public GameObject wormholeEffect;

    void Start()
    {
        InitializePowerUps3D();
        SetupShopButtons();
        UpdateShop();
    }

    // Substitua por esta implementação (mantém o resto do arquivo)
    void InitializePowerUps3D()
    {
        // Ajuste os custos/nomes conforme sua ordem esperada:
        // index 0 = Multiplicador, 1 = Laser, 2 = Wormhole, 3 = Last Chance
        powerUps[0] = new PowerUp3D { name = "Multiplicador", cost = 10, description = "+5 cliques por compra" };
        powerUps[1] = new PowerUp3D { name = "Laser", cost = 150, description = "Destrói o meteoro atual" };
        powerUps[2] = new PowerUp3D { name = "Wormhole", cost = 200, description = "Teleporta temporariamente o planeta" };
        powerUps[3] = new PowerUp3D { name = "Última Chance", cost = 250, description = "Evacua parte da população (efeito visual)" };
    }

    void SetupShopButtons()
    {
        // Tenta encontrar um painel explicitamente chamado "ItemsContent" (o mesmo que você usou no Canvas)
        GameObject itemsContent = GameObject.Find("ItemsContent");
        if (itemsContent != null)
        {
            // percorre os filhos em ordem (top → bottom conforme Hierarchy)
            int childCount = itemsContent.transform.childCount;
            for (int i = 0; i < Mathf.Min(childCount, powerUps.Length); i++)
            {
                var child = itemsContent.transform.GetChild(i);
                var btn = child.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    powerUps[i].buyButton = btn;
                    int index = i; // captura variável
                    powerUps[i].buyButton.onClick.RemoveAllListeners();
                    powerUps[i].buyButton.onClick.AddListener(() => BuyPowerUp3D(index));
                }
            }
            Debug.Log("[PowerUpManager3D] SetupShopButtons: mapped buttons from ItemsContent children.");
            return;
        }

        // fallback antigo: tenta detectar botões na cena (se você não tiver um ItemsContent com esse nome)
        var allButtons = Resources.FindObjectsOfTypeAll(typeof(Button)) as Button[];
        if (allButtons == null || allButtons.Length == 0)
        {
            Debug.LogWarning("[PowerUpManager3D] No UI Buttons found in scene via Resources.FindObjectsOfTypeAll.");
            return;
        }

        var sceneButtons = new System.Collections.Generic.List<Button>();
        foreach (var b in allButtons)
        {
            if (b == null) continue;
            try
            {
                if (b.gameObject.scene.IsValid() && b.gameObject.activeInHierarchy)
                    sceneButtons.Add(b);
            }
            catch { }
        }

        for (int i = 0; i < powerUps.Length && i < sceneButtons.Count; i++)
        {
            powerUps[i].buyButton = sceneButtons[i];
            int index = i;
            powerUps[i].buyButton.onClick.RemoveAllListeners();
            powerUps[i].buyButton.onClick.AddListener(() => BuyPowerUp3D(index));
        }
        Debug.Log("[PowerUpManager3D] SetupShopButtons: mapped buttons by scene detection (fallback).");
    }


    public void BuyPowerUp3D(int index)
    {
        if (index >= powerUps.Length) return;

        PowerUp3D powerUp = powerUps[index];
        if (GameManager3D.Instance == null)
        {
            Debug.LogWarning("[PowerUpManager3D] GameManager3D.Instance is null. Cannot buy.");
            return;
        }

        if (GameManager3D.Instance.totalClicks >= powerUp.cost && !powerUp.purchased)
        {
            GameManager3D.Instance.totalClicks -= powerUp.cost;
            // try UpdateUI if exists
            try { GameManager3D.Instance.UpdateUI(); } catch { }

            powerUp.purchased = true;
            ApplyPowerUpEffect3D(index);
            UpdateShop();

            Debug.Log($"3D Power-up purchased: {powerUp.name}");
        }
    }

    void ApplyPowerUpEffect3D(int index)
    {
        if (GameManager3D.Instance == null) return;

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
        var meteorObj = GameManager3D.Instance?.meteor;
        var mc = meteorObj != null ? meteorObj.GetComponent<MeteorController3D>() : null;
        if (mc == null) return false;

        if (waveNumber <= 2 && powerUps[1].purchased) // 3D Shield
        {
            Debug.Log("3D Shield deflected meteor!");
            mc.DeflectMeteor();
            return true;
        }

        if (waveNumber <= 3 && powerUps[2].purchased) // 3D Laser
        {
            mc.DestroyMeteor();
            return true;
        }

        if (waveNumber <= 4 && powerUps[3].purchased) // 3D Wormhole
        {
            mc.DivertMeteor();
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
                bool canAfford = (GameManager3D.Instance != null) && (GameManager3D.Instance.totalClicks >= powerUps[i].cost);
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
        // Destroy visual effects (tag-based or object-based cleanup)
        GameObject[] effects = GameObject.FindGameObjectsWithTag("PowerUpEffect");
        foreach (GameObject effect in effects)
        {
            Destroy(effect);
        }

        // Reset purchased status
        for (int i = 0; i < powerUps.Length; i++)
        {
            powerUps[i].purchased = false;
        }

        if (GameManager3D.Instance != null)
        {
            GameManager3D.Instance.clickMultiplier = 1;
        }

        UpdateShop();
    }
}
