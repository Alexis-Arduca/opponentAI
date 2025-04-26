using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWeapon : Enemy
{
    private void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.CompareTag("Enemy"))
    {
        Enemy enemyHit = collision.GetComponent<Enemy>();
        if (enemyHit != null && enemyHit != GetComponentInParent<Enemy>())
        {
            enemyHit.TakeDamage(damage);
        }
    }
}

}
