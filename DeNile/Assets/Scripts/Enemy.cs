using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Enemy : MonoBehaviour
{
    protected Rigidbody2D enemyRB;
    protected SpriteRenderer enemySR;
    protected EnemyState enemyState;
    protected Animator enemyAnim;
    protected PlayerController player;


    [Header("Enemy Health Settings")]
    [SerializeField] protected float enemyHealth;
    [Space(5)]

    [Header("Health Bar Settings")]
    [SerializeField] protected GameObject enemyHealthBarFill;
    [SerializeField] protected GameObject enemyDamageFX;
    [Space(5)]

    [Header("Enemy Movement Settings")]
    [SerializeField] protected float speed;
    [SerializeField] protected Transform pointA, pointB;
    protected Transform currentPoint;
    [Space(5)]

    [Header("Enemy Attack Settings")]
    [SerializeField] protected float enemyDamage;
    [Space(5)]

    [Header("Enemy Recoil Settings")]
    [SerializeField] protected float recoilLength;
    [SerializeField] protected float recoilStrength;
    [SerializeField] protected bool isRecoiling = false;
    protected float recoilTimer;

    // Start is called before the first frame update
    protected virtual void Start()
    {

    }

    protected virtual void Awake()
    {
        enemyRB = GetComponent<Rigidbody2D>(); //Gets the rigid body component

        enemySR = GetComponent<SpriteRenderer>(); //Gets the sprite renderer component

        enemyAnim = GetComponent<Animator>(); //Gets the animator component

        enemyState = GetComponent<EnemyState>(); //Gets the enemy state script component

        player = PlayerController.Instance; //Sets the player variable to the active player instance

        currentPoint = pointB.transform; //Sets the current point for the enemy to walk towards on it's patrol path
    }
    protected virtual void Update()
    {

        if (enemyHealth <= 0)
        {
            Destroy(gameObject); //If the enemy's health hits 0 it kills the enemy gameobject
        }
        if(isRecoiling)
        {
            if(recoilTimer < recoilLength)
            {
                recoilTimer += Time.deltaTime; //Increases the recoil timer if it hasn't hit the max time spent recoiling
            }
            else
            {
                isRecoiling = false; //Stops the enemy from recoiling and resets the timer for when it is hit next
                recoilTimer = 0;
            }
        }

        if (!isRecoiling)
        {
            EnemyPatrol(); //Triggers the enemy patrol movement as well as animation if the enemy is NOT recoiling
            enemyAnim.SetBool("Walking", true);
        }
        else
        {
            enemyAnim.SetBool("Walking", false); //Pauses the animation if the enemy is hit and is recoiling
        }
    }

    private void OnDrawGizmos() //Used to draw Gizmos in the editor to make editing the patrol path easier
    {
        Gizmos.DrawWireSphere(pointA.transform.position, 0.5f);
        Gizmos.DrawWireSphere(pointB.transform.position, 0.5f);
        Gizmos.DrawLine(pointA.transform.position, pointB.transform.position);
    }

    public virtual void enemyHit(float damageDone, Vector2 hitDirection, float hitStrength)
    {
        enemyHealth -= damageDone; //Descreses the enemy health
        enemyHealthBarFill.transform.localScale = new Vector2(enemyHealth, 0.2f); //Decreases the size of the enemy healthbar to match the current health
        if(!isRecoiling)
        {
            GameObject enemyDamageParticles = Instantiate(enemyDamageFX, transform.position, Quaternion.identity); //Spawn enemy hit particle effects
            enemyRB.AddForce(-hitStrength * recoilStrength * hitDirection); //Recoils the enemy in the direction the hit comes from
            Destroy(enemyDamageParticles, 1.5f); //Destroys enemy FX after 1.5 seconds
        }
        
    }
    protected virtual void Attack()
    {
        PlayerController.Instance.TakeDamage(enemyDamage); //If the enemy attacks, damage the player by their damage amount
    }

    protected void OnCollisionStay2D(Collision2D target) //Triggers when the enemy collides with the player
    {
        if (target.gameObject.CompareTag("Player") && !PlayerController.Instance.playerState.invincibleState) 
        {
            //If the player is not invincible and the enemy collides with it, it will attack the player and trigger the enemy's attacking animation
            Attack();
            enemyAnim.SetTrigger("Attacking");
            PlayerController.Instance.TimeSlow(0, 5, 0.5f); //Slows time when the player takes damage
        }

    }

    protected virtual void EnemyPatrol()
    {
        if(currentPoint == pointB.transform)
        {
            enemyRB.velocity = new Vector2(speed, 0); //Moves the enemy towards the point B
        }
        else
        {
            enemyRB.velocity = new Vector2(-speed, 0); //Moves the enemy towards the point A
        }

        if(Vector2.Distance(transform.position, currentPoint.position) < 0.5f && currentPoint == pointB.transform) //Checks if the enemy has reached point B
        {
            currentPoint = pointA.transform; //Changes the active point
            transform.localScale = new Vector2(-2, transform.localScale.y); //Flips the enemy
        }
        if (Vector2.Distance(transform.position, currentPoint.position) < 0.5f && currentPoint == pointA.transform) //^^
        {
            currentPoint = pointB.transform; //Changes the active point
            transform.localScale = new Vector2(2, transform.localScale.y); //^^
        }

    }
}
