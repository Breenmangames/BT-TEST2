using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyDetector : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private EnemyAI enemyAI;
    private InputScript inputScript;
    private BattleSystem battleSystem;

    public LayerMask layerMask;

    [SerializeField] Vector3 inputDirection;
    [SerializeField] private EnemyController currentTarget;

    public GameObject cam;

    private void Start()
    {
        inputScript = GetComponentInParent<InputScript>();
        battleSystem = GetComponentInParent<BattleSystem>();
    }

    private void Update()
    {
        var camera = Camera.main;
        var forward = camera.transform.forward;
        var right = camera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        inputDirection = forward * inputScript.moveAxis.y + right * inputScript.moveAxis.x;
        inputDirection = inputDirection.normalized;

        RaycastHit info;

        if (Physics.SphereCast(transform.position, 3f, inputDirection, out info, 10, layerMask))
        {
            if (info.collider.transform.GetComponent<EnemyController>().IsAttackable())
                currentTarget = info.collider.transform.GetComponent<EnemyController>();
        }
    }

    public EnemyController CurrentTarget()
    {
        return currentTarget;
    }

    public void SetCurrentTarget(EnemyController target)
    {
        currentTarget = target;
    }

    public float InputMagnitude()
    {
        return inputDirection.magnitude;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawRay(transform.position, inputDirection);
        Gizmos.DrawWireSphere(transform.position, 1);
        if (CurrentTarget() != null)
            Gizmos.DrawSphere(CurrentTarget().transform.position, .5f);
    }
}
