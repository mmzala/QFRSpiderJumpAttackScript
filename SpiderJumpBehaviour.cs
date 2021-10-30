using UnityEngine;

// Summary:
// This script uses an Quadratic Bézier Curve equation to create a jump attack
// , while this script runs, the spider will jump towards the target (player).
// "if (IsClientOnly(animator.gameObject)) return;" line in each OnState function is a hacky way
// to tell mirror not to run this on the client, but only on the server computer.
// Preview video: https://youtu.be/B_mgKHBfTHE

public class SpiderJumpBehaviour : EnemyBehaviourBase
{
    [SerializeField] private float _jumpSpeed = 1f;
    [SerializeField] private float _arcEffector = 10f;

    [Header("Knockback Settings")]
    [SerializeField] private float _knockbackForce = 1f;
    [SerializeField] private float _knockbackHeight = 5f;
    [SerializeField] private float _knockbackRadius = 5f;
    [SerializeField] private LayerMask _playersLayerMask;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private Vector3 _effectorPosition;

    private float timer;
    protected int _isAttackingID;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (IsClientOnly(animator.gameObject)) return;

        // _targetPosition is set in the chase behaviour
        _startPosition = animator.transform.position;

        // Claculate effector position in the middle of the start and end positions
        Vector3 midwayPoint = (_startPosition + _targetPosition) / 2;
        _effectorPosition = midwayPoint + Vector3.up * _arcEffector;

        timer = 0f;

        // Cache id for optimized setting
        _isAttackingID = Animator.StringToHash("isAttacking");
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (IsClientOnly(animator.gameObject)) return;

        HandleKnockback(animator.transform.position);

        // If spider's jump attack is done, set is attacking on false and go back to chasing state
        timer = timer + _jumpSpeed * Time.deltaTime;
        if (timer > 1f)
        {
            animator.SetBool(_isAttackingID, false);
            return;
        }

        // Calculate new position and rotation
        Vector3 newPos = CalculatePoint(_startPosition, _targetPosition, timer);
        animator.transform.position = newPos;
        animator.transform.LookAt(_targetPosition);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (IsClientOnly(animator.gameObject)) return;

        // Reset the rotation
        Vector3 newRot = new Vector3(0.0f, animator.transform.rotation.eulerAngles.y, 0.0f);
        animator.transform.rotation = Quaternion.Euler(newRot);

        animator.SetBool("jumpAttack", false);
    }

    private void HandleKnockback(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, _knockbackRadius, _playersLayerMask);

        foreach(Collider collider in colliders)
        {
            if (!collider.CompareTag("Player")) continue;

            // Calculate the direction vector away from the spider and push the player in that direction
            Vector3 dir = (collider.transform.position - position).normalized;
            dir.y = 0f;
            dir += Vector3.up * _knockbackHeight;

            collider.transform.position += dir * _knockbackForce * Time.deltaTime;
        }
    }

    /// <summary>
    /// Calculates point on arc based on the given t variable
    /// </summary>
    /// <param name="p0"> Start position </param>
    /// <param name="p2"> End position </param>
    /// <param name="t"></param>
    /// <returns> Point on arc </returns>
    public Vector3 CalculatePoint(Vector3 p0, Vector3 p2, float t)
    {
        // Equation used to calculate the curve:
        // B(t) = (1-t)2P0 + 2(1-t)tP1 + t2P2

        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0;
        p += 2 * u * t * _effectorPosition;
        p += tt * p2;

        return p;
    }

    public void SetTargetPosition(Vector3 position)
    {
        _targetPosition = position;
    }
}
