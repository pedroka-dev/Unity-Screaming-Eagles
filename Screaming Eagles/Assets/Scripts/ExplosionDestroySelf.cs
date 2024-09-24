using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionDestroySelf : MonoBehaviour
{
    //This script only makes the explosion destroy itself
    //All explosion behaviour is on player controller
    void Update()
    {
        StartCoroutine(DestroySelf(0.1f));
    }

    private IEnumerator DestroySelf(float delaySeconds )
    {
        yield return new WaitForSeconds(delaySeconds);
        Destroy(gameObject);
    }
}
