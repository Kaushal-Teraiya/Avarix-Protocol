using UnityEngine;
using Mirror;

public class Flag : NetworkBehaviour
{
  
 public Vector3 originalPosition;
 
 //public Vector3 basePos = new Vector3();

   

    void Start()
    {
        originalPosition = transform.position;
    }

                                                                                            
}


