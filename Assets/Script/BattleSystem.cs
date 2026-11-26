using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using System.Collections;

public class BattleSystem : MonoBehaviour
{
    private EnemyAI enemyAI;
    private EnemyDetector enemyDetector;
    private InputScript inputScript;
    private Animator animator;
    private CinemachineImpulseSource impulseSource;

    [Header("Target")]
    
    private EnemyController _lockedTarget;
    public EnemyController lockedTarget
    {
        get { return _lockedTarget; }
        set
        {
            if (_lockedTarget != value)
            {
                Debug.Log($"LockedTarget changed from {(_lockedTarget?.name ?? "NULL")} to {(value?.name ?? "NULL")}");
              
            }
            _lockedTarget = value;
        }
    }

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown;

    [Header("States")]
    public bool isAttackingEnemy = false;
    public bool isCountering = false;

    [Header("Public References")]
    [SerializeField] private Transform punchPosition;
    [SerializeField] private VFX punchParticle;
    [SerializeField] private GameObject lastHitCamera;
    [SerializeField] private Transform lastHitFocusObject;

    //Coroutines
    private Coroutine counterCoroutine;
    private Coroutine attackCoroutine;
    private Coroutine damageCoroutine;

    [Space]

    //Events
    public UnityEvent<EnemyController> OnTrajectory;
    public UnityEvent<EnemyController> OnHit;
    public UnityEvent<EnemyController> OnCounterAttack;

    int animationCount = 0;
    string[] attacks;

    void Start()
    {
        enemyAI = FindFirstObjectByType<EnemyAI>();
        animator = GetComponent<Animator>();
        enemyDetector = GetComponentInChildren<EnemyDetector>();
        inputScript = GetComponent<InputScript>();
        impulseSource = GetComponentInChildren<CinemachineImpulseSource>();

        if (enemyAI != null)
        {
            Debug.Log($"EnemyAI found. Alive enemies: {enemyAI.AliveEnemyCount()}");
        }
        else
        {
            Debug.LogError("EnemyAI not found in scene!");
        }
    }

    //This function gets called whenever the player inputs the punch action
    void AttackCheck()
    {
        if (isAttackingEnemy)
        {
            
            return;
        }

       
        // Get target from detector
        EnemyController detectorTarget = enemyDetector.CurrentTarget();
        

        // Simple assignment
        lockedTarget = detectorTarget;

        

        // If no target from detector, get random
        if (lockedTarget == null)
        {
            
            lockedTarget = enemyAI.RandomEnemy();
           
        }

        // Final check
        if (lockedTarget == null)
        {
            return;
        }

       
        float distance = TargetDistance(lockedTarget);
        Attack(lockedTarget, distance);
        
    }

    public void Attack(EnemyController target, float distance)
    {

        //Types of attack animation
        attacks = new string[] { "Kick", "Kick2", "Punch" };

        //Attack nothing in case target is null
        if (target == null)
        {

            AttackType("Punch", .2f, null, 0);
            return;
        }

        if (distance < 15)
        {
            animationCount = (int)Mathf.Repeat((float)animationCount + 1, (float)attacks.Length);
            string attackString = isLastHit() ? attacks[Random.Range(0, attacks.Length)] : attacks[animationCount];
            AttackType(attackString, attackCooldown, target, .65f);
        }
        else
        {
            lockedTarget = null;
            AttackType("Punch", .2f, null, 0);
        }

        
        JumpToTrget(target, distance);

    }

    private void JumpToTrget(EnemyController target, float distance)
    {
        Debug.Log(impulseSource);
        impulseSource.ImpulseDefinition.AmplitudeGain = Mathf.Max(3, 1 * distance);
    }

    void AttackType(string attackTrigger, float cooldown, EnemyController target, float movementDuration)
    {
        
        animator.SetTrigger(attackTrigger);

        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackCoroutine(isLastHit() ? 1.5f : cooldown));


        if (isLastHit())
            StartCoroutine(FinalBlowCoroutine());

        if (target == null)
            return;

        target.StopMoving();
        MoveTorwardsTarget(target, movementDuration);

        
        StartCoroutine(BackupDamageSystem(target, movementDuration)); //attackCheck isnt working properly so this is a backup system to ensure damage is applied

        IEnumerator BackupDamageSystem(EnemyController enemy, float delay)
        {
            // Wait for movement + a bit for the hit to land
            yield return new WaitForSeconds(delay + 0.2f);

            // Check if HitEvent was already called (enemy already damaged this frame)
            if (enemy != null && enemy.IsAttackable() && lockedTarget == enemy)
            {
                
                OnHit.Invoke(enemy);
                punchParticle.PlayParticleAtPosition(punchPosition.position);
            }
        }

