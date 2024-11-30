using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

enum SelectedWeapon
{
    Primary,
    Seecondary,
    Melee
}

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 8f;
    [SerializeField] private float groundedDrag = 1f;
    [SerializeField] private float airControlFactor = 0.5f;
    private float horizontalInput;

    [Header("Jumping")]
    [SerializeField] private float jumpingPower = 16f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    private bool isJumping;
    private bool canMarketGardenCrit;

    private readonly float marketGardenBufferTime = 0.1f;  // Market Garden buffer makes market gardening input more forgiving and allows bhoping crits
    private float rocketJumpBufferCounter;

    private readonly float coyoteTime = 0.2f;    // Coyote time allows player to jump a brief moment after being in the air
    private float coyoteTimeCounter;

    private readonly float jumpBufferTime = 0.2f;     // Jump buffer allows player to jump for a brief moment before touching the ground
    private float jumpBufferCounter;


    [Header("Aiming & Loadout")]
    [SerializeField] private Camera PlayerCamera;
    [SerializeField] private AudioClip drawPrimary;
    [SerializeField] private AudioClip drawMelee;

    private Vector2 mousePosition;
    private float aimAngle = 360f;  //scale of 0 to 360 always, counter clockwise
    private bool isFacingRight = true;
    private SelectedWeapon currentSelectedWeapon = SelectedWeapon.Primary;

    [Header("Primary Weapon")]
    [SerializeField] private float explosionSelfKnockback = 20f;
    [SerializeField] private float rocketJumpBonusFactor = 1.5f;
    [SerializeField] private GameObject spawnedRocket;
    [SerializeField] private AudioClip shootingRocketAudio;
    [SerializeField] private AudioClip reloadingRocketAudio;
    [SerializeField] private AudioClip clipEmptyAudio;

    private int primaryClipSize = 4;
    private float primaryFirerate = 0.8f;
    private float primaryReloadSpeed = 0.8f;
    //private float primaryDamage = 0f;

    private bool canShootPrimary = true;
    private bool isReloading = false;
    private int currentPrimaryClipContent = 4;

    [Header("Melee Weapon")]
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private GameObject spawnedMeleeAttack;
    [SerializeField] private AudioClip shovelAttackAudio;
    [SerializeField] private AudioClip shovelAttackCritAudio;
    [SerializeField] private AudioClip hitSoundAudio;
    [SerializeField] private List<AudioClip> hitCritAudios;

    private bool canShootMelee = true;
    private float meleeFireRate = 0.8f;
    private float meleeDamage = 65f;

    private bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, 0.25f, groundLayer);
    private AudioSource audioSource;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        HandlePlayerMovementInput();
        HandlePlayerJump();
        HandlePlayerAim();
        HandlePlayerWeapon();
        HandlePlayerLoadout();
    }

    private void FixedUpdate()
    {
        HandlePlayerMovementPhysics();
    }

    #region Movement
    private void HandlePlayerMovementInput()
    {
        //if(allowMovement)  
        horizontalInput = Input.GetAxisRaw("Horizontal");   //This gets absolute values like -1, 0 or 1, with no gradual increase like Input.GetAxis
        //}
    }

    private void HandlePlayerMovementPhysics()
    {
        var currentVelocity = rb.velocity.x;
        float targetVelocity = horizontalInput * movementSpeed;

        if (IsGrounded() && (horizontalInput == 0 || Math.Abs(currentVelocity) > movementSpeed))
        {
            rb.drag = groundedDrag;     // Apply drag to slow the player when there's no input or the velocity exceeds max speed
        }
        else
        {
            rb.drag = 0;    // Disable drag when moving or in air

            if (horizontalInput != 0)
            {
                // Calculate the force needed to achieve the target velocity
                float velocityChange = targetVelocity - currentVelocity;
                float force = velocityChange * rb.mass / Time.fixedDeltaTime;

                if (!IsGrounded())
                {
                    force *= airControlFactor; // Reduces movement control on air
                }

                // Apply movement force but cap it to prevent going over the movementSpeed
                if (Math.Abs(currentVelocity) < movementSpeed || Math.Sign(targetVelocity) != Math.Sign(currentVelocity))
                {
                    rb.AddForce(new Vector2(force, 0));
                }
            }
        }
    }

    private void HandlePlayerJump()
    {
        
        if (IsGrounded())
        {
            coyoteTimeCounter = coyoteTime;

            rocketJumpBufferCounter -= Time.deltaTime;
            if (rocketJumpBufferCounter <= 0f)
            {
                canMarketGardenCrit = false;
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
            rocketJumpBufferCounter = marketGardenBufferTime;
        }   

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Check if the player can jump (considering coyote time and jump buffer)
        if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f && !isJumping)
        {
            rb.AddForce(new Vector2(0f, jumpingPower), ForceMode2D.Impulse);
            jumpBufferCounter = 0f;

            StartCoroutine(JumpCooldown());
        }

        // Allow for a "variable jump height" by reducing upward velocity when the player releases the jump button
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.AddForce(new Vector2(0f, -rb.velocity.y * 0.5f), ForceMode2D.Impulse);   // Smooth deceleration when releasing the jump
            coyoteTimeCounter = 0f;
        }
    }

    private IEnumerator JumpCooldown()
    {
        isJumping = true;
        yield return new WaitForSeconds(0.4f);
        isJumping = false;
    }



    private void CharacterFlip()
    {
        if (isFacingRight && (aimAngle > 95 && aimAngle < 265) || !isFacingRight && (aimAngle < 85 || aimAngle > 275))      //Verifiy if player is aiming on the right or left side of the character, with a 10 degrees blindspot
        {
            Vector2 localScale = transform.localScale;
            isFacingRight = !isFacingRight;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }
    #endregion


    #region Attack
    private void HandlePlayerAim()
    {
        mousePosition = PlayerCamera.ScreenToWorldPoint(Input.mousePosition);
        aimAngle = Vector2.SignedAngle(Vector2.right, rb.position - mousePosition) + 180;       //garants angle between 0 and 360
        CharacterFlip();
    }

    private void HandlePlayerLoadout()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && currentSelectedWeapon != SelectedWeapon.Primary)   //Change to primary if not equiped and '1' is pressed
        {
            isReloading = false;
            currentSelectedWeapon = SelectedWeapon.Primary;
            audioSource.PlayOneShot(drawPrimary);
            StartCoroutine(PrimaryFireCooldown(0.5f));      //Firerate cooldown %50 shorter when switching weapons. Remember: switching to your shovel is always faster than reloading
            Debug.Log($"Changed weapon to {currentSelectedWeapon}");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && currentSelectedWeapon != SelectedWeapon.Melee)  //Change to primary if not equiped and '3' is pressed
        {
            isReloading = false;
            currentSelectedWeapon = SelectedWeapon.Melee;
            audioSource.PlayOneShot(drawMelee);
            StartCoroutine(MeleeFireCooldown(0.5f));    //Firerate cooldown %50 shorter when switching weapons. Remember: switching to your shovel is always faster than reloading
            Debug.Log($"Changed weapon to {currentSelectedWeapon}");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            //Secondary not implemented
            //TODO: play half life loadout empty sound
        }

        if (Input.GetKeyDown(KeyCode.R) && currentSelectedWeapon == SelectedWeapon.Primary)
        {
            if (!isReloading)
            {
                StartCoroutine(PrimaryReload());
            }
        }
    }

    //BUG: Sometimes can call this method multiple times, loading at double or even triple speed. idk why
    IEnumerator PrimaryReload()
    {
        isReloading = true;
        while (isReloading)
        {
            if (currentPrimaryClipContent < primaryClipSize)
            {
                yield return new WaitForSeconds(primaryReloadSpeed);
                if (!isReloading)   //If the nexts reload is cancelled by another action
                    break;
                currentPrimaryClipContent++;
                audioSource.PlayOneShot(reloadingRocketAudio);
                Debug.Log("Current Clip Content:" + currentPrimaryClipContent);
            }
            else
            {
                isReloading = false;
            }
        }
    }

    private void HandlePlayerWeapon()
    {
        if (Input.GetMouseButton(0))
        {
            if (currentSelectedWeapon == SelectedWeapon.Primary)
            {
                ShootPrimary();
            }
            else if(currentSelectedWeapon == SelectedWeapon.Melee)
            {
                SwingMelee();
            }
        }
    }

    private void ShootPrimary()
    {
        if (canShootPrimary)
        {
            if (currentPrimaryClipContent > 0)
            {
                //animator.SetTrigget("ShootRocket");
                isReloading = false;
                audioSource.PlayOneShot(shootingRocketAudio);
                Instantiate(spawnedRocket, rb.position, Quaternion.Euler(0, 0, aimAngle));
                currentPrimaryClipContent--;
                StartCoroutine(PrimaryFireCooldown());
                Debug.Log("Current Clip Content:" + currentPrimaryClipContent);
            }
            else
            {
                if (!isReloading)
                {
                    audioSource.PlayOneShot(clipEmptyAudio);
                    StartCoroutine(PrimaryReload());
                }
            }
        }
    }

    private void SwingMelee()
    {
        if (canShootMelee)
        {
            //animator.SetTrigget("MeleeSwing");
            if (canMarketGardenCrit)
            {
                audioSource.PlayOneShot(shovelAttackCritAudio);
            }
            else
            {
                audioSource.PlayOneShot(shovelAttackAudio);
            }

            Vector2 spawnDirection = new(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
            Vector2 meleeAttackposition = rb.position + spawnDirection * 2;

            GameObject meleeAttack = Instantiate(spawnedMeleeAttack, meleeAttackposition, Quaternion.Euler(0, 0, aimAngle));


            ContactFilter2D contactFilter = new()
            {
                useTriggers = true,
                useLayerMask = true
            };
            contactFilter.SetLayerMask(LayerMask.GetMask("Enemy"));     //TODO: dont make layer hardcoded; add support for hitting walls when no enemy is hit

            List<Collider2D> hitEnemies = new();
            int numberOfHits = Physics2D.OverlapCollider(meleeAttack.GetComponent<Collider2D>(),contactFilter, hitEnemies);

            if (numberOfHits > 0)
            {
                foreach (Collider2D enemy in hitEnemies)
                {
                    audioSource.PlayOneShot(hitSoundAudio,0.25f);    //this shit is too loud
                    if (canMarketGardenCrit)
                    {
                        audioSource.PlayOneShotRandom(hitCritAudios);
                        canMarketGardenCrit = false;    //consumes the crit. for balancing reasons, only 1 market gardener crit is allowed per rocket jump
                    }
                    Debug.Log("Enemy hit: " + enemy.gameObject.name);
                }
            }

            StartCoroutine(MeleeFireCooldown());
        }
    }

    private IEnumerator PrimaryFireCooldown(float weaponSwitchModifier = 1f)
    {
        canShootPrimary = false;
        yield return new WaitForSeconds(primaryFirerate * weaponSwitchModifier);
        canShootPrimary = true;
    }

    private IEnumerator MeleeFireCooldown(float weaponSwitchModifier = 1f)
    {
        canShootMelee = false;
        yield return new WaitForSeconds(meleeFireRate * weaponSwitchModifier);
        canShootMelee = true;
    }

    #endregion

    public void ReceiveExplosion(Vector2 explosionCenter, float damage = 0f)
    {
        Vector2 direction = rb.position - explosionCenter;
        direction.Normalize();  // Normalize the direction vector to get only the direction, ignoring magnitude
        Vector2 knockbackForce = direction * explosionSelfKnockback;
        if (!IsGrounded())
        {
            knockbackForce *= rocketJumpBonusFactor; //adds bonus knockback if already in air
        }

        rb.AddForce(knockbackForce, ForceMode2D.Impulse);
        canMarketGardenCrit = true;
        rocketJumpBufferCounter = marketGardenBufferTime;
    }
}