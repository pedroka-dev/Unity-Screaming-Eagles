using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Tilemaps;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rb;

    //Movement
    [SerializeField] private float movementSpeed = 8f;
    [SerializeField] private float groundDrag = 1f;
    private float horizontalInput;
    private bool isFacingRight = true;

    //Jumping
    [SerializeField] private float jumpingPower = 16f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    
    private bool isJumping;
    private float coyoteTime = 0.2f;
    private float coyoteTimeCounter;
    private float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;

    //Aiming
    [SerializeField] private Camera PlayerCamera;
    [SerializeField] private GameObject spawnedRocket;

    //Rocket Junp 
    [SerializeField] private float explosionKnockback = 20f;
    [SerializeField] private float airControlFactor = 0.5f;
    [SerializeField] private float rocketJumpBonusFactor = 1.5f;


    private bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, 0.25f, groundLayer);

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        HandlePlayerMovementInput();
        HandlePlayerJump();
        HandlePlayerAim();
    }

    private void FixedUpdate()
    {
        HandlePlayerMovementPhysics();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Explosion"))
        {
            HandleReceiveExplosion(collision.transform.position);
        }
    }

    private void HandlePlayerMovementInput()
    {
        //if(allowMovement)  
        horizontalInput = Input.GetAxisRaw("Horizontal");   //This gets absolute values like -1, 0 or 1, with no gradual increase like Input.GetAxis
        FlipCharacter();
        //}
    }

    private void HandlePlayerMovementPhysics()
    {
        var currentVelocity = rb.velocity.x;
        float targetVelocity = horizontalInput * movementSpeed;

        if (IsGrounded() && (horizontalInput == 0 || Math.Abs(currentVelocity) > movementSpeed))
        {
            // Apply drag to slow the player when there's no input or the velocity exceeds max speed
            rb.drag = groundDrag;
        }
        else
        {
            // Reset drag when moving or in air
            rb.drag = 0;

            if (!IsGrounded())      //todo: fix trying to change direction on air
            {
                targetVelocity *= airControlFactor; // For reducing air control
            }

            if (horizontalInput != 0)
            {
                // Calculate the force needed to achieve the target velocity
                float velocityChange = targetVelocity - currentVelocity;
                float force = velocityChange * rb.mass / Time.fixedDeltaTime;

                // Apply force but cap it to prevent going over the movementSpeed
                if (Math.Abs(currentVelocity) < movementSpeed || Math.Sign(targetVelocity) != Math.Sign(currentVelocity))
                {
                    rb.AddForce(new Vector2(force, 0));
                }
            }
        }
    }

    private void HandlePlayerJump()
    {
        // Coyote time allows player to jump a brief moment after being in the air
        if (IsGrounded())
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump buffer allows player to jump for a brief moment before touching the ground
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Check if the player can jump (coyote time and jump buffer)
        if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f && !isJumping)
        {
            //rb.drag = 0;
            rb.AddForce(new Vector2(0f, jumpingPower), ForceMode2D.Impulse); // Using AddForce for jumping

            jumpBufferCounter = 0f;

            StartCoroutine(JumpCooldown());
        }

        // Allow for a "variable jump height" by reducing upward velocity when the player releases the jump button
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.AddForce(new Vector2(0f, -rb.velocity.y * 0.5f), ForceMode2D.Impulse); // Smooth deceleration when releasing the jump
            coyoteTimeCounter = 0f;
        }
    }

    private void HandlePlayerAim()
    {
        Vector2 mousePosition = PlayerCamera.ScreenToWorldPoint(Input.mousePosition);
        if(Input.GetMouseButtonDown(0))
        {
            //if(ItemEquiped == RocketJumper) {
            ShootRocket(mousePosition);
        }
    }

    private void ShootRocket(Vector2 mousePosition)
    {
        float angle = Vector2.SignedAngle(Vector2.right, rb.position - mousePosition);
        Instantiate(spawnedRocket, rb.position, Quaternion.Euler(0, 0, angle + 90));
    }

    private IEnumerator JumpCooldown()
    {
        isJumping = true;
        yield return new WaitForSeconds(0.4f);
        isJumping = false;
    }
    

    private void FlipCharacter()
    {
        if (isFacingRight && horizontalInput < 0f || !isFacingRight && horizontalInput > 0f)
        {
            Vector2 localScale = transform.localScale;
            isFacingRight = !isFacingRight;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private void HandleReceiveExplosion(Vector2 explosionCenter)
    {
        Vector2 direction = rb.position - explosionCenter;
        direction.Normalize();  // Normalize the direction vector to get only the direction, ignoring magnitude
        Vector2 knockbackForce = direction * explosionKnockback;
        if (!IsGrounded())
        {
            knockbackForce *= rocketJumpBonusFactor; //adds bonus knockback if already in air
        }

        // Apply the force to the Rigidbody
        rb.AddForce(knockbackForce, ForceMode2D.Impulse);
    }
}