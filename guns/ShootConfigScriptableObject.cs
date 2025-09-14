using UnityEngine;

[CreateAssetMenu(fileName = "Shoot Config" , menuName = "Guns/ShootConfiguration" , order = 2)]
public class ShootConfigScriptableObject : ScriptableObject
{
   public LayerMask hitMask;
   public Vector3 spread = new Vector3(0.1f , 0.1f , 0.1f);
   public float FireRate = 0.25f;
}
