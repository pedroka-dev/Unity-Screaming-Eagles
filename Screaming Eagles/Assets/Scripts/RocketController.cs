using UnityEngine;

public class RocketController : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private GameObject spawnedExplosion;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ApplyVelocity();
        Destroy(gameObject, 30f);   //Destroys the rocket if doest explodes 30sec, to avoid performance issues
    }

    void ApplyVelocity()    //Gives constant velocity based on the initial rocket rotation angle at Z axis
    {
        float radians = (rb.transform.eulerAngles.z) * Mathf.Deg2Rad;
        Vector2 velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * projectileSpeed;
        rb.velocity = velocity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))       //todo: collision with mercs
        {
            Quaternion randomZRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            Instantiate(spawnedExplosion, rb.position, randomZRotation);
            Destroy(gameObject);
        }
    }
}
