using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Sobek : Enemy
{

    protected override void Start()
    {

    }

    protected override void Awake()
    {
        base.Awake();
        enemyRB.gravityScale = 12f; //Sets the gravity scale
    }

    protected override void Update()
    {
        base.Update();
        //FlipEnemy();
    }

    //void FlipEnemy() THIS IS NOT IN USE ANYMORE!
    //{
    //    if (transform.position.x > PlayerController.Instance.transform.position.x)
    //    {
    //        transform.localScale = new Vector2(-2, transform.localScale.y);
    //    }
    //    else if (transform.position.x < PlayerController.Instance.transform.position.x)
    //    {
    //        transform.localScale = new Vector2(2, transform.localScale.y);
    //    }
    //}

    public override void enemyHit(float damageDone, Vector2 hitDirection, float hitStrength)
    {
        base.enemyHit(damageDone, hitDirection, hitStrength); //If the enemy 'SOBEK' is hit, trigger it according to the base enemy class with these variables
    }
}
