using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("General")]

    [Header("Player Detection")]
    public Transform DetectionCenter;
    public GameObject CurrentTarget;
    public float KnownTargetTimeout = 4f;
    public float DetectionRange = 20f;
    public float AttackRange = 10f;
    public bool TargetInDetectionRange;
    public bool TargetInAttackRange;
    public bool CanSeeTarget;
    public LayerMask DetectionLayer;

    [Header("Weapon")]
    public Transform WeaponParentSocket;
    public WeaponController Weapon;
    public LayerMask FPSWeaponLayer;

    [Header("General Movement")]
    [SerializeField] private float m_gravityMultiplier = 2f;
    [SerializeField] private float m_StickToGroundForce = 10f;

    private Health m_health;
    private Animator m_animator;
    private CharacterController m_CharacterController;
    private NavMeshAgent m_agent;
    private CollisionFlags m_collisionFlags;
    private bool m_setup;
    private bool needsNewPosition;
    private bool m_roaming;

    void Start()
    {
        Init();
    }

    void Update()
    {
        // Fall to ground when spawned, setup nav agent on landing
        FallToGroundOnSpawn();

        if (m_setup && m_health.IsAlive)
        {
            if (!CurrentTarget)
            {
                // Enemy has no target so will randomly roam around
                CheckIfPlayerInDetectionRadius();
                Roam();
            }
            else
            {
                m_agent.destination = CurrentTarget.transform.position;
                FaceTarget(m_agent.destination);
            }

            m_animator.SetBool("Walking", m_agent.velocity != Vector3.zero);

            //FaceTarget(m_agent.destination);

            //m_agent.destination = (Vector3)(PlayerController.Instance?.gameObject.transform.position);
            //m_collisionFlags = m_CharacterController.Move(m_agent.desiredVelocity.normalized * m_agent.speed * Time.deltaTime);
            //m_agent.velocity = m_CharacterController.velocity;
        }

        //m_CharacterController.Move(m_agent.desiredVelocity.normalized * m_agent.speed * Time.deltaTime);
        //m_agent.velocity = m_CharacterController.velocity;
    }

    private void CheckIfPlayerInDetectionRadius()
    {
        Collider[] hitColliders = Physics.OverlapSphere(DetectionCenter.position, DetectionRange, DetectionLayer);

        foreach (var hitCollider in hitColliders)
        {
            Debug.DrawRay(DetectionCenter.position, (hitCollider.gameObject.transform.position - gameObject.transform.position), Color.red);
            if (Physics.Raycast(DetectionCenter.position, (hitCollider.gameObject.transform.position - gameObject.transform.position), out RaycastHit lookathit, DetectionRange, -1, QueryTriggerInteraction.Ignore))
            {
                
                if (lookathit.collider.gameObject == hitCollider.gameObject)
                {
                    CurrentTarget = hitCollider.gameObject;
                    TargetInDetectionRange = true;
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
        m_animator = GetComponent<Animator>();
        m_health = GetComponent<Health>();
        m_agent = GetComponent<NavMeshAgent>();

        m_health.onDie += OnDie;

        m_agent.enabled = false;
        m_agent.updatePosition = false;
        m_agent.updateRotation = false;
        m_setup = false;
        m_roaming = false;

        WeaponController weaponInstance = Instantiate(Weapon, WeaponParentSocket);
        weaponInstance.owner = gameObject;
        weaponInstance.sourcePrefab = Weapon.gameObject;
        weaponInstance.gameObject.layer = Mathf.RoundToInt(Mathf.Log(FPSWeaponLayer.value, 2));
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
        m_animator.SetBool("Walking", false);
        m_animator.SetBool("Death", true);
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
