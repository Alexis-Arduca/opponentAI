using UnityEngine;

public class Bokoblin : Enemy
{
    public Transform swordHitbox;
    public float knockbackForce = 2f;

    private Animator animator;

    /// <summary>
    /// Initiate enemy statistics (can be change trough the inspector)
    /// </summary>
    protected override void Start()
    {
        base.Start();
        health = 150;
        speed = 2f;
        damage = 10;
        detectionRange = 5f;
        attackRange = 1.5f;
        patrolSpeed = 1.5f;
        knockbackForce = 2f;

        animator = GetComponent<Animator>();
    }

    protected override void Update()
    {
        base.Update();
        UpdateAnimation();
    }

    /// <summary>
    /// Handle Everything about sprite animation
    /// </summary>
    private void UpdateAnimation()
    {
        Vector2 velocity = Vector2.zero;

        if (currentState == EnemyState.Patrolling)
            velocity = patrolDirection;
        else if (currentTarget != null)
            velocity = (currentTarget.transform.position - transform.position).normalized;

        if (currentState == EnemyState.Chasing || currentState == EnemyState.Patrolling)
        {
            animator.SetFloat("MoveX", velocity.x);
            animator.SetFloat("MoveY", velocity.y);
            animator.SetBool("IsMoving", true);

            if (velocity.x < 0)
                transform.localScale = new Vector3(-2, 2, 2);
            else if (velocity.x > 0)
                transform.localScale = new Vector3(2, 2, 2);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }
    }

    /// <summary>
    /// Override ChaseBehavior to ensure initiative from opponent
    /// </summary>
    /// <param name="distanceToTarget"></param>
    protected override void ChaseBehavior(float distanceToTarget)
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Patrolling;
            return;
        }

        if (distanceToTarget <= attackRange)
        {
            currentState = EnemyState.Attacking;
            return;
        }

        if (distanceToTarget > detectionRange)
        {
            currentState = EnemyState.Patrolling;
        }

        transform.position = Vector2.MoveTowards(transform.position, currentTarget.transform.position, speed * Time.deltaTime);
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("EnemyWeapon"))
        {
            Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
            transform.position += (Vector3)knockbackDirection * knockbackForce;

            TakeDamage(10);
        }
    }
}
