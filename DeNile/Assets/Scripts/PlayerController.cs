using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private float xAxis;
    PlayerStateList playerState;
    private bool canDash = true;
    private bool dashed;
    private float gravity;

    [Header("Player Movement Settings")]

    [SerializeField] private float walkSpeed = 1;

    [Header("Player Jump Settings")]
    [SerializeField] private float jumpForce = 25;
    
    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames;

    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;

    [Header("Player Ability Settings")]

    private int extraJumpCounter = 0;
    [SerializeField] private int maxExtraJumps;

    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;

    [Header("Ground Check Settings:")]

    [SerializeField] private Transform GroundCheck;
    [SerializeField] private float GroundCheckY = 0.2f;
    [SerializeField] private float GroundCheckX = 0.5f;
    [SerializeField] private LayerMask GroundLayer;

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

        gravity = rb.gravityScale;
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
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
    }
    
    void FlipPlayer()
    {
        if(xAxis < 0)
        {
            transform.localScale = new Vector2(-1, transform.localScale.y);
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
        }
    }

    private void Move()
    {
        rb.velocity = new Vector2(walkSpeed * xAxis, rb.velocity.y);
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
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        playerState.Dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
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
}
