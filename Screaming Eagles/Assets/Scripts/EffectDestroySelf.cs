using UnityEngine;

public class EffectDestroySelf : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1 && !animator.IsInTransition(0))     //Makes an effect destroy itself after the animation ends
        {
            Destroy(gameObject);
        }
    }
}