        IEnumerator AttackCoroutine(float duration)
        {
            
            inputScript.acceleration = 0;
            isAttackingEnemy = true;
            inputScript.enabled = false;
            yield return new WaitForSeconds(duration);

            
            isAttackingEnemy = false;



            yield return new WaitForSeconds(.2f);

            
            inputScript.enabled = true;
            LerpCharacterAcceleration();
        }

        IEnumerator FinalBlowCoroutine()
        {
            Time.timeScale = .5f;
            lastHitCamera.SetActive(true);
            lastHitFocusObject.position = lockedTarget.transform.position;
            yield return new WaitForSecondsRealtime(2);
            lastHitCamera.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    void MoveTorwardsTarget(EnemyController target, float duration)
    {
        OnTrajectory.Invoke(target);
        transform.DOLookAt(target.transform.position, 2f);
        transform.DOMove(TargetOffset(target.transform), duration);
    }

    void CounterCheck()
    {
        //Initial check
        if (isCountering || isAttackingEnemy || !enemyAI.AnEnemyIsPreparingAttack())
            return;

        lockedTarget = ClosestCounterEnemy();
        OnCounterAttack.Invoke(lockedTarget);

        if (TargetDistance(lockedTarget) > 2)
        {
            Attack(lockedTarget, TargetDistance(lockedTarget));
            return;
        }

        float duration = .2f;
        animator.SetTrigger("Dodge");
        transform.DOLookAt(lockedTarget.transform.position, .2f);
        transform.DOMove(transform.position + lockedTarget.transform.forward, duration);

        if (counterCoroutine != null)
            StopCoroutine(counterCoroutine);
        counterCoroutine = StartCoroutine(CounterCoroutine(duration));

        IEnumerator CounterCoroutine(float duration)
        {
            isCountering = true;
            inputScript.enabled = false;
            yield return new WaitForSeconds(duration);
            Attack(lockedTarget, TargetDistance(lockedTarget));
            isCountering = false;

        }
    }

    float TargetDistance(EnemyController target)
    {
        if (target == null)
        {
          
            return 999f;
        }
            
        return Vector3.Distance(transform.position, target.transform.position);
        Debug.Log("Distance to target: " + Vector3.Distance(transform.position, target.transform.position));
    }

    public Vector3 TargetOffset(Transform target)
    {
        Vector3 position;
        position = target.position;
        return Vector3.MoveTowards(position, transform.position, .95f);
    }

    public void HitEvent()
    {
        if (lockedTarget == null || enemyAI.AliveEnemyCount() == 0)
            return;

        OnHit.Invoke(lockedTarget);


        punchParticle.PlayParticleAtPosition(punchPosition.position);
    }

    // Manual hit for collision-based damage
    public void ManualHit(EnemyController enemy)
    {
        if (enemy == null || !enemy.IsAttackable())
            return;

        
        OnHit.Invoke(enemy);
        punchParticle.PlayParticleAtPosition(punchPosition.position);
    }

    public void DamageEvent()
    {
        animator.SetTrigger("Hit");

        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);
        damageCoroutine = StartCoroutine(DamageCoroutine());

        IEnumerator DamageCoroutine()
        {
            inputScript.enabled = false;
            yield return new WaitForSeconds(.5f);
            inputScript.enabled = true;
            LerpCharacterAcceleration();
        }
    }

    EnemyController ClosestCounterEnemy()
    {
        float minDistance = 100;
        int finalIndex = 0;

        for (int i = 0; i < enemyAI.allEnemies.Length; i++)
        {
            EnemyController enemy = enemyAI.allEnemies[i].enemyScript;

            if (enemy.IsPreparingAttack())
            {
                if (Vector3.Distance(transform.position, enemy.transform.position) < minDistance)
                {
                    minDistance = Vector3.Distance(transform.position, enemy.transform.position);
                    finalIndex = i;
                }
            }
        }

        return enemyAI.allEnemies[finalIndex].enemyScript;

    }

    void LerpCharacterAcceleration()
    {
        inputScript.acceleration = 0;
        DOVirtual.Float(1f, 0, .6f, ((acceleration) => inputScript.acceleration = acceleration));
    }

    bool isLastHit()
    {
        if (lockedTarget == null)
            return false;

        return enemyAI.AliveEnemyCount() == 1 && lockedTarget.health <= 1;
    }

    #region Input

    private void OnCounter()
    {
        CounterCheck();
    }

    private void OnAttack()
    {
       
        Debug.Log("Attack button pressed!"); //why wont my attacks work :(
        AttackCheck();
    }

    #endregion

}
