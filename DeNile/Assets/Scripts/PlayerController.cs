using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private float xAxis, yAxis;
    private Animator playerAnim;
    private bool canDash = true;
    private bool dashed;
    private float gravity;
    public PlayerStateList playerState;
    private bool restoreTime;
    private float restoreTimeSpeed;
    private SpriteRenderer sr;

    [Header("Player Stat Settings")]

    public int health;
    public int maxHealth;
    [SerializeField] float timeToHeal;
    [SerializeField] private float hitFlashSpeed;
    [SerializeField] private GameObject DamageParticles;
    [SerializeField] private GameObject HealingParticles;

    public delegate void OnHealthChangedDelegate();
    public OnHealthChangedDelegate onHealthChangedCallback;
    float healTimer;
    [Space(5)]

    [Header("Player Mana Settings")]
    [SerializeField] float mana;
    [SerializeField] float manaDrainSpeed;
    [SerializeField] float manaGain;
    [SerializeField] private UnityEngine.UI.Image manaStorage;
    [Space(5)]

    [Header("Player Movement Settings")]
    
    [SerializeField] private float walkSpeed = 1;
    [Space(5)]

    [Header("Player Jump Settings")]
    
    [SerializeField] private float jumpForce = 25;
    [SerializeField] private int jumpBufferFrames;
    [SerializeField] private float coyoteTime;

    private int jumpBufferCounter = 0;
    private float coyoteTimeCounter = 0;
    [Space(5)]

    [Header("Player Ability Settings")]

    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [SerializeField] private int maxExtraJumps;
    private int extraJumpCounter = 0;
    [Space(5)]

    [Header("Ground Check Settings")]

    [SerializeField] private Transform GroundCheck;
    [SerializeField] private float GroundCheckY = 0.2f;
    [SerializeField] private float GroundCheckX = 0.5f;
    [SerializeField] private LayerMask GroundLayer;
    [Space(5)]

    [Header("Player Recoil Settings")]

    [SerializeField] int recoilXSteps = 3;
    [SerializeField] int recoilYSteps = 3;
    [SerializeField] float recoilXSpeed = 25;
    [SerializeField] float recoilYSpeed = 25;
    private int stepsXRecoiled, stepsYRecoiled;
    [Space(5)]

    [Header("Attack Settings")]

    [SerializeField] private float playerDamage;
    [SerializeField] private float attackCooldown;
    [Space(5)]
    [SerializeField] private Transform sideAttackCheck, upAttackCheck, downAttackCheck;
    [SerializeField] private Vector2 sideAttackArea, upAttackArea, downAttackArea;
    [SerializeField] private LayerMask attackableLayer;
    [SerializeField] private GameObject slashFX;
    private bool attack = false;
    private float lastAttacked;

    [Space(5)]



    public static PlayerController Instance;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        playerState = GetComponent<PlayerStateList>();

        rb = GetComponent<Rigidbody2D>();

        sr = GetComponent<SpriteRenderer>();

        playerAnim = GetComponent<Animator>();

        gravity = rb.gravityScale;

        Health = maxHealth;

        Mana = mana;

        manaStorage.fillAmount = mana;
    }

    void Update()
    {
            GetInputs();
            UpdateJumpingBools();

            if (playerState.Dashing) return;
            FlipPlayer();
            Move();
            Jump();
            StartDash();
            Attack();
            ResetTimeScale();
            InvincibilityFlicker();
            Heal();

    }

    private void FixedUpdate()
    {
        if (playerState.Dashing) return;
        playerRecoil();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(sideAttackCheck.position, sideAttackArea);
        Gizmos.DrawWireCube(upAttackCheck.position, upAttackArea);
        Gizmos.DrawWireCube(downAttackCheck.position, downAttackArea);
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");

        yAxis = Input.GetAxisRaw("Vertical");

        attack = Input.GetButtonDown("Fire1");
    }
    
    void FlipPlayer()
    {
        if(xAxis < 0)
        {
            transform.localScale = new Vector2(-1, transform.localScale.y);
            playerState.lookingRight = false;
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
            playerState.lookingRight = true;
        }
    }

    private void Move()
    {
        rb.velocity = new Vector2(walkSpeed * xAxis, rb.velocity.y);

        playerAnim.SetBool("Walking", (rb.velocity.x != 0) && isGrounded());
    }

    void StartDash()
    {
        if (Input.GetButtonDown("Dash") && canDash && !dashed)
        {
            StartCoroutine(Dash());
            dashed = true;
        }

        if (isGrounded())
        {
            dashed = false;
        }
    }
    IEnumerator Dash()
    {
        canDash = false;
        playerState.Dashing = true;
        playerAnim.SetTrigger("Dashing");
        rb.gravityScale = 0;
        int direction = playerState.lookingRight ? 1 : -1;
        rb.velocity = new Vector2(direction * dashSpeed, 0);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        playerState.Dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void Attack()
    {
        lastAttacked += Time.deltaTime;
        if(attack && lastAttacked >= attackCooldown)
        {
            lastAttacked = 0;
            playerAnim.SetTrigger("Attacking");

            if(yAxis == 0 || yAxis < 0 && isGrounded())
            {
                Hit(sideAttackCheck, sideAttackArea, ref playerState.recoilingX, recoilXSpeed);

                Instantiate(slashFX, sideAttackCheck);
            }
            else if (yAxis > 0)
            {
                Hit(upAttackCheck, upAttackArea, ref playerState.recoilingY, recoilYSpeed);

                SlashEffectAngle(slashFX, 90, upAttackCheck);
            }
            else if (yAxis < 0 && !isGrounded())
            {
                Hit(downAttackCheck, downAttackArea, ref playerState.recoilingY, recoilYSpeed);
                SlashEffectAngle(slashFX, -90, downAttackCheck);
            }
        }
    }

    private void Hit(Transform attackTransform, Vector2 attackArea, ref bool recoilDirection, float recoilStrength)
    {
        Collider2D[] objectsHit = Physics2D.OverlapBoxAll(attackTransform.position, attackArea, 0, attackableLayer);

        if(objectsHit.Length > 0)
        {
            recoilDirection = true;
        }

        for(int i = 0; i < objectsHit.Length; i++)
        {
            if (objectsHit[i].GetComponent<Enemy>() != null)
            {
                objectsHit[i].GetComponent<Enemy>().enemyHit(playerDamage, 
                    (transform.position - objectsHit[i].transform.position).normalized, recoilStrength);

                if (objectsHit[i].CompareTag("Enemy"))
                {
                    Mana += manaGain;
                }
            }
        }
    }

    void SlashEffectAngle(GameObject slashFX, int FXAngle, Transform attackTransform)
    {
        slashFX = Instantiate(slashFX, attackTransform);
        slashFX.transform.eulerAngles = new Vector3 (0, 0, FXAngle);
        slashFX.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
    }

    void playerRecoil()
    {
        if (playerState.recoilingX)
        {
            if (playerState.lookingRight)
            {
                rb.velocity = new Vector2(-recoilXSpeed, 0);
            }
            else
            {
                rb.velocity = new Vector2 (recoilXSpeed, 0);
            }
        }

        if (playerState.recoilingY)
        {
            if(yAxis < 0)
            {
                rb.gravityScale = 0;
                rb.velocity = new Vector2(rb.velocity.x, recoilYSpeed);
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x, -recoilYSpeed);
            }
        }
        else
        {
            rb.gravityScale = gravity;
        }

        if(playerState.recoilingX && stepsXRecoiled < recoilXSteps)
        {
            stepsXRecoiled++;
        }
        else
        {
            StopRecoilX();
        }

        if (playerState.recoilingY && stepsYRecoiled < recoilYSteps)
        {
            stepsYRecoiled++;
        }
        else
        {
            StopRecoilY();
        }

        if (isGrounded())
        {
            StopRecoilY();
        }
    }

    void StopRecoilX()
    {
        stepsXRecoiled = 0;
        playerState.recoilingX = false;
    }

    void StopRecoilY()
    {
        stepsYRecoiled = 0;
        playerState.recoilingY = false;
    }

    public bool isGrounded()
    {
        if(Physics2D.Raycast(GroundCheck.position, Vector2.down, GroundCheckY, GroundLayer) ||
            Physics2D.Raycast(GroundCheck.position + new Vector3 (GroundCheckX, 0, 0), Vector2.down, GroundCheckY, GroundLayer) ||
            Physics2D.Raycast(GroundCheck.position + new Vector3 (-GroundCheckX, 0, 0), Vector2.down, GroundCheckY, GroundLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Jump()
    {
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && !playerState.Jumping)
            {
                rb.velocity = new Vector3(rb.velocity.x, jumpForce);

                playerState.Jumping = true;
            }
            else if(!isGrounded() && extraJumpCounter < maxExtraJumps && Input.GetButtonDown("Jump"))
            {
                playerState.Jumping = true;

                extraJumpCounter++;

                rb.velocity = new Vector3(rb.velocity.x, jumpForce);
            }

            if (Input.GetButtonUp("Jump") && rb.velocity.y > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);

                playerState.Jumping = false;
            }
        playerAnim.SetBool("Jumping", !isGrounded());
    }

    void UpdateJumpingBools()
    {
        if (isGrounded())
        {
            playerState.Jumping = false;

            coyoteTimeCounter = coyoteTime;

            extraJumpCounter = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else
        {
            jumpBufferCounter--;
        }
    }

    public void TakeDamage(float damage)
    {

        Health -= Mathf.RoundToInt(damage);
        StartCoroutine(CurrentlyInvincible());

    }

    IEnumerator CurrentlyInvincible()
    {
        playerState.invincibleState = true;
        GameObject manaLeakParticles = Instantiate(DamageParticles, transform.position, Quaternion.identity);
        Destroy(manaLeakParticles, 1.5f);
        yield return new WaitForSeconds(1f);
        playerState.invincibleState = false;
    }

    private void InvincibilityFlicker()
    {
        sr.material.color = playerState.invincibleState ? Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * hitFlashSpeed, 1.0f))
            : Color.white;
    }

    public int Health
    {
        get { return health; }
        set
        {
            if(health != value)
            {
                health = Mathf.Clamp(value, 0, maxHealth);

                if(onHealthChangedCallback != null)
                {
                    onHealthChangedCallback.Invoke();
                }
            }
        }
    }

    public float Mana
    {
        get { return mana; }
        set
        {
            if(mana != value)
            {
                mana = Mathf.Clamp(value, 0, 1);
                manaStorage.fillAmount = mana;
            }
        }
    }

    void ResetTimeScale()
    {
        if(restoreTime)
        {
            if(Time.timeScale < 1)
            {
                Time.timeScale += Time.deltaTime * restoreTimeSpeed;
            }
            else
            {
                Time.timeScale = 1;
                restoreTime = false;
            }
        }
    }
    public void TimeSlow(float newTimeScale, int restoreSpeed, float delay)
    {
        restoreTimeSpeed = restoreSpeed;
        Time.timeScale = newTimeScale;

        if (delay > 0)
        {
            StopCoroutine(ResumeNormalTime(delay));
            StartCoroutine(ResumeNormalTime(delay));
        }
        else
        {
            restoreTime = true;
        }
    }

    IEnumerator ResumeNormalTime(float delay)
    {
        restoreTime = true;
        yield return new WaitForSeconds(delay);
    }

    void Heal()
    {

        if (Input.GetButton("Healing") && Health < maxHealth && Mana > 0 && !playerState.Jumping && !playerState.Dashing && xAxis == 0)
        {
            playerState.healing = true;
            healTimer += Time.deltaTime;
            if(healTimer >= timeToHeal)
            {
                Health++;
                healTimer = 0;
                GameObject HealFX = Instantiate(HealingParticles, transform.position, Quaternion.identity);
                Destroy(HealFX, 1f);
            }
            Mana -= Time.deltaTime * manaDrainSpeed;
        }
        else
        {
            playerState.healing = false;
            healTimer = 0;
        }
    }
}
