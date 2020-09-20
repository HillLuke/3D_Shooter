using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InGameMenuManager : MonoBehaviour
{
    public GameObject menuRoot;

    void Start()
    {
        menuRoot.SetActive(false);
    }


    void Update()
    {
        if (Input.GetButtonDown("Pause Menu"))
        {
            menuRoot.SetActive(!menuRoot.activeSelf);

            if (menuRoot.activeSelf)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if ((menuRoot.activeSelf && Input.GetButtonDown("Cancel")))
        {
            menuRoot.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
