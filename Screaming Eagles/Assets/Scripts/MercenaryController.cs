using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Controller for both player and NPC mercenaries
//All ingame characters must have one
public class MercenaryController : MonoBehaviour
{
    Rigidbody2D rb;

    //Explosion and knockback
    [SerializeField] private float baseKnockbackPower = 20f; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {

    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.layer == LayerMask.NameToLayer("Explosion"))
        {
            HandleReceiveExplosion(collider.transform.position, 0);
        }
    }


    //On TF2, explosions have a 3 character radius
    //Also, its damage and knoback falls of 100% at the and 50% at the outer layer
    private void HandleReceiveExplosion(Vector2 explosionCenter, int maximunDamage)
    {
        Vector2 direction = rb.position - explosionCenter;
        Debug.DrawLine(rb.position, explosionCenter, Color.green, 1f);
        rb.velocity = new Vector2(direction.x * baseKnockbackPower, direction.y * baseKnockbackPower);
        //rb.AddTorque(direction * baseKnockbackPower, ForceMode2D.Impulse);
        //Debug.DrawLine(rb.position, direction, Color.yellow, 1f);
    }
}
