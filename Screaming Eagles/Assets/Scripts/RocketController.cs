using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketController : MonoBehaviour
{
    private Rigidbody2D rb;
    private ParticleSystem rocketSmokeTrail;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private float autoDetonationFuseTimeout = 10f;
    [SerializeField] private float damage = 0f;
    [SerializeField] private GameObject spawnedExplosion;
    [SerializeField] private LayerMask collisionLayerMask;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rocketSmokeTrail = GetComponent<ParticleSystem>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ApplyVelocity();
        StartCoroutine(DetonateAfterFuseTimeout(autoDetonationFuseTimeout));
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collisionLayerMask.Contains(collision.gameObject.layer))
        {
            Explodes();
        }
    }

    /// <summary>
    /// Gives constant velocity based on the initial rocket projectile angle at Z axis
    /// </summary>
    void ApplyVelocity()
    {
        float radians = (rb.transform.eulerAngles.z) * Mathf.Deg2Rad;
        Vector2 velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * projectileSpeed;
        rb.velocity = velocity;
    }

    private void Explodes()
    {
        rocketSmokeTrail.Stop();
        rb.simulated = false;
        spriteRenderer.enabled = false;
        Quaternion randomZRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        GameObject explosionObject = Instantiate(spawnedExplosion, rb.position, randomZRotation, null);
        SplashDamageMercenaries(explosionObject);
        Destroy(this);
    }

    private void SplashDamageMercenaries(GameObject explosion)  //todo: make compatible with enemy explosions; take responsability out of here
    {
        ContactFilter2D contactFilter = new()
        {
            useTriggers = true
        };
        contactFilter.SetLayerMask(LayerMask.GetMask("Mercenary"));         //TODO: dont make layer hardcoded; allow explosions to affect both players and enemies, besides when its rocket jumper explosion
        contactFilter.useLayerMask = true;

        List<Collider2D> hitEnemies = new();
        int numberOfHits = Physics2D.OverlapCollider(explosion.GetComponent<Collider2D>(), contactFilter, hitEnemies);

        if (numberOfHits > 0)
        {
            foreach (Collider2D enemy in hitEnemies)
            {
                if (enemy.gameObject.TryGetComponent<PlayerController>(out var mercenaryController))
                {
                    mercenaryController.ReceiveExplosion(explosion.transform.localPosition);
                }
            }
        }
    }

    /// <summary>
    /// Courotine for the projectile exploding by itself after the timeout, in seconds.
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    private IEnumerator DetonateAfterFuseTimeout(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Explodes();
    }
}
    
