using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Camera MainCamera;
    public AudioListener AudioListener;
    public Transform RotatePoint;

    [SerializeField] private bool m_isWalking;
    [SerializeField] private float m_walkSpeed;
    [SerializeField] private float m_runSpeed;
    [SerializeField] private float m_jumpSpeed;
    [SerializeField] private float m_gravityMultiplier;
    [SerializeField] private float m_StickToGroundForce;
    [SerializeField] private float m_StepInterval;
    [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
    [SerializeField] private bool m_useheadBob;
    [SerializeField] private bool m_useFovKick;
    [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
    [SerializeField] private FOVKick m_FovKick = new FOVKick();
    [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();

    private CharacterController m_CharacterController;
    private PlayerLookController m_playerLook;
    private AudioSource m_AudioSource;
    private CollisionFlags m_collisionFlags;
    private Vector3 m_OriginalCameraPosition;
    private Vector3 m_MoveDir;
    private float m_StepCycle;
    private float m_NextStep;
    private bool m_Jump;
    private bool m_PreviouslyGrounded;
    private bool m_Jumping;
    private Vector2 m_Input;

    // Start is called before the first frame update
    private void Start()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_playerLook = GetComponent<PlayerLookController>();
        m_AudioSource = GetComponent<AudioSource>();

        m_HeadBob.Setup(MainCamera, m_StepInterval);
        m_playerLook.Init(transform, MainCamera.transform);
        m_FovKick.Setup(MainCamera);
        m_OriginalCameraPosition = MainCamera.transform.localPosition;
        m_StepCycle = 0f;
        m_NextStep = m_StepCycle / 2f;
        m_Jumping = false;
    }

    // Update is called once per frame
    private void Update()
    {
        //Rotate to face mouse
        m_playerLook.LookRotation(transform, MainCamera.transform);

        // the jump state needs to read here to make sure it is not missed
        if (!m_Jump)
        {
            m_Jump = Input.GetButtonDown("Jump");
        }

        if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
        {
            StartCoroutine(m_JumpBob.DoBobCycle());
            m_MoveDir.y = 0f;
            m_Jumping = false;
        }
        if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
        {
            m_MoveDir.y = 0f;
        }
        m_PreviouslyGrounded = m_CharacterController.isGrounded;

    }

    private void FixedUpdate()
    {
        float speed;
        GetInput(out speed);
        // always move along the camera forward as it is the direction that it being aimed at
        Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

        // get a normal for the surface that is being touched to move along it
        RaycastHit hitInfo;
        Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                           m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

        m_MoveDir.x = desiredMove.x * speed;
        m_MoveDir.z = desiredMove.z * speed;

        if (m_CharacterController.isGrounded)
        {
            m_MoveDir.y = -m_StickToGroundForce;

            if (m_Jump)
            {
                m_MoveDir.y = m_jumpSpeed;
                m_Jump = false;
                m_Jumping = true;
            }
        }
        else
        {
            m_MoveDir += Physics.gravity * m_gravityMultiplier * Time.fixedDeltaTime;
        }

        m_collisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

        ProgressStepCycle(speed);
        UpdateCameraPosition(speed);

        m_playerLook.UpdateCursorLock();
    }

    private void GetInput(out float speed)
    {
        // Read input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        bool waswalking = m_isWalking;
        m_isWalking = !Input.GetKey(KeyCode.LeftShift);
        // set the desired speed to be walking or running
        speed = m_isWalking ? m_walkSpeed : m_runSpeed;
        m_Input = new Vector2(horizontal, vertical);

        // normalize input if it exceeds 1 in combined length:
        if (m_Input.sqrMagnitude > 1)
        {
            m_Input.Normalize();
        }

        // handle speed change to give an fov kick
        // only if the player is going to a run, is running and the fovkick is to be used
        if (m_isWalking != waswalking && m_useFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
        {
            StopAllCoroutines();
            StartCoroutine(!m_isWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
        }
    }

    private void ProgressStepCycle(float speed)
    {
        if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
        {
            m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_isWalking ? 1f : m_RunstepLenghten))) *
                         Time.fixedDeltaTime;
        }

        if (!(m_StepCycle > m_NextStep))
        {
            return;
        }

        m_NextStep = m_StepCycle + m_StepInterval;
    }

    private void UpdateCameraPosition(float speed)
    {
        Vector3 newCameraPosition;
        if (!m_useheadBob)
        {
            return;
        }
        if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
        {
            MainCamera.transform.localPosition =
                m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                  (speed * (m_isWalking ? 1f : m_RunstepLenghten)));
            newCameraPosition = MainCamera.transform.localPosition;
            newCameraPosition.y = MainCamera.transform.localPosition.y - m_JumpBob.Offset();
        }
        else
        {
            newCameraPosition = MainCamera.transform.localPosition;
            newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
        }
        MainCamera.transform.localPosition = newCameraPosition;
    }
}
