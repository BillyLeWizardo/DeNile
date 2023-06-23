using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sobek : Enemy
{
    // Start is called before the first frame update
    protected override void Start()
    {
        enemyRB.gravityScale = 12f;
    }

    protected override void Awake()
    {
        base.Awake();
    }
    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if (!isRecoiling)
        {
            transform.position = Vector2.MoveTowards(transform.position, new Vector2(PlayerController.Instance.transform.position.x,
                transform.position.y), speed * Time.deltaTime);
        }
    }

    public override void enemyHit(float damageDone, Vector2 hitDirection, float hitStrength)
    {
        base.enemyHit(damageDone, hitDirection, hitStrength);
    }
}
