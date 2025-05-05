using UnityEngine;
using TMPro;

/// <summary>
/// Simple timer that displays elapsed time in the UI.
/// </summary>
public class TimerTest : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    private float elapsedTime;

    private void Start()
    {
        elapsedTime = 0f;
        if (timerText == null)
        {
            timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }
}
