using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class Enemy : MonoBehaviour
{
    public enum EnemyState { Idle, Patrolling, Chasing, Attacking }
    public enum EnemyAction { Attack, Retreat, Wait, Reposition }

    protected EnemyState currentState;
    public int health;
    public float speed;
    public int damage;
    public float detectionRange;
    public float attackRange;
    public float patrolSpeed;
    public float attackCooldown;
    public float patrolPauseDuration = 2f;

    protected float attackTimer;
    protected Vector2 patrolDirection;
    protected Vector2 lastDirection;
    protected float patrolPauseTimer;
    protected float patrolMoveTimer;
    protected Vector2[] directions = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    protected Enemy currentTarget;
    private Rigidbody2D rb;

    protected float decisionCooldown = 1.5f;
    protected float decisionTimer;
    protected System.Random random;

    [Header("AI Seed (Optional)")]
    public int seed = -1; // -1 = random

    protected virtual void Start()
    {
        currentState = EnemyState.Idle;
        attackTimer = 0;
        decisionTimer = decisionCooldown;
        rb = GetComponent<Rigidbody2D>();
        SetNewPatrolDirection();

        // Seed Option
        if (seed == -1) {
            random = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
            Debug.Log("Current Opponent Seed: " + random);
        } else {
            random = new System.Random(seed);
        }
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
            Die();
    }

    public event Action OnDeath;
    protected virtual void Die()
    {
        Debug.Log(name + " died!");
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>
    /// Handle idle behavior (will just switch to patrol)
    /// </summary>
    /// <param name="distanceToTarget"></param>
    protected virtual void IdleBehavior(float distanceToTarget)
    {
        currentState = EnemyState.Patrolling;
    }

    /// <summary>
    /// Handle patrol behavior until we find an enemy
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
            currentState = EnemyState.Chasing;
    }

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
    /// Handle chase behavior until the enemy leave the range, or be in range for an attack
    /// </summary>
    /// <param name="distanceToTarget"></param>
    protected virtual void ChaseBehavior(float distanceToTarget)
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

        transform.position = Vector2.MoveTowards(transform.position, currentTarget.transform.position, speed * Time.deltaTime);

        if (distanceToTarget > detectionRange)
        {
            currentState = EnemyState.Patrolling;
        }
    }

    /// <summary>
    /// Handle attack behavior
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
            currentState = EnemyState.Chasing;
    }

    /// <summary>
    /// Will let the ia choose the next action
    /// </summary>
    /// <param name="distanceToTarget"></param>
    protected virtual void MakeDecision(float distanceToTarget)
    {
        decisionTimer -= Time.deltaTime;
        if (decisionTimer > 0) return;

        decisionTimer = decisionCooldown;

        if (distanceToTarget <= attackRange)
        {
            currentState = EnemyState.Attacking;
            return;
        }

        EnemyAction action = (EnemyAction)random.Next(0, Enum.GetNames(typeof(EnemyAction)).Length);

        switch (action)
        {
            case EnemyAction.Attack:
                TryAttack(distanceToTarget);
                break;
            case EnemyAction.Retreat:
                Retreat();
                break;
            case EnemyAction.Wait:
                Wait();
                break;
            case EnemyAction.Reposition:
                Reposition();
                break;
        }
    }


    protected virtual void TryAttack(float distanceToTarget)
    {
        if (distanceToTarget <= attackRange)
        {
            currentState = EnemyState.Attacking;
        }
    }

    protected virtual void Retreat()
    {
        if (currentTarget != null)
        {
            Vector2 direction = (transform.position - currentTarget.transform.position).normalized;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }

    protected virtual void Wait()
    {
        // Enemy don't move
    }

    protected virtual void Reposition()
    {
        if (currentTarget != null)
        {
            Vector2 lateral = Vector2.Perpendicular((currentTarget.transform.position - transform.position).normalized);
            float side = random.Next(0, 2) == 0 ? 1f : -1f;
            transform.position += (Vector3)(lateral * side * speed * 0.5f * Time.deltaTime);
        }
    }
}
