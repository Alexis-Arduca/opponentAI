using UnityEngine;

public class Bokoblin : Enemy
{
    public float retreatDistance;
    public float safeDistance;
    public Transform swordHitbox;
    public float knockbackForce;

    private Animator animator;

    protected override void Start()
    {
        base.Start();
        health = 50;
        speed = 2f;
        damage = 10;
        detectionRange = 5f;
        attackRange = 1.5f;
        patrolSpeed = 1.5f;
        retreatDistance = 2f;
        safeDistance = 3f;
        knockbackForce = 2f;

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

        if (currentState == EnemyState.Patrolling)
        {
            velocity = patrolDirection;
        }
        else if (currentTarget != null)
        {
            velocity = (currentTarget.transform.position - transform.position).normalized;
        }

        if (currentState == EnemyState.Chasing || currentState == EnemyState.Patrolling)
        {
            animator.SetFloat("MoveX", velocity.x);
            animator.SetFloat("MoveY", velocity.y);
            animator.SetBool("IsMoving", true);

            if (velocity.x < 0)
            {
                transform.localScale = new Vector3(-2, 2, 2);
            }
            else if (velocity.x > 0)
            {
                transform.localScale = new Vector3(2, 2, 2);
            }
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }
    }

    protected override void IdleBehavior(float distanceToTarget)
    {
        base.IdleBehavior(distanceToTarget);
    }

    protected override void PatrolBehavior(float distanceToTarget)
    {
        base.PatrolBehavior(distanceToTarget);
    }

    protected override void ChaseBehavior(float distanceToTarget)
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Patrolling;
            return;
        }

        if (distanceToTarget > detectionRange)
        {
            currentState = EnemyState.Patrolling;
        }
        else if (distanceToTarget < retreatDistance)
        {
            Vector2 direction = (transform.position - currentTarget.transform.position).normalized;
            transform.position = Vector2.MoveTowards(transform.position, transform.position + (Vector3)direction, speed * Time.deltaTime);
        }
        else if (distanceToTarget > safeDistance)
        {
            Vector2 direction = (currentTarget.transform.position - transform.position).normalized;
            transform.position = Vector2.MoveTowards(transform.position, currentTarget.transform.position, speed * Time.deltaTime);
        }
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
