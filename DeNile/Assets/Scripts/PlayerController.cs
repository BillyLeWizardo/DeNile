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
    [SerializeField] private GameObject gameOverScreen;

    public delegate void OnHealthChangedDelegate(); //This is a delegate due to it being used differently in multiple methods
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

    private void Awake() //Runs when the player object is instantiated/woken up
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject); //Destroys the object if it is not the current player instance
        }
        else
        {
            Instance = this; //Sets the current player game object to instance
        }
        DontDestroyOnLoad(gameObject); //Prevents it from being destroyed when a new scene is loaded
    }

    void Start()
    {
        playerState = GetComponent<PlayerStateList>(); //Gets the player state script component

        rb = GetComponent<Rigidbody2D>(); //gets the rigid body component

        sr = GetComponent<SpriteRenderer>(); //gets the sprite renderer component

        playerAnim = GetComponent<Animator>(); //gets the animator component

        gravity = rb.gravityScale; //sets the current gravity scale

        Health = maxHealth; //Sets the players current health to the max amount when the level starts

        Mana = mana; //Sets the max mana

        manaStorage.fillAmount = mana; //fills the mana icon on the HUD
    }

    void Update()
    {
        GetInputs(); //Checks for any inputs entered by the player
        UpdateJumpingBools();

        if(Health <= 0) //If health is 0 then kill the player
        {
            playerDies();
        }

        if (playerState.Dashing) return; //If the player is busy dashing then do not let them do anything else

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

    private void playerDies()
    {
        //If the player dies then freeze the game and enable the game over screen.
        Time.timeScale = 0f;
        gameOverScreen.SetActive(true);
    }

    private void OnDrawGizmos()
    {
        //Draws gizmos in the editor to see hitboxes
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(sideAttackCheck.position, sideAttackArea);
        Gizmos.DrawWireCube(upAttackCheck.position, upAttackArea);
        Gizmos.DrawWireCube(downAttackCheck.position, downAttackArea);
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal"); //Gets unity's input under the label Horizontal

        yAxis = Input.GetAxisRaw("Vertical"); //Gets unity's input under the label Vertical

        attack = Input.GetButtonDown("Fire1"); //Gets unity's input under the label Fire1
    }
    
    void FlipPlayer()
    {
        if(xAxis < 0) //If the player is moving right, flip their model to be facing right, and vice versa
        {
            transform.localScale = new Vector2(-1, transform.localScale.y);
            playerState.lookingRight = false; //Changes the player state to be looking right
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
            playerState.lookingRight = true;
        }
    }

    private void Move()
    {
        rb.velocity = new Vector2(walkSpeed * xAxis, rb.velocity.y); //Adds velocity to the direction that the player is pressing, multiplied by the character speed

        playerAnim.SetBool("Walking", (rb.velocity.x != 0) && isGrounded()); //Sets the player to walking if they are moving and grounded
    }

    void StartDash()
    {
        if (Input.GetButtonDown("Dash") && canDash && !dashed) //If the player presses the dash key, trigger the dash coroutine.
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
        canDash = false; //Prevents the player from spamming dash
        playerState.Dashing = true; //Sets the player to be currently dashing
        playerAnim.SetTrigger("Dashing"); //Plays the dashing animation
        rb.gravityScale = 0; //Disables the players gravity so they dash completely horizontally
        int direction = playerState.lookingRight ? 1 : -1; //If the player is not looking right, dash the player in the other direction
        rb.velocity = new Vector2(direction * dashSpeed, 0); //Moves the player in the direction they are facing
        yield return new WaitForSeconds(dashTime); //Waits for the dash timer's worth of seconds
        rb.gravityScale = gravity; //Resets gravity
        playerState.Dashing = false; //Stops the player from being in the dashing state
        yield return new WaitForSeconds(dashCooldown); //Prevents the player from dashing again until it's cooldown is done
        canDash = true; //Allows the player to dash again
    }

    void Attack()
    {
        lastAttacked += Time.deltaTime; //Increases the time since the player last attacked
        if(attack && lastAttacked >= attackCooldown) //If the player is attacking and they have waited for the cooldown to end, it will allows the player to attack again
        {
            lastAttacked = 0; //Resets the attack timer to prevent spamming
            playerAnim.SetTrigger("Attacking"); //Plays the attacking animation

            if(yAxis == 0 || yAxis < 0 && isGrounded()) //If the player attacks while grounded and not moving, the attack will be from the side
            {
                Hit(sideAttackCheck, sideAttackArea, ref playerState.recoilingX, recoilXSpeed); //Recoils the player along with triggering the Hit method

                Instantiate(slashFX, sideAttackCheck); //Spawns slash fx to the side of the player
            }
            else if (yAxis > 0) //If the player is holding W then the player will attack up
            {
                Hit(upAttackCheck, upAttackArea, ref playerState.recoilingY, recoilYSpeed);

                SlashEffectAngle(slashFX, 90, upAttackCheck); //Spawns slash FX above the player
            }
            else if (yAxis < 0 && !isGrounded()) //If the player is holding S and is above ground, the attack will be below the player, if they are grounded they will do a side attack
            {
                Hit(downAttackCheck, downAttackArea, ref playerState.recoilingY, recoilYSpeed); //Recoils the player upwards
                SlashEffectAngle(slashFX, -90, downAttackCheck); //Spawns slash FX under the player
            }
        }
    }

    private void Hit(Transform attackTransform, Vector2 attackArea, ref bool recoilDirection, float recoilStrength)
    {
        Collider2D[] objectsHit = Physics2D.OverlapBoxAll(attackTransform.position, attackArea, 0, attackableLayer); //Creates a box to check what objects it has overlapped on a certain layer

        if(objectsHit.Length > 0) //Triggers if the objectsHit variable senses an object has been hit
        {
            recoilDirection = true;
        }

        for(int i = 0; i < objectsHit.Length; i++) //Runs through every object hit in the objectsHit collider
        {
            if (objectsHit[i].GetComponent<Enemy>() != null) //If the object hit has the enemy component then this will trigger
            {
                objectsHit[i].GetComponent<Enemy>().enemyHit(playerDamage, 
                    (transform.position - objectsHit[i].transform.position).normalized, recoilStrength); //Damages the enemy and recoils them in the other direction from the player

                if (objectsHit[i].CompareTag("Enemy"))
                {
                    Mana += manaGain; //If the object hit is an enemy, regen the player's Mana
                }
            }
        }
    }

    void SlashEffectAngle(GameObject slashFX, int FXAngle, Transform attackTransform)
    {
        slashFX = Instantiate(slashFX, attackTransform); //Spawns the slash FX
        slashFX.transform.eulerAngles = new Vector3 (0, 0, FXAngle); //Rotates the slash FX bases on the entered value
        slashFX.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y); //Spawns the slash FX to fit the given size
    }

    void playerRecoil()
    {
        if (playerState.recoilingX)
        {
            if (playerState.lookingRight)
            {
                rb.velocity = new Vector2(-recoilXSpeed, 0); //If the player is looking right, recoil them to the left
            }
            else
            {
                rb.velocity = new Vector2 (recoilXSpeed, 0); //Else recoil the player to the left
            }
        }

        if (playerState.recoilingY)
        {
            if(yAxis < 0) //If the player's yAxis is under 0, meaning the player is attacking downwards or holding down
            {
                rb.gravityScale = 0; //Turns off gravity so the player can go upwards uninterrupted
                rb.velocity = new Vector2(rb.velocity.x, recoilYSpeed); //Pushes the player upwards
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x, -recoilYSpeed); //Pushes the player downwards
            }
        }
        else
        {
            rb.gravityScale = gravity; //If the player is not recoiling along the Y axis, reset gravity
        }

        if(playerState.recoilingX && stepsXRecoiled < recoilXSteps)
        {
            stepsXRecoiled++; //Counts how many steps the player has recoiled to prevent them from recoiling forever
        }
        else
        {
            StopRecoilX(); //Triggers the stop recoiling X method
        }

        if (playerState.recoilingY && stepsYRecoiled < recoilYSteps)
        {
            stepsYRecoiled++; //Counts how many steps the player has recoiled along the Y axis
        }
        else
        {
            StopRecoilY(); //Stops recoiling when they hit the max steps
        }

        if (isGrounded())
        {
            StopRecoilY(); //If the player is grounded then automatically stop recoiling them
        }
    }

    void StopRecoilX()
    {
        stepsXRecoiled = 0; //Resets the step counter
        playerState.recoilingX = false; //Sets the player to not be recoiling along the X axis
    }

    void StopRecoilY()
    {
        stepsYRecoiled = 0; //Resets the step counter for the Y axis recoil
        playerState.recoilingY = false; //Sets the player to not be recoiling along the Y axis
    }

    public bool isGrounded()
    {
        if(Physics2D.Raycast(GroundCheck.position, Vector2.down, GroundCheckY, GroundLayer) ||
            Physics2D.Raycast(GroundCheck.position + new Vector3 (GroundCheckX, 0, 0), Vector2.down, GroundCheckY, GroundLayer) ||
            Physics2D.Raycast(GroundCheck.position + new Vector3 (-GroundCheckX, 0, 0), Vector2.down, GroundCheckY, GroundLayer))
            //Raycasts to check for if the player model is currently colliding with any object with the Ground tag
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
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && !playerState.Jumping) //Gives the player a grace period (Coyote Timer) so they can still jump a few moments after they walk off a ledge 
            {
                rb.velocity = new Vector3(rb.velocity.x, jumpForce); //Adds a velocity to the player upwards so that they are sent upwards

                playerState.Jumping = true; //Triggers the jumping state
            }
            else if(!isGrounded() && extraJumpCounter < maxExtraJumps && Input.GetButtonDown("Jump")) //If the player is not grounded but presses the jump again and still has an extra jump left, they will jump for a second time
            {
                playerState.Jumping = true;

                extraJumpCounter++; //Increase the jump counter

                rb.velocity = new Vector3(rb.velocity.x, jumpForce);
            }

            if (Input.GetButtonUp("Jump") && rb.velocity.y > 0) //If they let go of the jump button they will start to descend
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);

                playerState.Jumping = false;
            }
        playerAnim.SetBool("Jumping", !isGrounded()); //Sets the player animation to jumping or not
    }

    void UpdateJumpingBools() //Resets values when the player is grounded again
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
            jumpBufferCounter = jumpBufferFrames; //Prevents the player from spamming jump too fast
        }
        else
        {
            jumpBufferCounter--; 
        }
    }

    public void TakeDamage(float damage)
    {

        Health -= Mathf.RoundToInt(damage); //Decreses the players health
        StartCoroutine(CurrentlyInvincible()); //Triggers the player invincibility coroutine

    }

    IEnumerator CurrentlyInvincible()
    {
        playerState.invincibleState = true; //Sets the player to be currently invincible
        GameObject manaLeakParticles = Instantiate(DamageParticles, transform.position, Quaternion.identity); //Spawns particles to show the player took damage
        Destroy(manaLeakParticles, 1.5f); //Destroys the particls after 1.5 seconds
        yield return new WaitForSeconds(1f); //Waits for a second
        playerState.invincibleState = false; //Stops the player from being invincible
    }

    private void InvincibilityFlicker()
    {
        sr.material.color = playerState.invincibleState ? Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * hitFlashSpeed, 1.0f))
            : Color.white; //Flashes the player white and black if they are invincible otherwise the sprite is set to white
    }

    public int Health
    {
        get { return health; }
        set
        {
            if(health != value) //Sets health to a value between 0 and max health
            {
                health = Mathf.Clamp(value, 0, maxHealth); //Clamps health to never increase more than or be less than 0 and the max Health

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
            if(mana != value) //Sets the mana level to a number between 0 and 1
            {
                mana = Mathf.Clamp(value, 0, 1);
                manaStorage.fillAmount = mana; //Fills the mana storage to be the amount of mana the player has
            }
        }
    }

    void ResetTimeScale()
    {
        if(restoreTime)
        {
            if(Time.timeScale < 1) //Restores the time scale slowly if it is currently being slowed
            {
                Time.timeScale += Time.deltaTime * restoreTimeSpeed;
            }
            else
            {
                Time.timeScale = 1; //Sets the time scale to 1 so the game can continue playing at normal speed
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
            StopCoroutine(ResumeNormalTime(delay)); //Stops the current time slow if it is playing when another is going to start
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
        yield return new WaitForSeconds(delay); //Waits a certain set amount of time before restoring time back to normal
    }

    void Heal()
    {

        if (Input.GetButton("Healing") && Health < maxHealth && Mana > 0 && !playerState.Jumping && !playerState.Dashing && xAxis == 0) //If the player is stationary and holds Right click
        {
            playerState.healing = true; //Sets the player to be healing
            healTimer += Time.deltaTime; //Increases the heal time
            if(healTimer >= timeToHeal) //once the timer reaches the time to heal
            {
                Health++; //Increase the players health
                healTimer = 0; //Resets the timer
                GameObject HealFX = Instantiate(HealingParticles, transform.position, Quaternion.identity); //spawns some particles to signal that health has been restored
                Destroy(HealFX, 1f); //Destroys the particles after 1 second
            }
            Mana -= Time.deltaTime * manaDrainSpeed; //Decreases mana over time as the player heals
        }
        else
        {
            playerState.healing = false; //Stops the player from being in the healing state
            healTimer = 0; //Resets the time for the next time they try to heal
        }
    }
}
