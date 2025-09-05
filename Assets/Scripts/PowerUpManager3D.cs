// ==================== PowerUpManager3D.cs ====================
// Arquivo: PowerUpManager3D.cs
// Anexe este script ao GameObject "GameManager"

using UnityEngine;
using UnityEngine.UI;

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

    void InitializePowerUps3D()
    {
        powerUps[0] = new PowerUp3D { name = "Click Multiplier", cost = 10, description = "5x click power" };
        powerUps[1] = new PowerUp3D { name = "Shield", cost = 50, description = "3D Energy Shield", visualEffect = shieldEffect };
        powerUps[2] = new PowerUp3D { name = "Laser", cost = 300, description = "Orbital Laser Platform", visualEffect = laserEffect };
        powerUps[3] = new PowerUp3D { name = "Wormhole", cost = 2000, description = "Spatial Rift Generator", visualEffect = wormholeEffect };
    }

    void SetupShopButtons()
    {
        Button[] shopButtons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);

        for (int i = 0; i < powerUps.Length && i < shopButtons.Length; i++)
        {
            powerUps[i].buyButton = shopButtons[i];
            int index = i;
            powerUps[i].buyButton.onClick.AddListener(() => BuyPowerUp3D(index));
        }
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