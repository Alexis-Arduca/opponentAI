using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class Enemy : MonoBehaviour
{
    public enum EnemyState { Idle, Patrolling, Chasing, Attacking, Frozen }
    protected EnemyState currentState;

    public int health;
    public float speed;
    public int damage;
    public float detectionRange;
    public float attackRange;
    public float patrolSpeed;
    public float attackCooldown;
    public float patrolPauseDuration = 2f;
    public float freezeDuration = 3f;

    protected float attackTimer;

    protected Vector2 patrolDirection;
    protected Vector2 lastDirection;
    protected float patrolPauseTimer;
    protected float patrolMoveTimer;
    protected Vector2[] directions = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    protected Enemy currentTarget;
    private Rigidbody2D rb;

    protected virtual void Start()
    {
        currentState = EnemyState.Idle;
        attackTimer = 0;

        rb = GetComponent<Rigidbody2D>();
        SetNewPatrolDirection();
    }

    protected virtual void Update()
    {
        UpdateTarget();

        float distanceToTarget = currentTarget != null ? Vector2.Distance(transform.position, currentTarget.transform.position) : Mathf.Infinity;

        switch (currentState)
        {
            case EnemyState.Idle:
                IdleBehavior(distanceToTarget);
                break;
            case EnemyState.Patrolling:
                PatrolBehavior(distanceToTarget);
                break;
            case EnemyState.Chasing:
                ChaseBehavior(distanceToTarget);
                break;
            case EnemyState.Attacking:
                AttackBehavior(distanceToTarget);
                break;
        }
    }

    protected virtual void UpdateTarget()
    {
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        currentTarget = allEnemies
            .Where(e => e != this && e.health > 0)
            .OrderBy(e => Vector2.Distance(transform.position, e.transform.position))
            .FirstOrDefault(e => Vector2.Distance(transform.position, e.transform.position) <= detectionRange);
    }

    public virtual void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log(name + " took damage: " + damage + ". Remaining health: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    public event Action OnDeath;
    protected virtual void Die()
    {
        Debug.Log(name + " died!");
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>
    /// Handle idle state (basically just pass in patrol mode)
    /// </summary>
    /// <param name="distanceToTarget"></param>
    protected virtual void IdleBehavior(float distanceToTarget)
    {
        currentState = EnemyState.Patrolling;
    }

    /// <summary>
    /// Handle patrol state until we find an enemy
    /// </summary>
    /// <param name="distanceToTarget"></param>
    protected virtual void PatrolBehavior(float distanceToTarget)
    {
        if (patrolPauseTimer > 0)
        {
            patrolPauseTimer -= Time.deltaTime;
            return;
        }

        transform.position += (Vector3)(patrolDirection * patrolSpeed * Time.deltaTime);
        patrolMoveTimer -= Time.deltaTime;

        if (patrolMoveTimer <= 0)
        {
            patrolPauseTimer = patrolPauseDuration;
            SetNewPatrolDirection();
        }

        if (currentTarget != null && distanceToTarget <= detectionRange)
        {
            currentState = EnemyState.Chasing;
        }
    }

    /// <summary>
    /// Select randomly a new direction to go for the patrol
    /// </summary>
    protected void SetNewPatrolDirection()
    {
        Vector2 randomDirection;
        do
        {
            randomDirection = directions[UnityEngine.Random.Range(0, directions.Length)];
        } while (randomDirection == lastDirection);

        lastDirection = randomDirection;
        patrolDirection = randomDirection;
        patrolMoveTimer = UnityEngine.Random.Range(1f, 3f);
    }

    /// <summary>
    /// Handle chase state until we are close to the enemy
    /// </summary>
    /// <param name="distanceToTarget"></param>
    protected virtual void ChaseBehavior(float distanceToTarget)
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Patrolling;
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, currentTarget.transform.position, speed * Time.deltaTime);

        if (distanceToTarget <= attackRange)
        {
            currentState = EnemyState.Attacking;
        }
    }

    /// <summary>
    /// Handle fight state until someone is not in range
    /// </summary>
    /// <param name="distanceToTarget"></param>
    protected virtual void AttackBehavior(float distanceToTarget)
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Patrolling;
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            attackTimer = attackCooldown;
            currentTarget.TakeDamage(damage);
            Debug.Log(name + " attacked " + currentTarget.name);
        }

        if (distanceToTarget > attackRange)
        {
            currentState = EnemyState.Chasing;
        }
    }
}
