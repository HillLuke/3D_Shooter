using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaBar : MonoBehaviour
{
    public Image FillImage;

    void Update()
    {
        try
        {
            float current = (float)(PlayerController.Instance?.CurrentStamina);
            float max = (float)(PlayerController.Instance?.MaxStamina);
            FillImage.fillAmount = current / max;
            //Debug.Log($"fill: {FillImage.fillAmount}  current: {current}  max:{max}");
        }
        catch (System.Exception)
        {
            //TODO redo this and the health bar.
        }
    }
}
