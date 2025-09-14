using UnityEngine;
using Mirror;
public class EnablePlayerNameOnAim : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float aimRange =100;
    [SerializeField] private LayerMask playerLayer; // Assign "Player" layer in Unity

    private SyncPlayerName lastTarget;  // 👈 Keeps track of last aimed player
   // private playerWeapon playerWeapon;
    private void Start() {
      //  playerWeapon = GetComponent<playerWeapon>();

      //  aimRange = playerWeapon.range;
    }
    private void Update()
    {

        if(!isLocalPlayer) return ;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, aimRange, playerLayer))
        {
            SyncPlayerName targetPlayer = hit.collider.GetComponentInParent<SyncPlayerName>();

            if (targetPlayer != null)
            {
                if (lastTarget != targetPlayer)  // 👈 If we're looking at a new player
                {
                    if (lastTarget != null) lastTarget.SetNameVisibility(false); // Hide old name
                    targetPlayer.SetNameVisibility(true);  // Show new name
                    lastTarget = targetPlayer;  // Update last target
                }
            }
        }
        else if (lastTarget != null)  // 👈 If we're NOT looking at any player anymore
        {
            lastTarget.SetNameVisibility(false); // Hide last name
            lastTarget = null;  // Reset last target
        }
    }
}
