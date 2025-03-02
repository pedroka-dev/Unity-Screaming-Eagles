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
    ParticleSystem smokeParticleSystem;

    [SerializeField] private MercenaryController mercenary;

    [Header("Movement & Jumping")]
    private float horizontalInput;
    [SerializeField] private Transform groundCheckTransform;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private AudioClip BhopJumpAudio;
    [SerializeField] private GameObject spawnedBhopPopup;

    private bool canMarketGardenCrit;

    private const float DEFAULT_MARKET_GARDEN_BUFFFER_TIME = 0.05f;  // Market Garden buffer makes market gardening input more forgiving and allows bhoping crits
    private float rocketJumpBufferCounter;

    private const float DEFAULT_COYOTE_TIME = 0.2f;    // Coyote time allows player to jump a brief moment after being in the air
    private float coyoteTimeCounter;

    private const float DEFAULT_JUMP_BUFFER_TIME = 0.2f;     // Jump buffer allows player to jump for a brief moment before touching the ground
    private float jumpBufferCounter;


    [Header("Aiming & Loadout")]
    public SelectedWeapon CurrentSelectedWeapon = SelectedWeapon.Primary;
    [SerializeField] private GameObject spawnedDamagePopup;
    [SerializeField] private GameObject spawnedCritPopup;
    [SerializeField] private GameObject spawnedClipEmptyPopup;
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
    private Coroutine reloadCoroutine;

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
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        smokeParticleSystem = GetComponent<ParticleSystem>();
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
            coyoteTimeCounter = DEFAULT_COYOTE_TIME;

            rocketJumpBufferCounter -= Time.deltaTime;
            if (rocketJumpBufferCounter <= 0f)
            {
                canMarketGardenCrit = false;
                smokeParticleSystem.Stop();
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
            rocketJumpBufferCounter = DEFAULT_MARKET_GARDEN_BUFFFER_TIME;
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = DEFAULT_JUMP_BUFFER_TIME;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (CanJump())
        {
            mercenary.Jump();
            jumpBufferCounter = 0f;

            if (IsBunnyhopping())
            {
                audioSource.PlayOneShot(BhopJumpAudio, 0.05f);
                Instantiate(spawnedBhopPopup, rb.position, new Quaternion());
            }       
        }

        // Allow for a "variable jump height" by reducing upward velocity when the player releases the jump button
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.AddForce(new Vector2(0f, -rb.velocity.y * 0.5f), ForceMode2D.Impulse);   // Smooth deceleration when releasing the jump
            coyoteTimeCounter = 0f;
        }
    }

    /// <summary>
    /// Uses the ground check object on the prefab to verify if the mercenary is contact with the ground bellow
    /// </summary>
    /// <returns></returns>
    private bool IsGrounded() => Physics2D.OverlapCircle(groundCheckTransform.position, 0.25f, groundLayer);

    /// <summary>
    /// Check if the player can jump (considering coyote time, jump buffer and if its not already jumping)
    /// </summary>
    /// <returns></returns>
    private bool CanJump() => coyoteTimeCounter > 0f && jumpBufferCounter > 0f && !mercenary.IsJumping;

    /// <summary>
    /// Check if the current jump is a market gardener bunny hop instead of normal jump.
    /// </summary>
    /// <returns></returns>
    private bool IsBunnyhopping() => canMarketGardenCrit //&& IsGrounded()
        && rb.velocity.y >= MercenaryController.DEFAULT_JUMPING_POWER / 3    //verifies if the jump is not too shallow
        && rocketJumpBufferCounter != DEFAULT_MARKET_GARDEN_BUFFFER_TIME;   //verifies if the bhop attempt is not on the first frame (for example, on the first rocket jump)


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
            reloadCoroutine ??= StartCoroutine(PrimaryReload());
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
        reloadCoroutine = null;     // Reset the coroutine reference when done
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
                audioSource.PlayOneShot(shootingRocketAudio,0.5f);
                Instantiate(spawnedRocket, rb.position, Quaternion.Euler(0, 0, aimAngle));
                currentPrimaryClipContent--;
                StartCoroutine(PrimaryFireCooldown());

            }
            else
            {
                if (!isReloading)
                {
                    reloadCoroutine ??= StartCoroutine(PrimaryReload());
                    Instantiate(spawnedClipEmptyPopup, rb.position, new Quaternion());
                    audioSource.PlayOneShot(clipEmptyAudio);
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
                audioSource.PlayOneShot(shovelAttackCritAudio,0.8f);
            }
            else
            {
                audioSource.PlayOneShot(shovelAttackAudio,0.8f);
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
            var enemyController = enemy.gameObject.GetComponent<MercenaryController>();
            if (enemyController.IsAlive)
            {
                int hitDamage = meleeDamage;
                audioSource.PlayOneShot(hitSoundAudio, 0.15f);    //this shit is too loud
                if (canMarketGardenCrit)
                {
                    hitDamage *= 3;
                    //canMarketGardenCrit = false;    //consumes the crit. for balancing reasons, only 1 market gardener crit is allowed per rocket jump
                    Instantiate(spawnedCritPopup, enemy.transform.position, new Quaternion());
                    audioSource.PlayOneShotRandom(hitCritAudios,0.8f);
                }

                float randomPositionOffset = Random.Range(-1f, 1f);  //Damage popup just looks better with a bit of offset. Feels more dynamic and keeps visually separated from the crit popup 
                Vector2 enemyPositionWithOffset = new(enemy.transform.position.x + randomPositionOffset, enemy.transform.position.y + randomPositionOffset / 2);

                GameObject damagePopUp = Instantiate(spawnedDamagePopup, enemyPositionWithOffset, new Quaternion());
                damagePopUp.GetComponent<DamagePopUpController>().SetText(hitDamage);

                enemyController.ReceiveDamage(hitDamage);
            }
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
        smokeParticleSystem.Play();
        rocketJumpBufferCounter = DEFAULT_MARKET_GARDEN_BUFFFER_TIME;
    }
}