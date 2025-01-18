using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum SelectedWeapon
{
    Primary,
    Seecondary,
    Melee
}

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;
    AudioSource audioSource;

    [SerializeField] private MercenaryController mercenary;

    [Header("Movement & Jumping")]
    private float horizontalInput;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    private bool canMarketGardenCrit;

    private readonly float marketGardenBufferTime = 0.05f;  // Market Garden buffer makes market gardening input more forgiving and allows bhoping crits
    private float rocketJumpBufferCounter;

    private readonly float coyoteTime = 0.2f;    // Coyote time allows player to jump a brief moment after being in the air
    private float coyoteTimeCounter;

    private readonly float jumpBufferTime = 0.2f;     // Jump buffer allows player to jump for a brief moment before touching the ground
    private float jumpBufferCounter;


    [Header("Aiming & Loadout")]
    public SelectedWeapon CurrentSelectedWeapon = SelectedWeapon.Primary;
    [SerializeField] private GameObject spawnedDamagePopup;
    [SerializeField] private GameObject spawnedCritPopup;
    [SerializeField] private Camera PlayerCamera;
    [SerializeField] private AudioClip drawPrimary;
    [SerializeField] private AudioClip drawMelee;
    [SerializeField] private List<SpriteRenderer> soldierPrimarySprite;
    [SerializeField] private List<SpriteRenderer> soldierMeleeSprite;
    
    private Vector2 mousePosition;
    private float aimAngle = 360f;  //scale of 0 to 360 always, counter clockwise
    

    [Header("Primary Weapon")]
    public int currentPrimaryClipContent = 4;
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

    [Header("Melee Weapon")]
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private GameObject spawnedMeleeAttack;
    [SerializeField] private AudioClip shovelAttackAudio;
    [SerializeField] private AudioClip shovelAttackCritAudio;
    [SerializeField] private AudioClip hitSoundAudio;
    [SerializeField] private List<AudioClip> hitCritAudios;

    private bool canShootMelee = true;
    private float meleeFireRate = 0.8f;
    private int meleeDamage = 65;

    private bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, 0.25f, groundLayer);
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        UpdateWeaponSprite();
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
        float targetVelocity = horizontalInput * mercenary.MaxMovementSpeed;

        if (IsGrounded() && (horizontalInput == 0 || Math.Abs(currentVelocity) > mercenary.MaxMovementSpeed))
        {
            rb.drag = MercenaryController.DEFAULT_GROUND_DRAG;     // Apply drag to slow the player when there's no input or the velocity exceeds max speed, while grounded
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
                    force *= MercenaryController.DEFAULT_AIR_CONTROL_FACTOR; // Reduces movement control on air
                }

                // Apply movement force but cap it to prevent going over the movementSpeed
                if (Math.Abs(currentVelocity) < mercenary.MaxMovementSpeed || Math.Sign(targetVelocity) != Math.Sign(currentVelocity))
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
        if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f && !mercenary.IsJumping)
        {
            mercenary.Jump();
            jumpBufferCounter = 0f;
        }

        // Allow for a "variable jump height" by reducing upward velocity when the player releases the jump button
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.AddForce(new Vector2(0f, -rb.velocity.y * 0.5f), ForceMode2D.Impulse);   // Smooth deceleration when releasing the jump
            coyoteTimeCounter = 0f;
        }
    }
   
    #endregion


    #region Attack
    private void HandlePlayerAim()
    {
        mousePosition = PlayerCamera.ScreenToWorldPoint(Input.mousePosition);
        aimAngle = Vector2.SignedAngle(Vector2.right, rb.position - mousePosition) + 180;       //garants angle between 0 and 360
        if (mercenary.IsFacingRight && (aimAngle > 95 && aimAngle < 265) || !mercenary.IsFacingRight && (aimAngle < 85 || aimAngle > 275))
        {
            mercenary.CharacterFlip(transform);
        }
    }

    private void HandlePlayerLoadout()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && CurrentSelectedWeapon != SelectedWeapon.Primary)   //Change to primary if not equiped and '1' is pressed
        {
            isReloading = false;
            CurrentSelectedWeapon = SelectedWeapon.Primary;
            audioSource.PlayOneShot(drawPrimary);
            StartCoroutine(PrimaryFireCooldown(0.5f));      //Firerate cooldown %50 shorter when switching weapons. Remember: switching to your shovel is always faster than reloading
            UpdateWeaponSprite();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && CurrentSelectedWeapon != SelectedWeapon.Melee)  //Change to primary if not equiped and '3' is pressed
        {
            isReloading = false;
            CurrentSelectedWeapon = SelectedWeapon.Melee;
            audioSource.PlayOneShot(drawMelee);
            StartCoroutine(MeleeFireCooldown(0.5f));    //Firerate cooldown %50 shorter when switching weapons. Remember: switching to your shovel is always faster than reloading
            UpdateWeaponSprite();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            //Secondary not implemented
            //TODO: play half life loadout empty sound
        }

        if (Input.GetKeyDown(KeyCode.R) && CurrentSelectedWeapon == SelectedWeapon.Primary)
        {
            if (!isReloading)
            {
                StartCoroutine(PrimaryReload());
            }
        }
    }

    private void UpdateWeaponSprite()
    {
        //this is a TERRIBLE and RETARDED way of doing this. but it will work for now
        foreach(SpriteRenderer spriteRenderer in soldierPrimarySprite)
        {
            spriteRenderer.enabled = CurrentSelectedWeapon == SelectedWeapon.Primary;
        }

        foreach (SpriteRenderer sprite in soldierMeleeSprite)
        {
            sprite.enabled = CurrentSelectedWeapon == SelectedWeapon.Melee;
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
            if (CurrentSelectedWeapon == SelectedWeapon.Primary)
            {
                ShootPrimary();
            }
            else if(CurrentSelectedWeapon == SelectedWeapon.Melee)
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

            List<Collider2D> enemiesHit = new();
            int numberOfHits = Physics2D.OverlapCollider(meleeAttack.GetComponent<Collider2D>(),contactFilter, enemiesHit);

            if (numberOfHits > 0)
            {
                MeleeHitEnemies(enemiesHit);
            }

            StartCoroutine(MeleeFireCooldown());
        }
    }

    private void MeleeHitEnemies(List<Collider2D> enemiesHit)
    {
        foreach (Collider2D enemy in enemiesHit)
        {
            int hitDamage = meleeDamage;
            audioSource.PlayOneShot(hitSoundAudio, 0.25f);    //this shit is too loud
            if (canMarketGardenCrit)
            {
                hitDamage *= 3;
                canMarketGardenCrit = false;    //consumes the crit. for balancing reasons, only 1 market gardener crit is allowed per rocket jump
                Instantiate(spawnedCritPopup, enemy.transform.position, new Quaternion());
                audioSource.PlayOneShotRandom(hitCritAudios);
            }

            float randomPositionOffset = Random.Range(-1f, 1f);  //Damage popup just looks better with a bit of offset. Feels more dynamic and keeps visually separated from the crit popup 
            Vector2 enemyPositionWithOffset = new(enemy.transform.position.x + randomPositionOffset, enemy.transform.position.y + randomPositionOffset / 2);

            GameObject damagePopUp = Instantiate(spawnedDamagePopup, enemyPositionWithOffset, new Quaternion());
            damagePopUp.GetComponent<DamagePopUpController>().SetText(hitDamage);
            //enemy.ReceiveDamage(hitDamage);
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

    public void ReceiveExplosion(Vector2 explosionCenter, int damage = 0)
    {
        //this.ReceiveDamage(damage);
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