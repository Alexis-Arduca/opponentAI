using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemyHit = collision.GetComponent<Enemy>();
            Enemy parentEnemy = GetComponentInParent<Enemy>();
            if (enemyHit != null && parentEnemy != null && enemyHit != parentEnemy)
            {
                enemyHit.TakeDamage(parentEnemy.damage, collision);
                Debug.Log(parentEnemy.name + " hit " + enemyHit.name + " for " + parentEnemy.damage + " damage.");
            }
        }
    }
}