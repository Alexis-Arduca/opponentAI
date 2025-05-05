using UnityEngine;

/// <summary>
/// Bokoblin behavior: enemy with patrol, chase, attack, retreat, and defensive states.
/// </summary>
public class Bokoblin : Enemy
{
    private Animator animator;

    protected override void Start()
    {
        base.Start();

        health = 150;
        moveSpeed = 2f;
        damage = 10;
        detectionRange = 5f;
        patrolSpeed = 1.5f;
        attackSpeed = 3.5f;
        attackDuration = 0.5f;
        retreatDistance = 1f;
        retreatDuration = 0.3f;
        aggressionLevel = 0.7f;
        courageLevel = 0.6f;
        tacticalLevel = 0.4f;

        animator = GetComponent<Animator>();
    }

    protected override void Update()
    {
        base.Update();
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        Vector2 velocity = Vector2.zero;
        bool isMoving = false;

        switch (currentState)
        {
            case EnemyState.Patrolling:
                velocity = patrolDirection;
                isMoving = true;
                break;
            case EnemyState.Chasing:
            case EnemyState.Attacking:
            case EnemyState.Defensive:
                if (currentTarget != null)
                {
                    velocity = (currentTarget.position - transform.position).normalized;
                    isMoving = true;
                }
                break;
            case EnemyState.Retreating:
                if (currentTarget != null)
                {
                    velocity = (transform.position - currentTarget.position).normalized;
                    isMoving = true;
                }
                break;
        }

        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsRetreating", currentState == EnemyState.Retreating);

        if (isMoving)
        {
            animator.SetFloat("MoveX", velocity.x);
            animator.SetFloat("MoveY", velocity.y);

            if (velocity.x < 0)
            {
                transform.localScale = new Vector3(-2, 2, 2);
            }
            else if (velocity.x > 0)
            {
                transform.localScale = new Vector3(2, 2, 2);
            }
        }
    }

    protected override void HandleIdle()
    {
        base.HandleIdle();
    }

    protected override void HandlePatrolling()
    {
        base.HandlePatrolling();
    }

    protected override void UpdateTarget()
    {
        base.UpdateTarget();
    }

    protected override void HandleChasing()
    {
        base.HandleChasing();
    }

    protected override void HandleAttacking()
    {
        base.HandleAttacking();
    }

    protected override void HandleRetreating()
    {
        base.HandleRetreating();
    }

    protected override void HandleDefensive()
    {
        base.HandleDefensive();
    }

    public override void TakeDamage(int damage, Collider2D collision)
    {
        base.TakeDamage(damage, collision);
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
    }

    protected override void Die()
    {
        base.Die();
    }
}