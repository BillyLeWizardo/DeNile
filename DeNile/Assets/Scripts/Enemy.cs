using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    [SerializeField] protected float enemyHealth;
    [SerializeField] protected float recoilLength;
    [SerializeField] protected float recoilStrength;
    protected float recoilTimer;

    [SerializeField] protected bool isRecoiling = false;

    protected Rigidbody2D enemyRB;

    [SerializeField] protected PlayerController player;
    [SerializeField] protected float speed;

    [SerializeField] protected float enemyDamage;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    protected virtual void Awake()
    {
        enemyRB = GetComponent<Rigidbody2D>();
        player = PlayerController.Instance;
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
    }

    public virtual void enemyHit(float damageDone, Vector2 hitDirection, float hitStrength)
    {
        enemyHealth -= damageDone;
        if(!isRecoiling)
        {
            enemyRB.AddForce(-hitStrength * recoilStrength * hitDirection); //Recoils the enemy in the direction the hit comes from
        }
    }

    protected void OnCollisionStay2D(Collision2D target)
    {
        if (target.gameObject.CompareTag("Player") && !PlayerController.Instance.playerState.invincibleState)
        {
            Attack();
            PlayerController.Instance.TimeSlow(0, 5, 0.5f);
        }
    }
    protected virtual void Attack()
    {
        PlayerController.Instance.TakeDamage(enemyDamage);
    }
}
