using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnergyManager : MonoBehaviour
{
    [Header("UIの割り当て")]
    public Image energyBarFill;
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI timerText;

    [Header("デバッグ設定")]
    public int initialEnergy = 50;
    public float recoveryTimeSeconds = 300f;

    private const int GAUGE_MAX = 50;
    private int currentEnergy;
    private float currentTimer;

    private const string ENERGY_KEY = "SavedEnergy";
    private const string TIMER_KEY = "SavedTimer";
    private const string LAST_TIME_KEY = "LastSavedTime";

    void Start()
    {
        if (PlayerPrefs.HasKey(ENERGY_KEY))
        {
            currentEnergy = PlayerPrefs.GetInt(ENERGY_KEY);
            currentTimer = PlayerPrefs.GetFloat(TIMER_KEY);

            string lastTimeStr = PlayerPrefs.GetString(LAST_TIME_KEY);
            DateTime lastTime = DateTime.Parse(lastTimeStr);
            TimeSpan timePassed = DateTime.Now - lastTime;
            float passedSeconds = (float)timePassed.TotalSeconds;

            if (currentEnergy < GAUGE_MAX)
            {
                currentTimer -= passedSeconds;

                while (currentTimer <= 0f && currentEnergy < GAUGE_MAX)
                {
                    currentEnergy++;
                    currentTimer += recoveryTimeSeconds;
                }

                if (currentEnergy >= GAUGE_MAX)
                {
                    currentEnergy = GAUGE_MAX;
                    currentTimer = recoveryTimeSeconds;
                }
            }
        }
        else
        {
            currentEnergy = initialEnergy;
            currentTimer = recoveryTimeSeconds;
        }

        UpdateUI();
        UpdateTimerText();
        SaveEnergyData();
    }

    void Update()
    {
        if (currentEnergy < GAUGE_MAX)
        {
            currentTimer -= Time.deltaTime;
            UpdateTimerText();

            if (currentTimer <= 0f)
            {
                currentEnergy++;
                currentTimer = recoveryTimeSeconds;

                UpdateUI();
                UpdateTimerText();
                SaveEnergyData();
            }
        }
        else
        {
            if (currentTimer != recoveryTimeSeconds)
            {
                currentTimer = recoveryTimeSeconds;
                UpdateTimerText();
                SaveEnergyData();
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            UseEnergy(10);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            PlayerPrefs.DeleteKey(ENERGY_KEY);
            PlayerPrefs.DeleteKey(TIMER_KEY);
            PlayerPrefs.DeleteKey(LAST_TIME_KEY);
            PlayerPrefs.Save();
            Debug.Log("スタミナのセーブデータを削除しました。再起動すると初期値に戻ります。");
        }
    }

    void UpdateUI()
    {
        if (energyBarFill != null)
        {
            float fillRatio = Mathf.Clamp01((float)currentEnergy / GAUGE_MAX);
            energyBarFill.fillAmount = fillRatio;
        }

        if (energyText != null)
        {
            energyText.text = currentEnergy.ToString() + " / " + GAUGE_MAX.ToString();
        }
    }

    void UpdateTimerText()
    {
        if (timerText == null) return;

        if (currentEnergy >= GAUGE_MAX)
        {
            timerText.text = "MAX";
        }
        else
        {
            int timeInSeconds = Mathf.CeilToInt(currentTimer);
            int minutes = timeInSeconds / 60;
            int seconds = timeInSeconds % 60;
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void UseEnergy(int amount)
    {
        int actualUsed = Mathf.Min(currentEnergy, amount);
        currentEnergy -= amount;
        if (currentEnergy < 0) currentEnergy = 0;

        // ★追加：消費したスタミナ量をミッションの進行度に加算する
        if (actualUsed > 0)
        {
            MissionManager.AddStaminaUsed(actualUsed);
        }

        UpdateUI();
        UpdateTimerText();
        SaveEnergyData();
    }

    public bool HasEnoughEnergy(int amount)
    {
        return currentEnergy >= amount;
    }

    private void SaveEnergyData()
    {
        PlayerPrefs.SetInt(ENERGY_KEY, currentEnergy);
        PlayerPrefs.SetFloat(TIMER_KEY, currentTimer);
        PlayerPrefs.SetString(LAST_TIME_KEY, DateTime.Now.ToString());
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        SaveEnergyData();
    }

    private void OnApplicationQuit()
    {
        SaveEnergyData();
    }
}