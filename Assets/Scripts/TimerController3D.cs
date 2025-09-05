// ==================== TimerController3D.cs ====================
// Arquivo: TimerController3D.cs
// Anexe este script ao GameObject "GameManager"

using UnityEngine;
using TMPro;
using System.Collections;

public class TimerController3D : MonoBehaviour
{
    private float currentTime;
    private bool timerRunning = false;

    public void StartTimer(float duration)
    {
        currentTime = duration;
        timerRunning = true;
        StartCoroutine(TimerCoroutine3D());
    }

    public void StopTimer()
    {
        timerRunning = false;
        StopAllCoroutines();
    }

    IEnumerator TimerCoroutine3D()
    {
        while (currentTime > 0 && timerRunning)
        {
            UpdateTimerDisplay3D();
            yield return new WaitForSeconds(1f);
            currentTime--;
        }

        if (timerRunning)
        {
            GameManager3D.Instance.OnTimerEnd();
        }
    }

    void UpdateTimerDisplay3D()
    {
        if (GameManager3D.Instance.timerText)
        {
            GameManager3D.Instance.timerText.text = Mathf.Ceil(currentTime).ToString();

            // Dramatic color changes for 3D
            if (currentTime <= 3)
            {
                GameManager3D.Instance.timerText.color = Color.red;
                GameManager3D.Instance.timerText.fontSize = 80; // Bigger for urgency
            }
            else if (currentTime <= 5)
            {
                GameManager3D.Instance.timerText.color = Color.yellow;
                GameManager3D.Instance.timerText.fontSize = 70;
            }
            else if (currentTime <= 10)
            {
                GameManager3D.Instance.timerText.color = Color.orange;
                GameManager3D.Instance.timerText.fontSize = 60;
            }
            else
            {
                GameManager3D.Instance.timerText.color = Color.white;
                GameManager3D.Instance.timerText.fontSize = 50;
            }
        }
    }
}