using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerController : MonoBehaviour
{
    //TODO Find a better way to keep a reference to local player
    public static PlayerController Instance { get; protected set; }

    [Header("General")]
    public Camera MainCamera;
    public AudioListener AudioListener;
    public Transform RotatePoint;
    public Character Character;
    public PlayerWeaponsManager PlayerWeaponsManager;

    [Header("Walking")]
    [SerializeField] private float m_walkSpeed;
    [SerializeField] private float m_StepInterval;
    [Header("Running")]
    [SerializeField] private float m_runSpeed;
    [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
    public float CurrentStamina;
    public float MaxStamina = 100f;
    [Header("Jumping")]
    [SerializeField] private float m_jumpSpeed;
    [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
    [Header("General Movement")]
    [SerializeField] private float m_gravityMultiplier;
    [SerializeField] private float m_StickToGroundForce;
    [SerializeField] private bool m_useheadBob;
    [SerializeField] private bool m_useFovKick;
    [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
    [SerializeField] private FOVKick m_FovKick = new FOVKick();
    [SerializeField] private float m_killHeight = -50f;
    [Header("Aiming")]
    [SerializeField] private float m_normalFOV = 70;
    [SerializeField] private float m_aimingFOV = 20;

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
    private bool m_isWalking;
    private Vector2 m_Input;
    private bool m_isIdle;

    private float StaminaRegenTimer = 0.0f;
    private const float StaminaDecreasePerFrame = 10.0f;
    private const float StaminaIncreasePerFrame = 20.0f;
    private const float StaminaTimeToRegen = 3.0f;

    // Start is called before the first frame update
    private void Start()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_playerLook = GetComponent<PlayerLookController>();
        m_AudioSource = GetComponent<AudioSource>();
        Character = GetComponent<Character>();
        PlayerWeaponsManager = GetComponent<PlayerWeaponsManager>();

        m_HeadBob.Setup(MainCamera, m_StepInterval);
        m_playerLook.Init(transform, RotatePoint);
        m_FovKick.Setup(MainCamera);
        m_OriginalCameraPosition = MainCamera.transform.localPosition;
        m_StepCycle = 0f;
        m_NextStep = m_StepCycle / 2f;
        m_Jumping = false;
        CurrentStamina = MaxStamina;

        Character.Health.onDie += OnDie;

        Instance = this;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Character.Health.IsAlive && transform.position.y < m_killHeight)
        {
            Character.Health.Kill();
        }

        //Rotate to face mouse
        m_playerLook.LookRotation(transform, RotatePoint);

        // the jump state needs to read here to make sure it is not missed
        if (!m_Jump)
        {
            m_Jump = Input.GetButtonDown("Jump");
        }

        if (Input.GetMouseButton(1))
        {
            MainCamera.fieldOfView = m_aimingFOV;
        }
        else
        {
            MainCamera.fieldOfView = m_normalFOV;
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

        if (!m_isWalking && !m_isIdle)
        {
            CurrentStamina = Mathf.Clamp(CurrentStamina - (StaminaDecreasePerFrame * Time.deltaTime), 0.0f, MaxStamina);
            StaminaRegenTimer = 0.0f;
        }
        else if (CurrentStamina < MaxStamina)
        {
            if (StaminaRegenTimer >= StaminaTimeToRegen)
                CurrentStamina = Mathf.Clamp(CurrentStamina + (StaminaIncreasePerFrame * Time.deltaTime), 0.0f, MaxStamina);
            else
                StaminaRegenTimer += Time.deltaTime;
        }

        if (m_isIdle & m_CharacterController.isGrounded)
        {
            Character.Animator.SetBool("Running", false);
            Character.Animator.SetBool("Walking", false);
            Character.Animator.SetBool("Jumping", false);
        }
        else
        {
            if (m_Jumping)
            {
                Character.Animator.SetBool("Jumping", true);
                Character.Animator.SetBool("Running", false);
                Character.Animator.SetBool("Walking", false);
            }
            else
            {
                Character.Animator.SetBool("Jumping", false);
                Character.Animator.SetBool("Running", !m_isWalking);
                Character.Animator.SetBool("Walking", m_isWalking);
            }           
        }
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

        Character.Animator.SetFloat("Dirx", horizontal);
        Character.Animator.SetFloat("Diry", vertical);

        m_isIdle = horizontal == 0 && vertical == 0;

        bool waswalking = m_isWalking;
        m_isWalking = !Input.GetKey(KeyCode.LeftShift);

        if (!m_isWalking && CurrentStamina == 0)
        {
            m_isWalking = true;
        }

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
        else
        {
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

    void OnDie()
    {
        //TODO: Handle death
    }

}
