using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Collects objects tagged as 'egg' when this GameObject receives a trigger enter.
/// Destroys the egg, increments energy (clamped to maxEnergy) and updates a Scrollbar value.
/// </summary>
public class EnergyController : MonoBehaviour
{
    [Header("Energy")]
    [Tooltip("Maximum energy count (e.g. 5).")]
    public int maxEnergy = 5;
    [Tooltip("Current energy count (read/write for debug).")]
    public int currentEnergy = 0;

    [Header("UI")]
    [Tooltip("Optional Scrollbar to display energy (value = currentEnergy / maxEnergy).")]
    public Scrollbar energyScrollbar;
    
    [Header("Player")]
    [Tooltip("Reference to the player's VRPlayerController to trigger flying. If null, will try to FindObjectOfType on Start.")]
    public VRPlayerController playerController;

    void Start()
    {
        // ensure scrollbar steps match maxEnergy (discrete steps)
        if (energyScrollbar != null)
        {
            energyScrollbar.size = 0f; // start empty
        }

        UpdateUI();

        if (playerController == null)
            playerController = FindObjectOfType<VRPlayerController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        if (other.CompareTag("Egg"))
        {
            Destroy(other.gameObject);
            currentEnergy = Mathf.Clamp(currentEnergy + 1, 0, maxEnergy);
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (energyScrollbar != null)
        {
            // represent energy by the scrollbar's size (fill amount) instead of the value/position
            float size = (maxEnergy > 0) ? (float)currentEnergy / (float)maxEnergy : 0f;
            energyScrollbar.size = Mathf.Clamp01(size);
        }
    }

    public void ResetEnergy()
    {
        currentEnergy = 0;
        UpdateUI();
    }

    /// <summary>
    /// Consume 3 energy and trigger player flying for 5 seconds.
    /// Returns true if cast succeeded.
    /// </summary>
    public void CastEnergy()
    {
        const int cost = 3;
        const float flyDuration = 5f;

        if (currentEnergy < cost)
        {
            Debug.Log("Not enough energy to cast.");
            return;
        }

        if (playerController == null)
        {
            Debug.LogWarning("PlayerController not assigned - cannot cast energy.");
            return;
        }

        currentEnergy = Mathf.Clamp(currentEnergy - cost, 0, maxEnergy);
        UpdateUI();
        Debug.Log("CastEnergy: consumed " + cost + " energy. Remaining: " + currentEnergy);
        playerController.CastEnergyFly(flyDuration);
        return;
    }
}
