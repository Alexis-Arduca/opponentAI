using UnityEngine;

/// <summary>
/// Bokoblin AI inheriting from Enemy, with customized stats and animations.
/// </summary>
public class Bokoblin : Enemy
{
    private Animator animator;

    protected override void Start()
    {
        base.Start();

        // maxHealth = 150;
        // currentHealth = maxHealth;
        // damage = 10;
        // moveSpeed = 5f;
        // detectionRange = 5f;
        // attackRange = 1.5f;
        // safeDistance = 2f;
        // patrolSpeed = 5f;
        // decisionInterval = 1f;
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
            case State.Patrolling:
                velocity = patrolDirection;
                isMoving = true;
                break;
            case State.Chasing:
            case State.Attacking:
            case State.Defensive:
                if (target != null)
                {
                    velocity = (target.position - transform.position).normalized;
                    isMoving = true;
                }
                break;
            case State.Stunned:
                isMoving = false;
                break;
        }

        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsAttacking", currentState == State.Attacking);
            animator.SetBool("IsStunned", currentState == State.Stunned);
            if (isMoving)
            {
                animator.SetFloat("MoveX", velocity.x);
                animator.SetFloat("MoveY", velocity.y);
            }
        }

        if (velocity.x < 0)
        {
            transform.localScale = new Vector3(-2, 2, 2);
        }
        else if (velocity.x > 0)
        {
            transform.localScale = new Vector3(2, 2, 2);
        }
    }

    protected override void UpdateStatsDisplay()
    {
        if (statsText != null)
        {
            statsText.text = $"Bokoblin {name} Stats\n" +
                             $"Health: {currentHealth}/{maxHealth}\n" +
                             $"State: {currentState}\n" +
                             $"Patrolling: {timeInPatrolling:F2} s\n" +
                             $"Chasing: {timeInChasing:F2} s\n" +
                             $"Attacking: {timeInAttacking:F2} s\n" +
                             $"Defensive: {timeInDefensive:F2} s\n" +
                             $"Recovering: {timeInRecovering:F2} s\n" +
                             $"Stunned: {timeInStunned:F2} s";
        }
    }
}
