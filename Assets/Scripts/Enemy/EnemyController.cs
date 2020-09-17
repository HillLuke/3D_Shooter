using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("General")]
    public Animator Animator;
    public Health Health;

    [Header("General Movement")]
    [SerializeField] private float m_gravityMultiplier = 2f;
    [SerializeField] private float m_StickToGroundForce = 10f;

    private CharacterController m_CharacterController;
    private Vector3 m_MoveDir;
    private CollisionFlags m_collisionFlags;

    void Start()
    {
        m_CharacterController = GetComponent<CharacterController>();
        Animator = GetComponent<Animator>();
        Health = GetComponent<Health>();

        Health.onDie += OnDie;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        if (m_CharacterController.isGrounded)
        {
            m_MoveDir.y = -m_StickToGroundForce;
        }
        else
        {
            m_MoveDir += Physics.gravity * m_gravityMultiplier * Time.fixedDeltaTime;
        }
        m_collisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
    }

    void OnDie()
    {
        Animator.SetBool("Death", true);
        StartCoroutine(Death());
    }

    IEnumerator Death()
    {
        yield return new WaitForSeconds(2f);
        GameObject.Destroy(gameObject);
    }
}
