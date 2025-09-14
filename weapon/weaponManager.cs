using System.Collections;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class weaponManager : NetworkBehaviour
{
    [SerializeField]
    private playerWeapon primaryWeapon;
    private playerWeapon currentWeapon;

    [SerializeField]
    private string weaponLayerName = "Weapon";

    [SerializeField]
    private Transform weaponHolder;

    [SerializeField]
    private GameObject handsObject; // The root parent (e.g., "Hands")

    private string WeaponName;
    private weaponGraphics currentGraphics;

    private Dictionary<string, Quaternion> savedRotations = new Dictionary<string, Quaternion>(); // Dictionary to store bone rotations

    void Start()
    {
        EquipWeapon(primaryWeapon);
        WeaponName = currentWeapon.GunName;
    }

    public string GunNameForSoundFX()
    {
        if (string.IsNullOrEmpty(WeaponName))
        {
            Debug.LogError("GunNameForSoundFX: WeaponName is NULL or EMPTY!");
            return "UnknownGun";
        }
        return WeaponName;
    }

    public playerWeapon GetcurrentWeapon() => currentWeapon;

    public weaponGraphics GetcurrentGraphics() => currentGraphics;

    void EquipWeapon(playerWeapon _weapon)
    {
        currentWeapon = _weapon;
        WeaponName = currentWeapon.GetGunName();

        GameObject _weaponINS = Instantiate(
            _weapon.graphics,
            weaponHolder.position,
            weaponHolder.rotation
        );
        _weaponINS.transform.SetParent(weaponHolder);

        currentGraphics = _weaponINS.GetComponent<weaponGraphics>();
        if (currentGraphics == null)
        {
            Debug.LogError("No weapon graphics component on the weapon object: " + _weaponINS.name);
        }

        if (isLocalPlayer)
        {
            Util.SetLayerRecursively(_weaponINS, LayerMask.NameToLayer(weaponLayerName));
        }

        StartCoroutine(EquipWeaponDelayed(_weaponINS)); // Start delayed process
        Debug.Log("Equipped Weapon: " + WeaponName);
    }

    // Delay for the weapon setup and transform adjustments
    private IEnumerator EquipWeaponDelayed(GameObject weaponObject)
    {
        if (handsObject == null)
        {
            Debug.LogWarning(
                "HandsObject not found maybe it is not the player Survivor, if it is player survivor then make sure the handsObject is assigned in the inspector"
            );
            yield break;
        }
        // Step 1: Save all rotations
        SaveHandRotations();

        // Step 2: Delay before modifying the handsObject's transform
        yield return new WaitForSeconds(2f); // Delay 2 seconds (adjust as needed)

        // Step 3: Parent handsObject to the weapon
        handsObject.transform.SetParent(weaponObject.transform, true);

        // Step 4: Delay before restoring the rotations and applying armature transform
        yield return new WaitForSeconds(0.2f); // Delay 2 seconds (adjust as needed)

        // Step 5: Restore rotations for each part of the hands
        RestoreHandRotations();

        // Step 6: Apply armature rotation and position adjustments
        ApplyArmatureTransform();
    }

    // Save all the relevant rotations of the hands (and other parts)
    private void SaveHandRotations()
    {
        if (handsObject == null)
        {
            Debug.LogWarning("Hands object is not assigned!");
            return;
        }

        // Log to confirm handsObject is found
        Debug.Log("Hands object found: " + handsObject.name);
        LogAllChildTransforms(handsObject.transform);

        // Attempt to find the "spine2" and navigate the hierarchy
        Transform spine2 = handsObject.transform.Find("mixamorig:Spine2");
        if (spine2 == null)
        {
            Debug.LogWarning("mixamorig:Spine2 not found in handsObject!");
            return;
        }

        // Left side
        Transform leftShoulder = spine2.Find("mixamorig:LeftShoulder");
        if (leftShoulder == null)
        {
            Debug.LogWarning("mixamorig:LeftShoulder not found in Spine2!");
            return;
        }

        Transform leftArm = leftShoulder.Find("mixamorig:LeftArm");
        if (leftArm == null)
        {
            Debug.LogWarning("mixamorig:LeftArm not found in LeftShoulder!");
            return;
        }

        Transform leftForearm = leftArm.Find("mixamorig:LeftForearm");
        if (leftForearm == null)
        {
            Debug.LogWarning("mixamorig:LeftForearm not found in LeftArm!");
            return;
        }

        Transform leftHand = leftForearm.Find("mixamorig:LeftHand");
        if (leftHand == null)
        {
            Debug.LogWarning("mixamorig:LeftHand not found in LeftForearm!");
            return;
        }

        // Save rotations for the left side bones
        savedRotations["mixamorig:LeftHand"] = leftHand.localRotation;
        savedRotations["mixamorig:LeftForearm"] = leftForearm.localRotation;
        savedRotations["mixamorig:LeftArm"] = leftArm.localRotation;
        savedRotations["mixamorig:LeftShoulder"] = leftShoulder.localRotation;
        savedRotations["mixamorig:Spine2"] = spine2.localRotation;

        // Right side
        Transform rightShoulder = spine2.Find("mixamorig:RightShoulder");
        if (rightShoulder == null)
        {
            Debug.LogWarning("mixamorig:RightShoulder not found in Spine2!");
            return;
        }

        Transform rightArm = rightShoulder.Find("mixamorig:RightArm");
        if (rightArm == null)
        {
            Debug.LogWarning("mixamorig:RightArm not found in RightShoulder!");
            return;
        }

        Transform rightForearm = rightArm.Find("mixamorig:RightForearm");
        if (rightForearm == null)
        {
            Debug.LogWarning("mixamorig:RightForearm not found in RightArm!");
            return;
        }

        Transform rightHand = rightForearm.Find("mixamorig:RightHand");
        if (rightHand == null)
        {
            Debug.LogWarning("mixamorig:RightHand not found in RightForearm!");
            return;
        }

        // Save rotations for the right side bones
        savedRotations["mixamorig:RightHand"] = rightHand.localRotation;
        savedRotations["mixamorig:RightForearm"] = rightForearm.localRotation;
        savedRotations["mixamorig:RightArm"] = rightArm.localRotation;
        savedRotations["mixamorig:RightShoulder"] = rightShoulder.localRotation;

        // Log the saved rotations
        Debug.Log("Hand rotations saved.");
    }

    // Restore the saved rotations
    private void RestoreHandRotations()
    {
        if (handsObject == null || savedRotations.Count == 0)
        {
            Debug.LogWarning("No saved rotations to restore!");
            return;
        }

        Transform armature = handsObject.transform.Find("Armature");
        if (armature != null)
        {
            foreach (var boneRotation in savedRotations)
            {
                Transform bone = armature.Find(boneRotation.Key);
                if (bone != null)
                {
                    bone.localRotation = boneRotation.Value;
                    Debug.Log($"Restored rotation for {boneRotation.Key}");
                }
                else
                {
                    Debug.LogWarning($"Bone {boneRotation.Key} not found in armature.");
                }
            }
        }
        else
        {
            Debug.LogWarning("Armature not found!");
        }

        Debug.Log("Hand rotations restored.");
    }

    // Apply the final transform to the armature
    private void ApplyArmatureTransform()
    {
        Transform armature = handsObject.transform.Find("Armature");
        if (armature != null)
        {
            // Apply the final rotation and position adjustment to the armature
            //  armature.localPosition = new Vector3(-0.25f, 0.3f, -0.75f);
            //  armature.localRotation = Quaternion.Euler(0f, 83.72f, 0f);
            armature.localPosition = new Vector3(0f, 0f, 0f);
            armature.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Debug.Log("Armature transform applied.");
        }
        else
        {
            Debug.LogWarning("Armature not found!");
        }
    }

    private void LogAllChildTransforms(Transform parent, string indent = "")
    {
        foreach (Transform child in parent)
        {
            Debug.Log(indent + child.name);
            LogAllChildTransforms(child, indent + "  ");
        }
    }
}
