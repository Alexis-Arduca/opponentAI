using UnityEngine;
using System;

/// <summary>
/// Basic enemy logic: patrol, chase, attack, retreat, and die. Damage is handled by the weapon.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public enum EnemyState { Idle, Patrolling, Chasing, Attacking, Retreating, Defensive }

    [Header("Stats")]
    public int health = 100;
    public int damage = 5;
    public float moveSpeed = 2f;
    public float detectionRange = 5f;
    public float patrolSpeed = 1.5f;
    public float patrolChangeInterval = 3f;
    public float knockbackForce = 2f;

    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float attackSpeed = 3f;
    public float attackDuration = 0.5f;
    public float retreatDistance = 1f;
    public float retreatDuration = 0.3f;
    protected float attackTimer;
    protected bool isRetreatingAfterAttack;

    [Header("Patrol Settings")]
    public float patrolPauseDuration = 2f;
    protected float patrolPauseTimer;
    protected float patrolMoveTimer;
    protected Vector2 patrolDirection;
    protected Vector2 lastPatrolDirection;

    [Header("Personality")]
    [Range(0f, 1f)]
    public float aggressionLevel = 0.5f;
    [Range(0f, 1f)]
    public float courageLevel = 0.5f;
    [Range(0f, 1f)]
    public float tacticalLevel = 0.5f;

    [Header("State Time Tracking")]
    protected float timeInIdle;
    protected float timeInPatrolling;
    protected float timeInChasing;
    protected float timeInAttacking;
    protected float timeInRetreating;
    protected float timeInDefensive;

    protected EnemyState currentState;
    protected Rigidbody2D rb;
    protected Transform currentTarget;
    protected Transform swordHitbox;

    private Vector2[] possibleDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentState = EnemyState.Idle;
        SetNewPatrolDirection();
        attackTimer = 0f;
        isRetreatingAfterAttack = false;

        timeInIdle = 0f;
        timeInPatrolling = 0f;
        timeInChasing = 0f;
        timeInAttacking = 0f;
        timeInRetreating = 0f;
        timeInDefensive = 0f;
    }

    protected virtual void Update()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                timeInIdle += Time.deltaTime;
                break;
            case EnemyState.Patrolling:
                timeInPatrolling += Time.deltaTime;
                break;
            case EnemyState.Chasing:
                timeInChasing += Time.deltaTime;
                break;
            case EnemyState.Attacking:
                timeInAttacking += Time.deltaTime;
                break;
            case EnemyState.Retreating:
                timeInRetreating += Time.deltaTime;
                break;
            case EnemyState.Defensive:
                timeInDefensive += Time.deltaTime;
                break;
        }

        UpdateTarget();

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Patrolling:
                HandlePatrolling();
                break;
            case EnemyState.Chasing:
                HandleChasing();
                break;
            case EnemyState.Attacking:
                HandleAttacking();
                break;
            case EnemyState.Retreating:
                HandleRetreating();
                break;
            case EnemyState.Defensive:
                HandleDefensive();
                break;
        }
    }

    protected virtual void UpdateTarget()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (var enemy in enemies)
        {
            if (enemy != this && enemy.health > 0)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < detectionRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = enemy.transform;
                }
            }
        }

        currentTarget = closestTarget;

        if (currentTarget != null)
        {
            float healthRatio = (float)health / 100f;
            float distance = Vector2.Distance(transform.position, currentTarget.position);

            if (healthRatio < 0.3f && courageLevel < 0.4f)
            {
                currentState = EnemyState.Retreating;
            }
            else if (distance < attackRange && aggressionLevel > 0.6f)
            {
                currentState = EnemyState.Attacking;
            }
            else if (healthRatio < 0.5f && tacticalLevel > 0.5f)
            {
                currentState = EnemyState.Defensive;
            }
            else
            {
                currentState = EnemyState.Chasing;
            }
        }
        else
        {
            currentState = EnemyState.Patrolling;
        }
    }

    protected virtual void HandleIdle()
    {
        currentState = EnemyState.Patrolling;
    }

    protected virtual void HandlePatrolling()
    {
        if (patrolPauseTimer > 0)
        {
            patrolPauseTimer -= Time.deltaTime;
            rb.velocity = Vector2.zero;
            return;
        }

        rb.MovePosition(rb.position + patrolDirection * patrolSpeed * Time.deltaTime);
        patrolMoveTimer -= Time.deltaTime;

        if (patrolMoveTimer <= 0)
        {
            patrolPauseTimer = patrolPauseDuration;
            SetNewPatrolDirection();
        }
    }

    protected virtual void HandleChasing()
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Patrolling;
            return;
        }

        float distance = Vector2.Distance(transform.position, currentTarget.position);
        if (distance > detectionRange)
        {
            currentTarget = null;
            currentState = EnemyState.Patrolling;
            return;
        }

        Vector2 direction = (currentTarget.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.deltaTime);
    }

    protected virtual void HandleAttacking()
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Patrolling;
            return;
        }

        float distance = Vector2.Distance(transform.position, currentTarget.position);
        if (distance > attackRange && !isRetreatingAfterAttack)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        attackTimer -= Time.deltaTime;

        if (!isRetreatingAfterAttack)
        {
            Vector2 direction = (currentTarget.position - transform.position).normalized;
            rb.MovePosition(rb.position + direction * attackSpeed * Time.deltaTime);

            if (attackTimer <= 0)
            {
                isRetreatingAfterAttack = true;
                attackTimer = retreatDuration;
            }
        }
        else
        {
            Vector2 retreatDirection = (transform.position - currentTarget.position).normalized;
            rb.MovePosition(rb.position + retreatDirection * moveSpeed * Time.deltaTime);

            if (attackTimer <= 0)
            {
                isRetreatingAfterAttack = false;
                attackTimer = attackDuration;
                currentState = EnemyState.Chasing;
            }
        }
    }

    protected virtual void HandleRetreating()
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Patrolling;
            return;
        }

        Vector2 retreatDirection = (transform.position - currentTarget.position).normalized;
        rb.MovePosition(rb.position + retreatDirection * moveSpeed * 1.2f * Time.deltaTime);

        if ((float)health / 100f > 0.5f)
        {
            currentState = EnemyState.Patrolling;
        }
    }

    protected virtual void HandleDefensive()
    {
        if (currentTarget == null)
        {
            currentState = EnemyState.Patrolling;
            return;
        }

        float distance = Vector2.Distance(transform.position, currentTarget.position);
        float safeDistance = 3f;
        Vector2 direction = (currentTarget.position - transform.position).normalized;

        if (distance < safeDistance)
        {
            rb.MovePosition(rb.position - direction * moveSpeed * 0.8f * Time.deltaTime);
        }
        else if (distance > safeDistance + 1f)
        {
            rb.MovePosition(rb.position + direction * moveSpeed * 0.5f * Time.deltaTime);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }

        if ((float)health / 100f > 0.7f && aggressionLevel > 0.5f)
        {
            currentState = EnemyState.Chasing;
        }
    }

    protected void SetNewPatrolDirection()
    {
        Vector2 newDirection;
        do
        {
            newDirection = possibleDirections[UnityEngine.Random.Range(0, possibleDirections.Length)];
        } while (newDirection == lastPatrolDirection);

        lastPatrolDirection = newDirection;
        patrolDirection = newDirection;
        patrolMoveTimer = patrolChangeInterval;
    }

    public virtual void TakeDamage(int damage, Collider2D collision)
    {
        health -= damage;
        Debug.Log(name + " took damage: " + damage + ". Remaining health: " + health);

        if (health <= 0)
        {
            Die();
        }
        else
        {
            if (collision != null)
            {
                Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
                rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        Enemy otherEnemy = collision.collider.GetComponent<Enemy>();
        if (otherEnemy != null)
        {
            Vector2 knockbackDirection = (transform.position - otherEnemy.transform.position).normalized;
            rb.AddForce(knockbackDirection * knockbackForce * 0.5f, ForceMode2D.Impulse);
        }
    }

    public event Action OnDeath;
    protected virtual void Die()
    {
        Debug.Log(name + " died!");
        
        Debug.Log($"{name} Time Spent in States:");
        Debug.Log($"Idle: {timeInIdle:F2} seconds");
        Debug.Log($"Patrolling: {timeInPatrolling:F2} seconds");
        Debug.Log($"Chasing: {timeInChasing:F2} seconds");
        Debug.Log($"Attacking: {timeInAttacking:F2} seconds");
        Debug.Log($"Retreating: {timeInRetreating:F2} seconds");
        Debug.Log($"Defensive: {timeInDefensive:F2} seconds");

        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
