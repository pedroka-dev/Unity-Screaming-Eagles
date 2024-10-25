using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionDestroySelf : MonoBehaviour
{
    //This script only makes the explosion destroy itself
    //All explosion behaviour is on player controller

    private Animator animator;
    private AudioSource audioSource;
    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        audioSource.Play();
    }

    void Update()
    {
        //Hides the explosion once animation has finished
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1 && !animator.IsInTransition(0))
        {
            gameObject.transform.localScale = new Vector3(0, 0, 0);
            gameObject.layer = 0;
        }

        //Destroy the object permanetly once the explosion audio has finished
        if (!audioSource.isPlaying)
        {
            Destroy(gameObject);
        }
    }
}
