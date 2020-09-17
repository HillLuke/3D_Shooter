using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public enum WeaponShootType
{
    Manual,
    Automatic,
}

[System.Serializable]
public struct CrosshairData
{
    [Tooltip("The image that will be used for this weapon's crosshair")]
    public Sprite crosshairSprite;
    [Tooltip("The size of the crosshair image")]
    public int crosshairSize;
    [Tooltip("The color of the crosshair image")]
    public Color crosshairColor;
}

public class WeaponController : MonoBehaviour
{
    [Header("Information")]
    [Tooltip("The name that will be displayed in the UI for this weapon")]
    public string weaponName;
    [Tooltip("The image that will be displayed in the UI for this weapon")]
    public Sprite weaponIcon;

    [Tooltip("Default data for the crosshair")]
    public CrosshairData crosshairDataDefault;

    [Header("Internal References")]
    [Tooltip("The root object for the weapon, this is what will be deactivated when the weapon isn't active")]
    public GameObject weaponRoot;

    [Header("Shoot Parameters")]
    [Tooltip("The type of weapon wil affect how it shoots")]
    public WeaponShootType shootType;
    [Tooltip("The projectile prefab")]
    public ProjectileBase projectilePrefab;
    [Tooltip("Minimum duration between two shots")]
    public float delayBetweenShots = 0.5f;
    [Tooltip("Angle for the cone in which the bullets will be shot randomly (0 means no spread at all)")]
    public float bulletSpreadAngle = 0f;
    [Tooltip("Amount of bullets per shot")]
    public int bulletsPerShot = 1;

    [Header("Ammo Parameters")]
    [Tooltip("Amount of ammo reloaded per second")]
    public float ammoReloadRate = 1f;
    [Tooltip("Delay after the last shot before starting to reload")]
    public float ammoReloadDelay = 2f;
    [Tooltip("Maximum amount of ammo in the gun")]
    public float maxAmmo = 8;

    [Header("Audio & Visual")]
    [Tooltip("Optional weapon animator for OnShoot animations")]
    public Animator weaponAnimator;
    [Tooltip("Prefab of the muzzle flash")]
    public GameObject muzzleFlashPrefab;
    [Tooltip("Unparent the muzzle flash instance on spawn")]
    public bool unparentMuzzleFlash;
    [Tooltip("sound played when shooting")]
    public AudioClip shootSFX;
    [Tooltip("Sound played when changing to this weapon")]
    public AudioClip changeWeaponSFX;

    public UnityAction onShoot;

    float m_CurrentAmmo;
    float m_LastTimeShot = Mathf.NegativeInfinity;
    float m_TimeBeginCharge;
    Vector3 m_LastMuzzlePosition;

    public GameObject owner { get; set; }
    public GameObject sourcePrefab { get; set; }
    public bool isCharging { get; private set; }
    public float currentAmmoRatio { get; private set; }
    public bool isWeaponActive { get; private set; }
    public bool isCooling { get; private set; }
    public float currentCharge { get; private set; }
    public Vector3 muzzleWorldVelocity { get; private set; }
    public float GetAmmoNeededToShoot() => (1) / maxAmmo;

    AudioSource m_ShootAudioSource;

    const string k_AnimAttackParameter = "Attack";

    void Awake()
    {
        m_CurrentAmmo = maxAmmo;

        m_ShootAudioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        UpdateAmmo();

        if (Time.deltaTime > 0)
        {
            //muzzleWorldVelocity = (weaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
            //m_LastMuzzlePosition = weaponMuzzle.position;
        }
    }

    void UpdateAmmo()
    {
        if (m_LastTimeShot + ammoReloadDelay < Time.time && m_CurrentAmmo < maxAmmo && !isCharging)
        {
            // reloads weapon over time
            //m_CurrentAmmo += ammoReloadRate * Time.deltaTime;
            m_CurrentAmmo = maxAmmo;

            // limits ammo to max value
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo, 0, maxAmmo);

            isCooling = true;
        }
        else
        {
            isCooling = false;
        }

        if (maxAmmo == Mathf.Infinity)
        {
            currentAmmoRatio = 1f;
        }
        else
        {
            currentAmmoRatio = m_CurrentAmmo / maxAmmo;
        }
    }

    public void UseAmmo(float amount)
    {
        m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0f, maxAmmo);
        m_LastTimeShot = Time.time;
    }

    public bool HandleShootInputs(bool inputDown, bool inputHeld)
    {
        switch (shootType)
        {
            case WeaponShootType.Manual:
                if (inputDown)
                {
                    return TryShoot();
                }
                return false;

            case WeaponShootType.Automatic:
                if (inputHeld)
                {
                    return TryShoot();
                }
                return false;

            default:
                return false;
        }
    }

    bool TryShoot()
    {
        bool hasEnoughAmmo = m_CurrentAmmo >= 1f;
        bool enoughTimePast = m_LastTimeShot + delayBetweenShots < Time.time;
        if (hasEnoughAmmo
            && enoughTimePast)
        {
            HandleShoot();
            m_CurrentAmmo -= 1;
            return true;
        }

        return false;
    }

    void HandleShoot()
    {
        var ownerWeaponManager = owner.GetComponent<PlayerWeaponsManager>();
        var muzzle = owner.GetComponent<PlayerWeaponsManager>().Muzzle;

        // spawn all bullets with random direction
        for (int i = 0; i < bulletsPerShot; i++)
        {

            Vector3 fromPosition = ownerWeaponManager.Muzzle.transform.position;
            Vector3 toPosition = ownerWeaponManager.Target.transform.position;
            Vector3 direction = toPosition - fromPosition;
            Vector3 centerCamera = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0));

            ProjectileBase newProjectile = Instantiate(projectilePrefab, fromPosition, Quaternion.LookRotation(GetShotDirectionWithinSpread(direction)));
            newProjectile.Shoot(this);
        }

        // muzzle flash
        if (muzzleFlashPrefab != null)
        {
            GameObject muzzleFlashInstance = Instantiate(muzzleFlashPrefab, muzzle.position, muzzle.rotation, muzzle.transform);
            // Unparent the muzzleFlashInstance
            if (unparentMuzzleFlash)
            {
                muzzleFlashInstance.transform.SetParent(null);
            }

            Destroy(muzzleFlashInstance, 2f);
        }

        m_LastTimeShot = Time.time;

        // play shoot SFX
        if (shootSFX)
        {
            m_ShootAudioSource.PlayOneShot(shootSFX);
        }

        // Trigger attack animation if there is any
        if (weaponAnimator)
        {
            weaponAnimator.SetTrigger(k_AnimAttackParameter);
        }

        // Callback on shoot
        if (onShoot != null)
        {
            onShoot();
        }
    }

    public Vector3 GetShotDirectionWithinSpread(Vector3 point)
    {
        float spreadAngleRatio = bulletSpreadAngle / 180f;
        Vector3 spreadWorldDirection = Vector3.Slerp(point, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);

        return spreadWorldDirection;
    }

    public void ShowWeapon(bool show)
    {
        weaponRoot.SetActive(show);

        if (show && changeWeaponSFX)
        {
            m_ShootAudioSource.PlayOneShot(changeWeaponSFX);
        }

        isWeaponActive = show;
    }
}
