using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    public class MercenaryController : MonoBehaviour
    {
        private Rigidbody2D rb;
        private AudioSource audioSource;
        private ParticleSystem bloodParticleSystem;
        private SpriteRenderer spriteRenderer;

        public const float DEFAULT_MOVEMENT_SPEED = 15f;
        public const float DEFAULT_JUMPING_POWER = 26;
        public const float DEFAULT_GROUND_DRAG = 10f;
        public const float DEFAULT_AIR_CONTROL_FACTOR = 0.05f;

        [SerializeField] private MercenaryClass MercenaryClass;
        [SerializeField] private MercenaryTeam MercenaryTeam;
        [SerializeField] private bool isDummy = false;
        [SerializeField] private List<AudioClip> receiveDamageAudios;
        [SerializeField] private List<AudioClip> mercenaryDeathAudios;
        [SerializeField] private GameObject spawnedDeathDustcloud;
        

        public int MaxHealth { get; private set; }
        public int MaxOverheal { get; private set; }
        public float MaxMovementSpeed { get; private set; }

        #region TODO: Class specific stats
        //public bool CanDoubleJump { get; private set; } = false;
        //public bool HasPassiveRegeneration { get; private set; } = false;
        //public bool IsImmuneToAfterburn { get; private set; } = false;
        #endregion

        public float CurrentHealth { get; private set; }
        public bool IsJumping { get; private set; } = false;
        public bool IsAlive { get; private set; } = true;
        public bool IsFacingRight { get; private set; }  = true;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            bloodParticleSystem = GetComponentInChildren<ParticleSystem>();
            audioSource = GetComponent<AudioSource>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            SetClassStats();
            CurrentHealth = MaxHealth;
        }


        public void Jump()
        {
            rb.AddForce(new Vector2(0f, MercenaryController.DEFAULT_JUMPING_POWER), ForceMode2D.Impulse);
            StartCoroutine(JumpCooldown());
        }

        //public void HealHealth(int healValue, bool canOverheal = false)
        //{
        //    if (canOverheal)
        //    {
        //        CurrentHealth = CurrentHealth + healValue > MaxOverheal ? MaxOverheal : CurrentHealth + healValue;
        //        //StartCoroutine(TickDownOverheal());
        //    }
        //    else
        //    {
        //        CurrentHealth = CurrentHealth + healValue > MaxHealth ? MaxHealth : CurrentHealth + healValue;
        //    }
        //    //TODO: heal popup (both players and enemies)
        //    //TODO: heal particle
        //}

        public void ReceiveDamage(int damageValue)
        {
            CurrentHealth -= damageValue;
            if (CurrentHealth < 0)
            {
                Kill();
            }
            else
            {
                if (damageValue > 0)
                    bloodParticleSystem.Play();

                audioSource.PlayOneShotRandom(receiveDamageAudios, 0.8f);
            }
        }

        private void Kill()
        {
            CurrentHealth = 0;
            IsAlive = false;    //TODO: block new inputs

            Quaternion randomZRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));
            Instantiate(spawnedDeathDustcloud, rb.transform.position, randomZRotation);
           
            audioSource.PlayOneShotRandom(mercenaryDeathAudios, 0.8f);

            bloodParticleSystem.emission.SetBursts(new[] { new ParticleSystem.Burst(0, 15, 1, 0.001f) });  //Basically, a big burst. TODO: fix values for player
            bloodParticleSystem.Play();

            if (isDummy)    //TODO: Modify to player as well 
            {
                rb.simulated = false;
                spriteRenderer.enabled = false;
                StartCoroutine(gameObject.DestroyAfterAudioAndPaticlesEnd(audioSource, bloodParticleSystem));
            }


            gameObject.layer = LayerMask.NameToLayer("Neutral");
        }


        /// <summary>
        /// Flips the mercenary horizontally on the scene. 
        /// </summary>
        /// <param name="transform"></param>
        public void CharacterFlip(Transform transform)
        {
            Vector2 localScale = transform.localScale;
            IsFacingRight = !IsFacingRight;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }


        /// <summary>
        /// Sets the Max Health, Max Overheal, Max Movement Speed and other unique class stats.
        /// </summary>
        /// <param name="mercenaryClass"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SetClassStats()
        {
            switch (MercenaryClass) {
                case MercenaryClass.Scout:
                    MaxHealth = 125;
                    MaxOverheal = 185;
                    MaxMovementSpeed = DEFAULT_MOVEMENT_SPEED * 1.33f;
                    //CanDoubleJump = true;
                    break;

                case MercenaryClass.Soldier:
                    MaxHealth = 200;
                    MaxOverheal = 300;
                    MaxMovementSpeed = DEFAULT_MOVEMENT_SPEED * 0.8f;
                    break;

                case MercenaryClass.Pyro:
                    throw new NotImplementedException();
                    //BaseHealth = 175;
                    //BaseOverhealHealth = 260;
                    //BaseMovementSpeed = DEFAULT_MOVEMENT_SPEED;
                    //IsImmuneToAfterburn = true;
                    //break;

                case MercenaryClass.Demoman:
                    MaxHealth = 175;
                    MaxOverheal = 260;
                    MaxMovementSpeed = DEFAULT_MOVEMENT_SPEED * 93;
                    break;

                case MercenaryClass.Heavy:
                    MaxHealth = 300;
                    MaxOverheal = 400;
                    MaxMovementSpeed = DEFAULT_MOVEMENT_SPEED * 77;
                    break;

                case MercenaryClass.Engineer:
                    throw new NotImplementedException();
                    //BaseHealth = 125;
                    //BaseOverhealHealth = 185;
                    //BaseMovementSpeed = DEFAULT_MOVEMENT_SPEED;
                    //break;

                case MercenaryClass.Medic:
                    MaxHealth = 150;
                    MaxOverheal = 225;
                    MaxMovementSpeed = DEFAULT_MOVEMENT_SPEED * 107;
                    //HasPassiveRegeneration = true;
                    break;

                case MercenaryClass.Sniper:
                    MaxHealth = 125;
                    MaxOverheal = 185;
                    MaxMovementSpeed = DEFAULT_MOVEMENT_SPEED;
                    break;

                case MercenaryClass.Spy:
                    throw new NotImplementedException();
                    //BaseHealth = 125;
                    //BaseOverhealHealth = 185;
                    //BaseMovementSpeed = DEFAULT_MOVEMENT_SPEED * 107;
                    //break;
            }
        }

        /// <summary>
        /// Used to stop mercenary jump spam.
        /// </summary>
        /// <returns></returns>
        private IEnumerator JumpCooldown()
        {
            IsJumping = true;
            yield return new WaitForSeconds(0.4f);
            IsJumping = false;
        }
    }
}
