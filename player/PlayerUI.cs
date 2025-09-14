using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(playerController))]
public class PlayerUI : MonoBehaviour
{
    [SerializeField] private RectTransform thrusterFuelFill;
    [SerializeField] private RectTransform sprintFuelFill;
    [SerializeField] public TMP_Text healthText; // ✅ Add this for health UI

    private playerController controller;

    public void setController(playerController _controller)
    {
        controller = _controller;
    }

    void Update()
    {
        SetThrusterFuelAmount(controller.getThrusterFuelAmount());
        SetSprintFuelAmount(controller.getSprintFuelAmount());
    }

    void SetThrusterFuelAmount(float _amount)
    {
        thrusterFuelFill.localScale = new Vector3(1f, _amount, 1f);
    }

    void SetSprintFuelAmount(float _amount)
    {
        sprintFuelFill.localScale = new Vector3(1f , _amount , 1f);
    }

    // ✅ Method to update the player's health text
    public void SetHealth(int health)
    {
        if (healthText != null)
        {
            healthText.text = health.ToString();
            healthText.ForceMeshUpdate();

            Debug.Log($"Health text updated: {health}"); // ✅ Check if this runs
        }
        else
        {
            Debug.LogError("HealthText reference is NULL in PlayerUI!");
        }
    }
}
