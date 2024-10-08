using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionDestroySelf : MonoBehaviour
{
    //This script only makes the explosion destroy itself
    //All explosion behaviour is on player controller

    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Check if the explosion animation has finished and then destroy the object
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1 && !animator.IsInTransition(0))
        {
            Destroy(gameObject);
        }
    }
}
