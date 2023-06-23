using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private float xAxis;
    private Animator playerAnim;
    private bool canDash = true;
    private bool dashed;
    private float gravity;
    public PlayerStateList playerState;
    private bool restoreTime;
    private float restoreTimeSpeed;
    private SpriteRenderer sr;

    [Header("Player Stat Settings")]
    
    [SerializeField] private GameObject ManaLeak;
    [SerializeField] private float hitFlashSpeed;
    public int health;
    public int maxHealth;

    public delegate void OnHealthChangedDelegate();
    public OnHealthChangedDelegate onHealthChangedCallback;

    float healTimer;
    [SerializeField] float timeToHeal;
    [SerializeField] private GameObject healingLoopFX;
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
    
    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames;

    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    [Space(5)]

    [Header("Player Ability Settings")]

    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    private int extraJumpCounter = 0;
    [SerializeField] private int maxExtraJumps;
    [Space(5)]

    [Header("Ground Check Settings")]

    [SerializeField] private Transform GroundCheck;
    [SerializeField] private float GroundCheckY = 0.2f;
    [SerializeField] private float GroundCheckX = 0.5f;
    [SerializeField] private LayerMask GroundLayer;
    [Space(5)]

    [Header("Player Recoil Settings")]
    
    [SerializeField] int recoilXSteps = 5;
    [SerializeField] float recoilXSpeed = 100;
    private int stepsXRecoiled;
    [Space(5)]

    [Header("Attack Settings")]

    [SerializeField] private float playerDamage;
    [SerializeField] private Transform sideAttackCheck;
    [SerializeField] private Vector2 sideAttackArea;
    [SerializeField] private LayerMask attackableLayer;
    [SerializeField] private GameObject slashFX;
    private bool attack = false;
    private float attackCooldown, lastAttacked;
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
    }
    // Start is called before the first frame update
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

    // Update is called once per frame
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
        if(playerState.Dashing) return;
        playerRecoil();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(sideAttackCheck.position, sideAttackArea);
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");

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
        rb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
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

            Hit(sideAttackCheck, sideAttackArea, ref playerState.recoilingX, recoilXSpeed);

            Instantiate(slashFX, sideAttackCheck);

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

        if(playerState.recoilingX && stepsXRecoiled < recoilXSteps)
        {
            stepsXRecoiled++;
        }
        else
        {
            StopRecoilX();
        }
    }

    void StopRecoilX()
    {
        stepsXRecoiled = 0;
        playerState.recoilingX = false;
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
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);

            playerState.Jumping = false;
        }
        if (!playerState.Jumping)
        {
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
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
        GameObject manaLeakParticles = Instantiate(ManaLeak, transform.position, Quaternion.identity);
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
                GameObject HealFX = Instantiate(healingLoopFX, transform.position, Quaternion.identity);
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
