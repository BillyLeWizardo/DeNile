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
        enemyRB = GetComponent<Rigidbody2D>();

        enemySR = GetComponent<SpriteRenderer>();

        enemyAnim = GetComponent<Animator>();

        enemyState = GetComponent<EnemyState>();

        player = PlayerController.Instance;

        currentPoint = pointB.transform;
    }
    protected virtual void Update()
    {

        if (enemyHealth <= 0)
        {
            Destroy(gameObject);
        }
        if(isRecoiling)
        {
            if(recoilTimer < recoilLength)
            {
                recoilTimer += Time.deltaTime;
            }
            else
            {
                isRecoiling = false;
                recoilTimer = 0;
            }
        }

        if (!isRecoiling)
        {
            EnemyPatrol();
            enemyAnim.SetBool("Walking", true);
        }
        else
        {
            enemyAnim.SetBool("Walking", false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(pointA.transform.position, 0.5f);
        Gizmos.DrawWireSphere(pointB.transform.position, 0.5f);
        Gizmos.DrawLine(pointA.transform.position, pointB.transform.position);
    }

    public virtual void enemyHit(float damageDone, Vector2 hitDirection, float hitStrength)
    {
        enemyHealth -= damageDone;
        enemyHealthBarFill.transform.localScale = new Vector2(enemyHealth, 0.2f);
        if(!isRecoiling)
        {
            GameObject enemyDamageParticles = Instantiate(enemyDamageFX, transform.position, Quaternion.identity);
            enemyRB.AddForce(-hitStrength * recoilStrength * hitDirection); //Recoils the enemy in the direction the hit comes from
            Destroy(enemyDamageParticles, 1.5f);
        }
        
    }
    protected virtual void Attack()
    {
        PlayerController.Instance.TakeDamage(enemyDamage);
    }

    protected void OnCollisionStay2D(Collision2D target)
    {
        if (target.gameObject.CompareTag("Player") && !PlayerController.Instance.playerState.invincibleState)
        {
            Attack();
            enemyAnim.SetTrigger("Attacking");
            PlayerController.Instance.TimeSlow(0, 5, 0.5f);
        }

    }

    protected virtual void EnemyPatrol()
    {
        Vector2 point = currentPoint.position - transform.position;

        if(currentPoint == pointB.transform)
        {
            enemyRB.velocity = new Vector2(speed, 0);
        }
        else
        {
            enemyRB.velocity = new Vector2(-speed, 0);
        }

        if(Vector2.Distance(transform.position, currentPoint.position) < 0.5f && currentPoint == pointB.transform)
        {
            currentPoint = pointA.transform;
            transform.localScale = new Vector2(-2, transform.localScale.y);
        }
        if (Vector2.Distance(transform.position, currentPoint.position) < 0.5f && currentPoint == pointA.transform)
        {
            currentPoint = pointB.transform;
            transform.localScale = new Vector2(2, transform.localScale.y);
        }

    }
}
