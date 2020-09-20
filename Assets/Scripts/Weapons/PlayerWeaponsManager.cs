using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerWeaponsManager : MonoBehaviour
{
    public enum WeaponSwitchState
    {
        Up,
        Down,
        PutDownPrevious,
        PutUpNew,
    }

    public Camera PlayerCamera;
    public Transform WeaponParentSocket;
    public Transform Target;
    public Transform LookAt;
    public List<WeaponController> StartingWeapons = new List<WeaponController>();
    public int ActiveWeaponIndex { get; private set; }
    public bool IsPointingAtEnemy { get; private set; }
    public LayerMask FPSWeaponLayer;
    public LayerMask AimCollision;
    public LayerMask LookAtLayerMask;
    public float weaponSwitchDelay = 1f;

    public UnityAction<WeaponController> onSwitchedToWeapon;
    public UnityAction<WeaponController, int> onAddedWeapon;
    public UnityAction<WeaponController, int> onRemovedWeapon;

    private Character Character;
    private WeaponController[] m_WeaponSlots = new WeaponController[9]; // 9 available weapon slots
    private WeaponSwitchState m_WeaponSwitchState;
    private int m_WeaponSwitchNewWeaponIndex;
    private float m_TimeStartedWeaponSwitch;
    bool m_FireInputWasHeld;

    // Start is called before the first frame update
    void Start()
    {
        Character = GetComponent<Character>();
        
        ActiveWeaponIndex = -1;
        m_WeaponSwitchState = WeaponSwitchState.Down;
        onSwitchedToWeapon += OnWeaponSwitched;

        foreach (var weapon in StartingWeapons)
        {
            AddWeapon(weapon);
        }

        SwitchWeapon(true);
    }

    // Update is called once per frame
    void Update()
    {
        // weapon switch handling
        string axisName = "Mouse ScrollWheel";
        int change = 0;
        if (Input.GetAxis(axisName) > 0f)
            change = -1;
        else if (Input.GetAxis(axisName) < 0f)
            change = 1;
        if (change != 0)
        {
            bool switchUp = change > 0;
            SwitchWeapon(switchUp);
        }

        WeaponController activeWeapon = GetActiveWeapon();

        // Pointing at enemy handling
        IsPointingAtEnemy = false;
        if (activeWeapon)
        {
            if (Physics.Raycast(PlayerCamera.transform.position, PlayerCamera.transform.forward, out RaycastHit hit, 200, AimCollision, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<EnemyController>())
                {
                    IsPointingAtEnemy = true;
                }
                if (Target != null)
                {
                    Target.transform.position = hit.point;
                    Character.Target = (hit.point);
                }
            }
        }

        if (Physics.Raycast(PlayerCamera.transform.position, PlayerCamera.transform.forward, out RaycastHit lookathit, 200, LookAtLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (LookAt != null)
            {
                LookAt.transform.position = lookathit.point;
                Character.Target = lookathit.point;
            }
        }

        bool hasFired = activeWeapon.HandleShootInputs(
                GetFireInputDown(),
                GetFireInputHeld());

        if (Input.GetKeyDown(KeyCode.R))
        {
            activeWeapon.Reload();
        }
    }

    private void LateUpdate()
    {
        UpdateWeaponSwitching();
        m_FireInputWasHeld = GetFireInputHeld();
    }

    void UpdateWeaponSwitching()
    {
        // Calculate the time ratio (0 to 1) since weapon switch was triggered
        float switchingTimeFactor = 0f;
        if (weaponSwitchDelay == 0f)
        {
            switchingTimeFactor = 1f;
        }
        else
        {
            switchingTimeFactor = Mathf.Clamp01((Time.time - m_TimeStartedWeaponSwitch) / weaponSwitchDelay);
        }

        // Handle transiting to new switch state
        if (switchingTimeFactor >= 1f)
        {
            if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                // Deactivate old weapon
                WeaponController oldWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                if (oldWeapon != null)
                {
                    oldWeapon.ShowWeapon(false);
                }

                ActiveWeaponIndex = m_WeaponSwitchNewWeaponIndex;
                switchingTimeFactor = 0f;

                // Activate new weapon
                WeaponController newWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                if (onSwitchedToWeapon != null)
                {
                    onSwitchedToWeapon.Invoke(newWeapon);
                }

                if (newWeapon)
                {
                    m_TimeStartedWeaponSwitch = Time.time;
                    m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                }
                else
                {
                    // if new weapon is null, don't follow through with putting weapon back up
                    m_WeaponSwitchState = WeaponSwitchState.Down;
                }
            }
            else if (m_WeaponSwitchState == WeaponSwitchState.PutUpNew)
            {
                m_WeaponSwitchState = WeaponSwitchState.Up;
            }
        }
    }

    public bool GetFireInputDown()
    {
        return GetFireInputHeld() && !m_FireInputWasHeld;
    }

    public bool GetFireInputReleased()
    {
        return !GetFireInputHeld() && m_FireInputWasHeld;
    }

    public bool GetFireInputHeld()
    {
        return Input.GetMouseButton(0);
    }

    public bool HasWeapon(WeaponController weaponPrefab)
    {
        // Checks if we already have a weapon coming from the specified prefab
        foreach (var w in m_WeaponSlots)
        {
            if (w != null && w.sourcePrefab == weaponPrefab.gameObject)
            {
                return true;
            }
        }

        return false;
    }

    public bool AddWeapon(WeaponController weaponPrefab)
    {
        // if we already hold this weapon type (a weapon coming from the same source prefab), don't add the weapon
        if (HasWeapon(weaponPrefab))
        {
            return false;
        }

        // search our weapon slots for the first free one, assign the weapon to it, and return true if we found one. Return false otherwise
        for (int i = 0; i < m_WeaponSlots.Length; i++)
        {
            // only add the weapon if the slot is free
            if (m_WeaponSlots[i] == null)
            {
                // spawn the weapon prefab as child of the weapon socket
                WeaponController weaponInstance = Instantiate(weaponPrefab, WeaponParentSocket);
                weaponInstance.transform.localPosition = Vector3.zero;
                weaponInstance.transform.localRotation = Quaternion.identity;

                // Set owner to this gameObject so the weapon can alter projectile/damage logic accordingly
                weaponInstance.Owner = gameObject;
                weaponInstance.sourcePrefab = weaponPrefab.gameObject;

                // Assign the first person layer to the weapon
                int layerIndex = Mathf.RoundToInt(Mathf.Log(FPSWeaponLayer.value, 2)); // This function converts a layermask to a layer index
                foreach (Transform t in weaponInstance.gameObject.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = layerIndex;
                }

                m_WeaponSlots[i] = weaponInstance;

                if (onAddedWeapon != null)
                {
                    onAddedWeapon.Invoke(weaponInstance, i);
                }

                return true;
            }
        }

        // Handle auto-switching to weapon if no weapons currently
        if (GetActiveWeapon() == null)
        {
            SwitchWeapon(true);
        }

        return false;
    }
    public WeaponController GetWeaponAtSlotIndex(int index)
    {
        // find the active weapon in our weapon slots based on our active weapon index
        if (index >= 0 &&
            index < m_WeaponSlots.Length)
        {
            return m_WeaponSlots[index];
        }

        // if we didn't find a valid active weapon in our weapon slots, return null
        return null;
    }

    public void SwitchWeapon(bool ascendingOrder)
    {
        Character.Animator.SetBool("Reloading", false);
        int newWeaponIndex = -1;
        int closestSlotDistance = m_WeaponSlots.Length;
        for (int i = 0; i < m_WeaponSlots.Length; i++)
        {
            // If the weapon at this slot is valid, calculate its "distance" from the active slot index (either in ascending or descending order)
            // and select it if it's the closest distance yet
            if (i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
            {
                int distanceToActiveIndex = GetDistanceBetweenWeaponSlots(ActiveWeaponIndex, i, ascendingOrder);

                if (distanceToActiveIndex < closestSlotDistance)
                {
                    closestSlotDistance = distanceToActiveIndex;
                    newWeaponIndex = i;
                }
            }
        }

        // Handle switching to the new weapon index
        SwitchToWeaponIndex(newWeaponIndex);
    }

    public void SwitchToWeaponIndex(int newWeaponIndex, bool force = false)
    {
        if (force || (newWeaponIndex != ActiveWeaponIndex && newWeaponIndex >= 0))
        {
            // Store data related to weapon switching animation
            m_WeaponSwitchNewWeaponIndex = newWeaponIndex;
            m_TimeStartedWeaponSwitch = Time.time;

            // Handle case of switching to a valid weapon for the first time (simply put it up without putting anything down first)
            if (GetActiveWeapon() == null)
            {
                m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                ActiveWeaponIndex = m_WeaponSwitchNewWeaponIndex;

                WeaponController newWeapon = GetWeaponAtSlotIndex(m_WeaponSwitchNewWeaponIndex);
                if (onSwitchedToWeapon != null)
                {
                    onSwitchedToWeapon.Invoke(newWeapon);
                }
            }
            // otherwise, remember we are putting down our current weapon for switching to the next one
            else
            {
                m_WeaponSwitchState = WeaponSwitchState.PutDownPrevious;
            }
        }
    }

    int GetDistanceBetweenWeaponSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
    {
        int distanceBetweenSlots = 0;

        if (ascendingOrder)
        {
            distanceBetweenSlots = toSlotIndex - fromSlotIndex;
        }
        else
        {
            distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);
        }

        if (distanceBetweenSlots < 0)
        {
            distanceBetweenSlots = m_WeaponSlots.Length + distanceBetweenSlots;
        }

        return distanceBetweenSlots;
    }

    public WeaponController GetActiveWeapon()
    {
        return GetWeaponAtSlotIndex(ActiveWeaponIndex);
    }

    void OnWeaponSwitched(WeaponController newWeapon)
    {
        if (newWeapon != null)
        {
            newWeapon.ShowWeapon(true);
        }
    }
}
