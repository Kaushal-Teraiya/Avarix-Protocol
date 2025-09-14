using UnityEngine;
using TMPro;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;

    public void SetHealth(int health)
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + health.ToString();
        }
        else
        {
            Debug.LogError("HealthText reference is missing in HealthUI!");
        }
    }
}
