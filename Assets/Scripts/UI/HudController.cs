using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class HudController : MonoBehaviour
{
    public GameObject centreDot;
    public GameObject interactPrompt;
    public GameObject radioPrompt;

    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI batteryText;

    public void UpdateMoneyText(float money)
    {
        moneyText.text = money.ToString("F2");
    }

    public void UpdateBatteryText(float battery)
    {
        if(battery < 0)
        {
            battery = 0;
        }

        batteryText.text = battery.ToString("F2");
    }

    public void ShowInteractPrompt(bool show)
    {
        interactPrompt.SetActive(show);
    }

    public void ShowRadioPrompt(bool show)
    {
        radioPrompt.SetActive(show);
    }
}
