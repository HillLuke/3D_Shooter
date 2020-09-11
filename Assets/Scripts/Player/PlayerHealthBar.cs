using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    public Image FillImage;

    void Update()
    {
        try
        {
            float current = (float)(PlayerController.Instance?.Health?.CurrentHealth);
            float max = (float)(PlayerController.Instance?.Health?.MaxHealth);
            FillImage.fillAmount = current / max;
        }
        catch (System.Exception)
        {

        }
    }
}
