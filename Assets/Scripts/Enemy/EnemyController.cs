using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("General")]
    public Character Character;

    [Header("Player Detection")]
    public Transform DetectionCenter;
    public GameObject CurrentTarget;
    public float KnownTargetTimeout = 2f;
    public float DetectionRange = 20f;
    public float AttackRange = 10f;
    public float KeepDistance = 15f;
    public bool IsTargetInDetectionRange;
    public bool IsTargetInAttackRange;
    public bool CanSeeTarget;
    public LayerMask DetectionLayer;

    [Header("Weapon")]
    public Transform WeaponParentSocket;
    public WeaponController Weapon;
    public WeaponController WeaponInstance;
    public LayerMask FPSWeaponLayer;

    [Header("General Movement")]
    [SerializeField] private float m_gravityMultiplier = 2f;
    [SerializeField] private float m_StickToGroundForce = 10f;

    private CharacterController m_CharacterController;
    private NavMeshAgent m_agent;
    private CollisionFlags m_collisionFlags;
    private bool m_setup;
    private bool needsNewPosition;
    private bool m_roaming;
    private float m_targetLostTime;

    void Start()
    {
        Character = GetComponent<Character>();

        Init();
    }

    void Update()
    {
        // Fall to ground when spawned, setup nav agent on landing
        FallToGroundOnSpawn();

        if (m_setup && Character.Health.IsAlive)
        {
            if (!CurrentTarget)
            {
                // Enemy has no target so will randomly roam around
                CheckIfATargetIsInDetectionRange();
                Roam();
            }
            else
            {
                IsTargetInDetectionRange = IsTargetWithinRange(CurrentTarget.gameObject.transform.position, DetectionRange, DetectionLayer, out RaycastHit DetectHit);
                IsTargetInAttackRange = IsTargetWithinRange(CurrentTarget.gameObject.transform.position, AttackRange, DetectionLayer, out RaycastHit AttackHit);
                IsTargetWithinRange(CurrentTarget.gameObject.transform.position, AttackRange, -1, out RaycastHit CanSeeHit);

                CanSeeTarget = CanSeeHit.collider?.gameObject == CurrentTarget;

                Debug.Log($"IsTargetInDetectionRange {IsTargetInDetectionRange} --- IsTargetInAttackRange {IsTargetInAttackRange} --- CanSeeTarget {CanSeeTarget}");

                m_agent.destination = CurrentTarget.transform.position;
                FaceTarget(m_agent.destination);

                // Return to roaming if target is lost for more than KnownTargetTimeout
                if (!CanSeeTarget)
                {
                    if (m_targetLostTime == 0)
                    {
                        m_targetLostTime = Time.deltaTime;
                    }

                    if (m_targetLostTime >= KnownTargetTimeout)
                    {
                        m_targetLostTime = 0;
                        CurrentTarget = null;
                        IsTargetInDetectionRange = false;
                        IsTargetInAttackRange = false;
                        CanSeeTarget = false;
                        m_agent.isStopped = true;
                        m_agent.ResetPath();
                    }
                    else
                    {
                        m_targetLostTime += Time.deltaTime;
                    }
                }
                else
                {
                    m_targetLostTime = 0;
                }

                if (IsTargetInAttackRange && CanSeeTarget)
                {
                    ShootTarget();
                }
            }

            Character.Animator.SetBool("Walking", m_agent.velocity != Vector3.zero);

            //FaceTarget(m_agent.destination);

            //m_agent.destination = (Vector3)(PlayerController.Instance?.gameObject.transform.position);
            //m_collisionFlags = m_CharacterController.Move(m_agent.desiredVelocity.normalized * m_agent.speed * Time.deltaTime);
            //m_agent.velocity = m_CharacterController.velocity;
        }

        //m_CharacterController.Move(m_agent.desiredVelocity.normalized * m_agent.speed * Time.deltaTime);
        //m_agent.velocity = m_CharacterController.velocity;
    }

    private bool IsTargetWithinRange(Vector3 target, float range,LayerMask layerMask, out RaycastHit lookathit)
    {
        if (Physics.Raycast(DetectionCenter.position, (target - gameObject.transform.position), out RaycastHit hit, range, layerMask, QueryTriggerInteraction.Ignore))
        {
            lookathit = hit;
            return true;
        }
        else
        {
            lookathit = hit;
            return false;
        }
    }

    private void CheckIfATargetIsInDetectionRange()
    {
        Collider[] hitColliders = Physics.OverlapSphere(DetectionCenter.position, DetectionRange, DetectionLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (IsTargetWithinRange(hitCollider.gameObject.transform.position, DetectionRange, -1, out RaycastHit lookathit))
            {                
                if (lookathit.collider.gameObject == hitCollider.gameObject)
                {
                    CurrentTarget = hitCollider.gameObject;
                    IsTargetInDetectionRange = true;
                    CanSeeTarget = true;
                }
            }

            break; // for now just use first collider
        }
    }

    private void Roam() 
    {
        if (m_roaming)
        {
            if (!m_agent.pathPending)
            {
                if (m_agent.remainingDistance <= m_agent.stoppingDistance)
                {
                    if (!m_agent.hasPath || m_agent.velocity.sqrMagnitude == 0f)
                    {
                        needsNewPosition = true;
                    }
                }
            }

            if (needsNewPosition)
            {
                m_agent.destination = NewTarget(gameObject.transform.position);
                needsNewPosition = false;
            }
        }
    }

    private void FallToGroundOnSpawn()
    {
        if (!m_setup)
        {
            if (m_CharacterController.isGrounded)
            {
                m_setup = true;
                m_agent.enabled = true;
                needsNewPosition = true;
                m_roaming = true;
                m_agent.updateRotation = true;
                m_agent.updatePosition = true;
            }
            else
            {
                m_collisionFlags = m_CharacterController.Move(Physics.gravity * m_gravityMultiplier * Time.fixedDeltaTime);
            }
        }
    }

    private void Init()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_agent = GetComponent<NavMeshAgent>();

        Character.Health.onDie += OnDie;

        m_agent.stoppingDistance = KeepDistance;
        m_agent.enabled = false;
        m_agent.updatePosition = false;
        m_agent.updateRotation = false;
        m_setup = false;
        m_roaming = false;

        WeaponInstance = Instantiate(Weapon, WeaponParentSocket);
        WeaponInstance.Owner = gameObject;
        WeaponInstance.sourcePrefab = Weapon.gameObject;
        WeaponInstance.gameObject.layer = Mathf.RoundToInt(Mathf.Log(FPSWeaponLayer.value, 2));
    }

    private Vector3 NewTarget(Vector3 currentPosition)
    {
        float roamRadius = 20f;
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection += currentPosition;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, roamRadius, 1);
        return hit.position;
    }

    private void FaceTarget(Vector3 destination)
    {
        Vector3 lookPos = destination - transform.position;
        lookPos.y = 0;
        Quaternion rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 20f);
    }

    private void ShootTarget()
    {
        if (CurrentTarget)
        {
            if (Physics.Raycast(DetectionCenter.position, (CurrentTarget.gameObject.transform.position - gameObject.transform.position), out RaycastHit lookathit, AttackRange, DetectionLayer, QueryTriggerInteraction.Ignore))
            {
                Character.Target = lookathit.point;
                WeaponInstance.HandleShootInputs(true, true);
            }
        }

    }

    void FixedUpdate()
    {
        //if (m_CharacterController.isGrounded)
        //{
        //    m_MoveDir.y = -m_StickToGroundForce;
        //}
        //else
        //{
        //    m_agent.enabled = false;
        //    m_MoveDir += Physics.gravity * m_gravityMultiplier * Time.fixedDeltaTime;
        //}
        //m_collisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
    }

    void MoveToPlayer()
    {
        if (m_CharacterController.isGrounded)
        {
            m_agent.destination = (Vector3)(PlayerController.Instance?.gameObject.transform.position);
        }
    }

    void OnDie()
    {
        Character.Animator.SetBool("Walking", false);
        Character.Animator.SetBool("Death", true);
        m_CharacterController.detectCollisions = false;
        m_CharacterController.enabled = false;
        m_agent.isStopped = true;
        StartCoroutine(Death());
    }

    IEnumerator Death()
    {
        yield return new WaitForSeconds(2f);
        GameObject.Destroy(gameObject);
    }
}
