using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Base enemy AI with probabilistic decision-making for attack and defense.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public enum State { Patrolling, Chasing, Attacking, Defensive, Recovering, Stunned }
    public enum AttackType { None, QuickAttack, HeavyAttack, ChargeAttack }

    [Header("Stats")]
    [SerializeField] protected int maxHealth = 60;
    [SerializeField] protected int currentHealth;
    [SerializeField] public int damage = 5;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float detectionRange = 5f;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float minAttackRange = 0.5f;
    [SerializeField] protected float safeDistance = 3f;

    [Header("Personality")]
    [Range(0f, 1f)][SerializeField] protected float aggressionLevel = 0.5f;
    [Range(0f, 1f)][SerializeField] protected float courageLevel = 0.5f;
    [Range(0f, 1f)][SerializeField] protected float tacticalLevel = 0.5f;
    [Range(0f, 1f)][SerializeField] protected float coordinationLevel = 0.5f;

    [Header("Decision")]
    [SerializeField] protected float decisionInterval = 1.5f;
    [SerializeField] protected float reactionTime = 0.2f;
    protected float decisionTimer;

    [Header("Patrol")]
    [SerializeField] protected float patrolSpeed = 1.5f;
    [SerializeField] protected float patrolChangeInterval = 3f;
    protected float patrolTimer;
    protected Vector2 patrolDirection;

    [Header("Attack Settings")]
    [SerializeField] protected float quickAttackCooldown = 1f;
    [SerializeField] protected float heavyAttackCooldown = 3f;
    [SerializeField] protected float chargeAttackCooldown = 5f;
    [SerializeField] protected float quickAttackDamage = 5f;
    [SerializeField] protected float heavyAttackDamage = 10f;
    [SerializeField] protected float chargeAttackSpeed = 5f;
    protected float quickAttackTimer = 0f;
    protected float heavyAttackTimer = 0f;
    protected float chargeAttackTimer = 0f;

    [Header("Defensive Settings")]
    [SerializeField] protected float blockChance = 0.3f;
    [SerializeField] protected float dodgeCooldown = 2f;
    [SerializeField] protected float dodgeDistance = 2f;
    [SerializeField] protected float feintChance = 0.2f;
    [SerializeField] protected float stunDuration = 1f;
    protected float dodgeTimer = 0f;
    protected float stunTimer = 0f;

    [Header("Group Behavior")]
    [SerializeField] protected float allyDetectionRange = 3f;
    [SerializeField] protected float flankAngle = 45f;

    [Header("Recovery Settings")]
    [SerializeField] protected float recoveryTime = 0.5f;
    [SerializeField] protected float invulnerabilityTime = 0.5f;
    protected float recoveryTimer;
    protected float invulnerabilityTimer;

    [Header("UI")]
    [SerializeField] protected TextMeshProUGUI statsText;

    [Header("State Time Tracking")]
    protected float timeInPatrolling;
    protected float timeInChasing;
    protected float timeInAttacking;
    protected float timeInDefensive;
    protected float timeInRecovering;
    protected float timeInStunned;

    protected State currentState;
    protected Rigidbody2D rb;
    protected Transform target;
    private static readonly Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
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
        invulnerabilityTimer -= Time.deltaTime;
        switch (currentState)
        {
            case State.Patrolling: timeInPatrolling += Time.deltaTime; break;
            case State.Chasing: timeInChasing += Time.deltaTime; break;
            case State.Attacking: timeInAttacking += Time.deltaTime; break;
            case State.Defensive: timeInDefensive += Time.deltaTime; break;
            case State.Recovering: timeInRecovering += Time.deltaTime; break;
            case State.Stunned: timeInStunned += Time.deltaTime; break;
        }

        UpdateStatsDisplay();

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f && currentState != State.Stunned)
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
            case State.Recovering: HandleRecovering(); break;
            case State.Stunned: HandleStunned(); break;
        }
    }

    protected virtual void UpdateStatsDisplay()
    {
        if (statsText != null)
        {
            statsText.text = $"{name} Stats\n" +
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

    protected virtual void DecideState()
    {
        target = null;
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        float closestDistance = detectionRange;
        float targetHealthRatio = 1f;
        int nearbyAllies = 0;

        foreach (var enemy in enemies)
        {
            if (enemy != this && enemy.currentHealth > 0)
            {
                float distances = Vector2.Distance(transform.position, enemy.transform.position);
                if (distances < closestDistance)
                {
                    closestDistance = distances;
                    target = enemy.transform;
                    targetHealthRatio = (float)enemy.currentHealth / enemy.maxHealth;
                }
                if (distances < allyDetectionRange)
                {
                    nearbyAllies++;
                }
            }
        }

        if (target == null)
        {
            currentState = State.Patrolling;
            return;
        }

        float healthRatio = (float)currentHealth / maxHealth;
        float distance = Vector2.Distance(transform.position, target.position);
        float adjustedAggression = aggressionLevel * (1f - targetHealthRatio * 0.5f) * (1f + coordinationLevel * nearbyAllies * 0.2f);
        float retreatChance = (1f - courageLevel) * (1f - healthRatio);

        bool isPathBlocked = Physics2D.Raycast(transform.position, (target.position - transform.position).normalized, distance, LayerMask.GetMask("Obstacles"));

        if (healthRatio < 0.3f && Random.value < retreatChance)
        {
            currentState = State.Defensive;
        }
        else if (distance < attackRange && distance > minAttackRange && !isPathBlocked)
        {
            float attackChance = adjustedAggression * healthRatio * (1f - 0.5f * tacticalLevel);
            float defensiveChance = tacticalLevel + (1f - adjustedAggression) * (1f - healthRatio);

            float total = attackChance + defensiveChance;
            attackChance /= total;
            defensiveChance /= total;

            currentState = Random.value < attackChance ? State.Attacking : State.Defensive;
        }
        else
        {
            float chaseChance = adjustedAggression * healthRatio;
            float defensiveChance = tacticalLevel + (1f - adjustedAggression) * (1f - healthRatio);

            float total = chaseChance + defensiveChance;
            chaseChance /= total;
            defensiveChance /= total;

            currentState = Random.value < chaseChance && !isPathBlocked ? State.Chasing : State.Defensive;
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
        Vector2 moveDirection = direction;

        int nearbyAllies = 0;
        foreach (var ally in FindObjectsOfType<Enemy>())
        {
            if (ally != this && Vector2.Distance(transform.position, ally.transform.position) < allyDetectionRange)
            {
                nearbyAllies++;
            }
        }

        if (nearbyAllies > 0 && Random.value < coordinationLevel)
        {
            float flankSide = Random.value < 0.5f ? 1f : -1f;
            moveDirection = Quaternion.Euler(0, 0, flankAngle * flankSide) * direction;
        }

        rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.deltaTime);
    }

    protected virtual void HandleAttacking()
    {
        if (target == null || Vector2.Distance(transform.position, target.position) > attackRange || Vector2.Distance(transform.position, target.position) < minAttackRange)
        {
            currentState = State.Chasing;
            return;
        }

        quickAttackTimer -= Time.deltaTime;
        heavyAttackTimer -= Time.deltaTime;
        chargeAttackTimer -= Time.deltaTime;

        AttackType chosenAttack = ChooseAttack();
        switch (chosenAttack)
        {
            case AttackType.QuickAttack:
                StartCoroutine(PerformQuickAttack());
                break;
            case AttackType.HeavyAttack:
                StartCoroutine(PerformHeavyAttack());
                break;
            case AttackType.ChargeAttack:
                StartCoroutine(PerformChargeAttack());
                break;
            default:
                Vector2 direction = (target.position - transform.position).normalized;
                rb.MovePosition(rb.position + direction * moveSpeed * 1.2f * Time.deltaTime);
                break;
        }
    }

    protected virtual AttackType ChooseAttack()
    {
        float healthRatio = (float)currentHealth / maxHealth;
        float attackChance = aggressionLevel * healthRatio;
        float distance = Vector2.Distance(transform.position, target.position);

        float quickAttackWeight = 0.5f * attackChance * (1f + aggressionLevel);
        float heavyAttackWeight = 0.3f * attackChance * tacticalLevel;
        float chargeAttackWeight = 0.2f * attackChance * (distance > minAttackRange ? 1f : 0f);

        float totalWeight = quickAttackWeight + heavyAttackWeight + chargeAttackWeight;
        if (totalWeight == 0) return AttackType.None;

        quickAttackWeight /= totalWeight;
        heavyAttackWeight /= totalWeight;
        chargeAttackWeight /= totalWeight;

        float roll = Random.value;
        if (quickAttackTimer <= 0f && roll < quickAttackWeight)
            return AttackType.QuickAttack;
        if (heavyAttackTimer <= 0f && roll < quickAttackWeight + heavyAttackWeight)
            return AttackType.HeavyAttack;
        if (chargeAttackTimer <= 0f && roll < quickAttackWeight + heavyAttackWeight + chargeAttackWeight)
            return AttackType.ChargeAttack;
        return AttackType.None;
    }

    protected virtual IEnumerator PerformQuickAttack()
    {
        yield return new WaitForSeconds(reactionTime);
        if (target != null && Vector2.Distance(transform.position, target.position) <= attackRange)
        {
            quickAttackTimer = quickAttackCooldown;
            if (target.GetComponent<Enemy>() is Enemy enemy)
                enemy.TakeDamage((int)quickAttackDamage, null);
        }
    }

    protected virtual IEnumerator PerformHeavyAttack()
    {
        yield return new WaitForSeconds(reactionTime);
        if (target != null && Vector2.Distance(transform.position, target.position) <= attackRange)
        {
            heavyAttackTimer = heavyAttackCooldown;
            if (target.GetComponent<Enemy>() is Enemy enemy)
                enemy.TakeDamage((int)heavyAttackDamage, null);
            currentState = State.Recovering;
            recoveryTimer = recoveryTime;
        }
    }

    protected virtual IEnumerator PerformChargeAttack()
    {
        yield return new WaitForSeconds(reactionTime);
        if (target != null)
        {
            chargeAttackTimer = chargeAttackCooldown;
            Vector2 direction = (target.position - transform.position).normalized;
            rb.AddForce(direction * chargeAttackSpeed, ForceMode2D.Impulse);
            yield return new WaitForSeconds(0.3f);
            if (target != null && Vector2.Distance(transform.position, target.position) < attackRange)
            {
                if (target.GetComponent<Enemy>() is Enemy enemy)
                    enemy.TakeDamage((int)quickAttackDamage, this);
            }
            currentState = State.Recovering;
            recoveryTimer = recoveryTime;
        }
    }

    protected virtual void HandleDefensive()
    {
        if (target == null)
        {
            currentState = State.Patrolling;
            return;
        }

        dodgeTimer -= Time.deltaTime;
        float distance = Vector2.Distance(transform.position, target.position);
        Vector2 direction = (target.position - transform.position).normalized;

        if (distance < safeDistance * 0.5f && dodgeTimer <= 0f && Random.value < tacticalLevel)
        {
            PerformDodge();
            return;
        }

        if (Random.value < feintChance * tacticalLevel)
        {
            StartCoroutine(PerformFeint());
            return;
        }

        if (distance < safeDistance)
        {
            rb.MovePosition(rb.position - direction * moveSpeed * 0.8f * Time.deltaTime);
        }
        else if (distance > safeDistance + 1f)
        {
            Vector2 flankDirection = Quaternion.Euler(0, 0, Random.Range(-45f, 45f)) * direction;
            rb.MovePosition(rb.position + flankDirection * moveSpeed * 0.5f * Time.deltaTime);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    protected virtual void PerformDodge()
    {
        dodgeTimer = dodgeCooldown;
        Vector2 dodgeDir = Quaternion.Euler(0, 0, Random.Range(-90f, 90f)) * (target.position - transform.position).normalized;
        rb.MovePosition(rb.position + dodgeDir * dodgeDistance);
    }

    protected virtual IEnumerator PerformFeint()
    {
        Vector2 direction = (target.position - transform.position).normalized;
        rb.MovePosition(rb.position - direction * moveSpeed * 0.5f * Time.deltaTime);
        yield return new WaitForSeconds(0.5f);
        if (target != null && Vector2.Distance(transform.position, target.position) < attackRange && Random.value < aggressionLevel)
        {
            currentState = State.Attacking;
        }
    }

    protected virtual void HandleRecovering()
    {
        recoveryTimer -= Time.deltaTime;
        rb.velocity = Vector2.zero;
        if (recoveryTimer <= 0f)
        {
            DecideState();
        }
    }

    protected virtual void HandleStunned()
    {
        stunTimer -= Time.deltaTime;
        rb.velocity = Vector2.zero;
        if (stunTimer <= 0f)
        {
            currentState = State.Defensive;
            DecideState();
        }
    }

    protected void SetNewPatrolDirection()
    {
        patrolDirection = directions[Random.Range(0, directions.Length)];
    }

    public virtual void TakeDamage(int damage, Enemy attacker)
    {
        if (invulnerabilityTimer > 0f)
        {
            Debug.Log($"{name} is invulnerable!");
            return;
        }

        // Gestion du blocage
        if (Random.value < blockChance * tacticalLevel && currentState == State.Defensive)
        {
            Debug.Log($"{name} blocked the attack!");
            if (Random.value < tacticalLevel * 0.5f)
            {
                StartCoroutine(PerformQuickAttack());
            }
            return;
        }

        int adjustedDamage = currentState == State.Defensive ? damage / 2 : damage;
        currentHealth -= adjustedDamage;
        invulnerabilityTimer = invulnerabilityTime;

        Debug.Log($"{name} took {adjustedDamage} damage. Health: {currentHealth}/{maxHealth}");

        if (attacker != null && (attacker.currentState == State.Attacking && (attacker.heavyAttackTimer > 0f || attacker.chargeAttackTimer > 0f)))
        {
            if (Random.value < 0.3f * attacker.tacticalLevel)
            {
                currentState = State.Stunned;
                stunTimer = stunDuration;
                Debug.Log($"{name} is stunned!");
            }
        }

        if (currentHealth <= 0)
        {
            Debug.Log($"{name} died!");
            Destroy(gameObject);
        }
    }
}
