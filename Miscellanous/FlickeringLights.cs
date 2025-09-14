using UnityEngine;
using System.Collections;

public class LightFlicker : MonoBehaviour
{
    private Light lightSource;
    public float flickerMinTime = 0.002f;
    public float flickerMaxTime = 0.5f;

    void Start()
    {
        lightSource = GetComponent<Light>();
        StartCoroutine(FlickerEffect());
    }

    IEnumerator FlickerEffect()
    {
        while (true)
        {
            lightSource.enabled = !lightSource.enabled;
            yield return new WaitForSeconds(Random.Range(flickerMinTime, flickerMaxTime));
        }
    }
}
