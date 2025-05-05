using UnityEngine;
using TMPro;

/// <summary>
/// Base enemy AI with probabilistic decision-making for attack and defense.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public enum State { Patrolling, Chasing, Attacking, Defensive }

    [Header("Stats")]
    [SerializeField] protected int health = 100;
    [SerializeField] public int damage = 5;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float detectionRange = 5f;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float safeDistance = 3f;

    [Header("Personality")]
    [Range(0f, 1f)] [SerializeField] protected float aggressionLevel = 0.5f;
    [Range(0f, 1f)] [SerializeField] protected float courageLevel = 0.5f;
    [Range(0f, 1f)] [SerializeField] protected float tacticalLevel = 0.5f;

    [Header("Decision")]
    [SerializeField] protected float decisionInterval = 1.5f;
    protected float decisionTimer;

    [Header("Patrol")]
    [SerializeField] protected float patrolSpeed = 1.5f;
    [SerializeField] protected float patrolChangeInterval = 3f;
    protected float patrolTimer;
    protected Vector2 patrolDirection;

    [Header("UI")]
    [SerializeField] protected TextMeshProUGUI statsText;

    [Header("State Time Tracking")]
    protected float timeInPatrolling;
    protected float timeInChasing;
    protected float timeInAttacking;
    protected float timeInDefensive;

    protected State currentState;
    protected Rigidbody2D rb;
    protected Transform target;
    private static readonly Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentState = State.Patrolling;
        decisionTimer = decisionInterval;
        patrolTimer = patrolChangeInterval;
        SetNewPatrolDirection();

        if (statsText == null)
        {
            statsText = GameObject.Find("EnemyStatsText")?.GetComponent<TextMeshProUGUI>();
        }
    }

    protected virtual void Update()
    {
        switch (currentState)
        {
            case State.Patrolling: timeInPatrolling += Time.deltaTime; break;
            case State.Chasing: timeInChasing += Time.deltaTime; break;
            case State.Attacking: timeInAttacking += Time.deltaTime; break;
            case State.Defensive: timeInDefensive += Time.deltaTime; break;
        }

        UpdateStatsDisplay();

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            DecideState();
            decisionTimer = decisionInterval;
        }

        switch (currentState)
        {
            case State.Patrolling: HandlePatrolling(); break;
            case State.Chasing: HandleChasing(); break;
            case State.Attacking: HandleAttacking(); break;
            case State.Defensive: HandleDefensive(); break;
        }
    }

    protected virtual void UpdateStatsDisplay()
    {
        if (statsText != null)
        {
            statsText.text = $"{name} Stats\n" +
                             $"Health: {health}\n" +
                             $"Patrolling: {timeInPatrolling:F2} s\n" +
                             $"Chasing: {timeInChasing:F2} s\n" +
                             $"Attacking: {timeInAttacking:F2} s\n" +
                             $"Defensive: {timeInDefensive:F2} s";
        }
    }

    protected virtual void DecideState()
    {
        target = null;
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        float closestDistance = detectionRange;

        foreach (var enemy in enemies)
        {
            if (enemy != this && enemy.health > 0)
            {
                float distances = Vector2.Distance(transform.position, enemy.transform.position);
                if (distances < closestDistance)
                {
                    closestDistance = distances;
                    target = enemy.transform;
                }
            }
        }

        if (target == null)
        {
            currentState = State.Patrolling;
            return;
        }

        float healthRatio = (float)health / 100f;
        float distance = Vector2.Distance(transform.position, target.position);

        float retreatChance = (1f - courageLevel) * (1f - healthRatio);
        if (Random.value < retreatChance && healthRatio < 0.3f)
        {
            currentState = State.Defensive;
            return;
        }

        if (distance < attackRange)
        {
            float attackChance = aggressionLevel * healthRatio * (1f - 0.5f * tacticalLevel);
            float defensiveChance = tacticalLevel + (1f - aggressionLevel) * (1f - healthRatio);

            float total = attackChance + defensiveChance;
            attackChance /= total;
            defensiveChance /= total;

            currentState = Random.value < attackChance ? State.Attacking : State.Defensive;
        }
        else
        {
            float chaseChance = aggressionLevel * healthRatio;
            float defensiveChance = tacticalLevel + (1f - aggressionLevel) * (1f - healthRatio);

            float total = chaseChance + defensiveChance;
            chaseChance /= total;
            defensiveChance /= total;

            currentState = Random.value < chaseChance ? State.Chasing : State.Defensive;
        }
    }

    protected virtual void HandlePatrolling()
    {
        rb.MovePosition(rb.position + patrolDirection * patrolSpeed * Time.deltaTime);
        patrolTimer -= Time.deltaTime;

        if (patrolTimer <= 0)
        {
            SetNewPatrolDirection();
            patrolTimer = patrolChangeInterval;
        }
    }

    protected virtual void HandleChasing()
    {
        if (target == null || Vector2.Distance(transform.position, target.position) > detectionRange)
        {
            currentState = State.Patrolling;
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.deltaTime);
    }

    protected virtual void HandleAttacking()
    {
        if (target == null || Vector2.Distance(transform.position, target.position) > attackRange)
        {
            currentState = State.Chasing;
            return;
        }

        Vector2 direction = (target.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * 1.5f * Time.deltaTime);
    }

    protected virtual void HandleDefensive()
    {
        if (target == null)
        {
            currentState = State.Patrolling;
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        Vector2 direction = (target.position - transform.position).normalized;

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
    }

    protected void SetNewPatrolDirection()
    {
        patrolDirection = directions[Random.Range(0, directions.Length)];
    }

    public virtual void TakeDamage(int damage, Collider2D collision)
    {
        health -= damage;
        Debug.Log($"{name} took {damage} damage. Health: {health}");
        if (health <= 0)
        {
            Debug.Log($"{name} died!");
            Destroy(gameObject);
        }
    }
}