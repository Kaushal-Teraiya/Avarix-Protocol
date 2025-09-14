using UnityEngine;

public class HealthDropTest : MonoBehaviour
{

    public GameObject HealthDrop;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Instantiate(HealthDrop , transform.position + new Vector3(0f,0f,2f) ,transform.rotation);
            
        }
    }
}
