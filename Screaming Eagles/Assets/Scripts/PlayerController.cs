using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Tilemaps;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rb;

    //Movement
    [SerializeField] private float speed = 8f;
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
    [SerializeField] private GameObject spawnedExplosionFoo;


    private bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, 0.25f, groundLayer);

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        HandlePlayerMovement();
        HandlePlayerJump();
        HandlePlayerAim();
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(horizontalInput * speed, rb.velocity.y);      //Gives player horizontal speed if trying to walk
    }

    private void HandlePlayerMovement()
    {
        //if(allowMovement)  
        horizontalInput = Input.GetAxisRaw("Horizontal");   //This gets absolute values like -1, 0 or 1, with no gradual increase like Input.GetAxis
        FlipCharacter();
        //}
    }

    private void HandlePlayerJump()
    {
        //if(allowMovement)  
        //Coyote time allows player to jump a brief moment after being on air
        if (IsGrounded())
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        //Jump buffer allows player to jump for a brief moment before touching the ground
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f && !isJumping)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);

            jumpBufferCounter = 0f;

            StartCoroutine(JumpCooldown());
        }

        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);

            coyoteTimeCounter = 0f;
        }
        //}
    }

    private void HandlePlayerAim()
    {
        Vector3 mousePosition = PlayerCamera.ScreenToWorldPoint(Input.mousePosition);
        float angle = Vector2.Angle(mousePosition, rb.position);
        Debug.DrawLine(rb.position, mousePosition, Color.yellow, 0.001f);
        mousePosition.z = 0;
        if (Input.GetMouseButtonDown(0))
        {
            Instantiate(spawnedExplosionFoo, mousePosition, new Quaternion());
        }
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
}