using System.Collections;
using System.Diagnostics.Contracts;
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
    [Tooltip("Prefab of the muzzle flash")]
    public GameObject muzzleFlashPrefab;
    [Tooltip("Unparent the muzzle flash instance on spawn")]
    public bool unparentMuzzleFlash;
    [Tooltip("sound played when shooting")]
    public AudioClip shootSFX;
    [Tooltip("Sound played when changing to this weapon")]
    public AudioClip changeWeaponSFX;

    public UnityAction onShoot;

    public float currentAmmo;
    float m_LastTimeShot = Mathf.NegativeInfinity;
    float m_reloadStart;
    float m_TimeBeginCharge;
    Vector3 m_LastMuzzlePosition;
    GameObject m_owner;

    public GameObject Owner {
        get { return m_owner; } 
        set {
            m_owner = value;
            Character = m_owner.GetComponent<Character>();
        }        
    }
    public GameObject sourcePrefab { get; set; }
    public bool isCharging { get; private set; }
    public float currentAmmoRatio { get; private set; }
    public bool isWeaponActive { get; private set; }
    public bool isCooling { get; private set; }
    public float currentCharge { get; private set; }
    public Vector3 muzzleWorldVelocity { get; private set; }
    public float GetAmmoNeededToShoot() => (1) / maxAmmo;

    private AudioSource m_ShootAudioSource;
    private bool m_reloading;
    private bool m_reloadOverride;
    private Character Character;

    void Awake()
    {
        currentAmmo = maxAmmo;

        m_ShootAudioSource = GetComponent<AudioSource>();

        if (Character)
        {
            Character.Animator.SetBool("Reloading", false);
        }
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
        if (currentAmmo == 0 && !m_reloading || m_reloadOverride)
        {
            m_reloadStart = Time.time;
            m_reloading = true;
            m_reloadOverride = false;
            Owner.GetComponent<Character>().Animator.SetBool("Reloading", m_reloading);
            Owner.GetComponent<Character>().Animator.SetFloat("ReloadSpeed", ammoReloadRate);
        }

        if (m_reloading && Time.time - m_reloadStart > ammoReloadRate)
        {
            m_reloading = false;
            Owner.GetComponent<Character>().Animator.SetBool("Reloading", m_reloading);
            currentAmmo = maxAmmo;
        }

        if (maxAmmo == Mathf.Infinity)
        {
            currentAmmoRatio = 1f;
        }
        else
        {
            currentAmmoRatio = currentAmmo / maxAmmo;
        }
        if (isWeaponActive)
        {

        }
    }

    /// <summary>
    /// Relaod weapon from user input
    /// </summary>
    public void Reload()
    {
        if (currentAmmo != maxAmmo)
        {
            m_reloadOverride = true;
        }
    }

    public void UseAmmo(float amount)
    {
        currentAmmo = Mathf.Clamp(currentAmmo - amount, 0f, maxAmmo);
        m_LastTimeShot = Time.time;
    }

    public bool HandleShootInputs(bool inputDown, bool inputHeld)
    {
        if (m_reloading)
        {
            return false;
        }

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
        bool hasEnoughAmmo = currentAmmo >= 1f;
        bool enoughTimePast = m_LastTimeShot + delayBetweenShots < Time.time;
        if (hasEnoughAmmo
            && enoughTimePast)
        {
            HandleShoot();
            currentAmmo -= 1;
            return true;
        }

        return false;
    }

    void HandleShoot()
    {
        // spawn all bullets with random direction
        for (int i = 0; i < bulletsPerShot; i++)
        {
            Vector3 fromPosition = Character.Muzzle.transform.position;
            Vector3 toPosition = Character.Target;
            Vector3 direction = toPosition - fromPosition;

            ProjectileBase newProjectile = Instantiate(projectilePrefab, fromPosition, Quaternion.LookRotation(GetShotDirectionWithinSpread(direction)));
            newProjectile.Shoot(this);
        }

        // muzzle flash
        if (muzzleFlashPrefab != null)
        {
            GameObject muzzleFlashInstance = Instantiate(muzzleFlashPrefab, Character.Muzzle.position, Character.Muzzle.rotation, Character.Muzzle.transform);
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

        // Callback on shoot
        if (onShoot != null)
        {
            onShoot();
        }
    }

    public Vector3 GetShotDirectionWithinSpread(Vector3 point)
    {
        if (Character && Character.BulletSpreadOverride > bulletSpreadAngle)
        {
            float spreadAngleRatio = Character.BulletSpreadOverride / 180f;
            Vector3 spreadWorldDirection = Vector3.Slerp(point, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);

            return spreadWorldDirection;
        }
        else
        {
            float spreadAngleRatio = bulletSpreadAngle / 180f;
            Vector3 spreadWorldDirection = Vector3.Slerp(point, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);

            return spreadWorldDirection;
        }
    }

    public void ShowWeapon(bool show)
    {
        if (!show)
        {
            m_reloading = false;
        }

        weaponRoot.SetActive(show);

        if (show && changeWeaponSFX)
        {
            m_ShootAudioSource.PlayOneShot(changeWeaponSFX);
        }

        isWeaponActive = show;
    }

}
