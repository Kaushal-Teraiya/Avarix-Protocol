using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAttacherWithDelay : MonoBehaviour
{
    [Header("Weapon Holder Path")]
    public string weaponHolderPath = "Camera/WeaponHolder"; // Adjust if needed

    [Header("Offset After Parenting")]
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    private Transform weapon;

    // private bool isAttached = false;
    private Dictionary<string, Vector3> characterHandScales = new Dictionary<string, Vector3>()
    {
        { "Ragdoll_Eve", new Vector3(0.886f, 0.886f, 0.886f) },
        { "Ragdoll_Dreyar", new Vector3(0.277f, 0.277f, 0.277f) },
        { "Ragdoll_Vampire", new Vector3(3.77f, 3.77f, 3.77f) },
        { "Ragdoll_Knight", new Vector3(0.83999f, 0.83999f, 0.83999f) },
        { "Ragdoll_Archer", new Vector3(1, 1, 1) },
        { "Ragdoll_Pumpkin", new Vector3(0.07f, 0.07f, 0.07f) },
        { "Ragdoll_Ninja", new Vector3(0.47f, 0.47f, 0.47f) },
    };

    void Start()
    {
        StartCoroutine(WaitAndAttach());
    }

    // private IEnumerator WaitAndAttach()
    // {
    //     // Wait until the weapon appears under the WeaponHolder
    //     while (weapon == null)
    //     {
    //         Transform holder = GameObject.Find(weaponHolderPath)?.transform;

    //         if (holder != null && holder.childCount > 0)
    //         {
    //             weapon = holder.GetChild(0); // Assumes weapon is first child
    //         }

    //         yield return new WaitForSeconds(0.1f); // Keep checking
    //     }

    //     // Now attach the hands to the found weapon
    //     transform.SetParent(weapon, false);
    //     yield return new WaitForSeconds(0.1f); // Let the parent apply

    //     // transform.localPosition = positionOffset;
    //     // transform.localRotation = Quaternion.Euler(rotationOffset);
    //     transform.localScale = new Vector3(0.277f, 0.277f, 0.277f);

    //     isAttached = true;
    //     Debug.Log("Hands attached to " + weapon.name + " with offset.");
    // }

    private IEnumerator WaitAndAttach()
    {
        while (weapon == null)
        {
            Transform holder = GameObject.Find(weaponHolderPath)?.transform;
            if (holder != null && holder.childCount > 0)
            {
                weapon = holder.GetChild(0);
            }

            yield return new WaitForSeconds(0.1f);
        }

        transform.SetParent(weapon, false);
        yield return new WaitForSeconds(0.1f);

        // Find child that starts with "Ragdoll_"
        Transform ragdoll = null;
        foreach (Transform child in transform.root)
        {
            if (child.name.StartsWith("Ragdoll_"))
            {
                ragdoll = child;
                break;
            }
        }

        if (ragdoll != null)
        {
            string characterName = ragdoll.name; // Full name like "Ragdoll_Dreyar"

            if (characterHandScales.TryGetValue(characterName, out Vector3 scale))
            {
                transform.localScale = scale;
                Debug.Log(
                    $"[HandAttacher] Detected character '{characterName}', set scale to {scale}"
                );
            }
            else
            {
                transform.localScale = Vector3.one;
                Debug.LogWarning(
                    $"[HandAttacher] No scale mapping found for '{characterName}'. Using default."
                );
            }
        }
        else
        {
            transform.localScale = Vector3.one;
            Debug.LogError("[HandAttacher] No Ragdoll_ object found under player root.");
        }

        //isAttached = true;
    }
}
