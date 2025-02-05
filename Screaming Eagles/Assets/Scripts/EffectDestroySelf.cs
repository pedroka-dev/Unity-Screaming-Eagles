using UnityEngine;

public class EffectDestroySelf : MonoBehaviour
{
    private Animator animator;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1 && !animator.IsInTransition(0))     //Makes an effect destroy itself after the animation ends
        {
            spriteRenderer.enabled = false;
            if(audioSource == null || !audioSource.isPlaying)
                Destroy(gameObject);
        }
    }
}
