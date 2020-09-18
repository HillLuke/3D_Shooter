using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("General")]
    public Animator Animator;
    public Health Health;

    [Header("General Movement")]
    [SerializeField] private float m_gravityMultiplier = 2f;
    [SerializeField] private float m_StickToGroundForce = 10f;

    private CharacterController m_CharacterController;
    private CollisionFlags m_collisionFlags;
    private NavMeshAgent m_agent;
    private bool m_setup;

    void Start()
    {
        m_CharacterController = GetComponent<CharacterController>();
        Animator = GetComponent<Animator>();
        Health = GetComponent<Health>();
        m_agent = GetComponent<NavMeshAgent>();

        Health.onDie += OnDie;

        m_agent.enabled = false;
        m_agent.updatePosition = false;
        m_agent.updateRotation = false;
        m_setup = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_CharacterController.isGrounded && !m_setup)
        {
            m_setup = true;
            m_agent.enabled = true;
        }

        if (!m_setup)
        {
            m_collisionFlags =  m_CharacterController.Move(Physics.gravity * m_gravityMultiplier * Time.fixedDeltaTime);
            m_agent.enabled = false;
        }

        if (m_setup)
        {
            m_agent.updateRotation = true;
            m_agent.updatePosition = true;
            m_agent.destination = (Vector3)(PlayerController.Instance?.gameObject.transform.position);
            FaceTarget(m_agent.destination);
            //m_collisionFlags = m_CharacterController.Move(m_agent.desiredVelocity.normalized * m_agent.speed * Time.deltaTime);
            //m_agent.velocity = m_CharacterController.velocity;
        }



        //m_CharacterController.Move(m_agent.desiredVelocity.normalized * m_agent.speed * Time.deltaTime);
        //m_agent.velocity = m_CharacterController.velocity;
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
        m_CharacterController.detectCollisions = false;
        m_CharacterController.enabled = false;
        m_agent.isStopped = true;
        Animator.SetBool("Death", true);
        StartCoroutine(Death());
    }

    IEnumerator Death()
    {
        yield return new WaitForSeconds(2f);
        GameObject.Destroy(gameObject);
    }
}
