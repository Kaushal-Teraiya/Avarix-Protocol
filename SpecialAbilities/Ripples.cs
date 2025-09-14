using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class SpawnShieldRipples : MonoBehaviour
{
    public GameObject shieldRipples; // Prefab for the ripple effect

    private VisualEffect shieldRipplesVFX;

    private void OnCollisionEnter(Collision co)
    {
        if (co.gameObject.tag == "Bullet")
        {
            // Spawn the ripple effect
            var ripples = Instantiate(shieldRipples, transform);
            shieldRipplesVFX = ripples.GetComponent<VisualEffect>();

            // Get contact point and normal
            ContactPoint contact = co.contacts[0];
            Vector3 localHitPoint = transform.InverseTransformPoint(contact.point); // Convert to local space
            Vector3 localNormal = transform.InverseTransformDirection(contact.normal); // Convert normal to local space

            // Apply a slight inward offset along the normal to correct the position
            localHitPoint += localNormal * -0.2f; // Adjust this value if needed

            // Set the corrected position for the VFX
            shieldRipplesVFX.SetVector3("SphereCenter", localHitPoint);

            // Destroy the ripple effect after 2 seconds
            Destroy(ripples, 2);
        }
    }

    public void TriggerRippleEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (shieldRipples == null)
        {
            Debug.LogError("Shield ripple prefab is not assigned!");
            return;
        }

        // Spawn the ripple effect only at the correct location
        var ripples = Instantiate(shieldRipples, transform);
        shieldRipplesVFX = ripples.GetComponent<VisualEffect>();

        if (shieldRipplesVFX == null)
        {
            Debug.LogError("VisualEffect component not found on ripple prefab!");
            return;
        }

        // Convert hit point to local space of the shield
        Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);
        Vector3 localNormal = transform.InverseTransformDirection(hitNormal);

        localHitPoint += localNormal * -0.2f; // Adjust position slightly inwards

        // Set the VFX parameter
        shieldRipplesVFX.SetVector3("SphereCenter", localHitPoint);
        shieldRipplesVFX.Play(); // Ensure the effect actually starts playing

        Debug.Log("Ripple effect triggered at: " + localHitPoint);

        Destroy(ripples, 2f);
    }
}
