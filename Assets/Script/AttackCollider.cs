using UnityEngine;

public class AttackCollider : MonoBehaviour
{
    private BattleSystem battleSystem;
    private bool canDamage = false;

    void Start()
    {
        battleSystem = GetComponentInParent<BattleSystem>();
    }


    public void EnableDamage()
    {
        canDamage = true;
       
    }


    public void DisableDamage()
    {
        canDamage = false;
       
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canDamage)
            return;

        EnemyController enemy = other.GetComponent<EnemyController>();

        if (enemy != null && enemy.IsAttackable())
        {
            
            battleSystem.ManualHit(enemy);
            DisableDamage(); // Prevent multiple hits from same attack
        }
    }
}
