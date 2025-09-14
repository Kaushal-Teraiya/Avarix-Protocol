using UnityEngine;

public class HPbarLookAtCam : MonoBehaviour
{

    private Camera lookCamHP;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lookCamHP = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
    

        if (lookCamHP == null) 
            lookCamHP = Camera.main; 

        if (lookCamHP != null)
        {
        
            transform.LookAt(lookCamHP.transform);
            transform.Rotate(0, 0, 0); // Make sure it faces correctly
        }
   
    }
}
