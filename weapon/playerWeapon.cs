using UnityEngine;

[System.Serializable]
public class playerWeapon
{
    public string GunName;
    public int damage = 10;
    public float range = 100f;
    public GameObject graphics;
    public float FireRate = 0f;
    public int currentAmmo;
    public int maxAmmo;
    public int reloadDelay;
    public int impactForce = 30;

    public string GetGunName()
    {
        if (GunName == null)
        {
            Debug.Log("gun name is null");
        }
        return GunName;
    }
}
