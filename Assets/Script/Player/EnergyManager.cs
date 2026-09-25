using System; // ★追加：時間（DateTime）の計算に必要
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

    // ★追加：セーブデータ用の「合言葉（キー）」
    private const string ENERGY_KEY = "SavedEnergy";
    private const string TIMER_KEY = "SavedTimer";
    private const string LAST_TIME_KEY = "LastSavedTime";

    void Start()
    {
        // ★大改修：セーブデータがあるかどうかをチェックする
        if (PlayerPrefs.HasKey(ENERGY_KEY))
        {
            // 1. セーブデータを読み込む
            currentEnergy = PlayerPrefs.GetInt(ENERGY_KEY);
            currentTimer = PlayerPrefs.GetFloat(TIMER_KEY);

            // 2. 「最後に遊んだ時間」と「今の時間」の差（経過した秒数）を計算する
            string lastTimeStr = PlayerPrefs.GetString(LAST_TIME_KEY);
            DateTime lastTime = DateTime.Parse(lastTimeStr);
            TimeSpan timePassed = DateTime.Now - lastTime;
            float passedSeconds = (float)timePassed.TotalSeconds;

            // 3. 経過した時間の分だけ、裏でタイマーを回してスタミナを回復させる
            if (currentEnergy < GAUGE_MAX)
            {
                currentTimer -= passedSeconds;

                // タイマーがマイナスになっている間（回復分が貯まっている間）、回復し続ける
                while (currentTimer <= 0f && currentEnergy < GAUGE_MAX)
                {
                    currentEnergy++;
                    currentTimer += recoveryTimeSeconds;
                }

                // もし計算の結果、MAXまで回復していたらタイマーを綺麗にリセット
                if (currentEnergy >= GAUGE_MAX)
                {
                    currentEnergy = GAUGE_MAX;
                    currentTimer = recoveryTimeSeconds;
                }
            }
        }
        else
        {
            // セーブデータがない（本当に初めてゲームを起動した）時だけ初期値を入れる
            currentEnergy = initialEnergy;
            currentTimer = recoveryTimeSeconds;
        }

        UpdateUI();
        UpdateTimerText();
        SaveEnergyData(); // 開始時に一度セーブしておく
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
                SaveEnergyData(); // ★追加：回復した瞬間にセーブ！
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

        // ===============================================
        // 【テスト用】Zキー：スタミナ10消費
        // ===============================================
        if (Input.GetKeyDown(KeyCode.Z))
        {
            UseEnergy(10);
        }

        // ===============================================
        // 【テスト用】Cキー：セーブデータを消去して完全初期化
        // ===============================================
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
        currentEnergy -= amount;
        if (currentEnergy < 0) currentEnergy = 0;

        UpdateUI();
        UpdateTimerText();
        SaveEnergyData(); // ★追加：消費した瞬間にセーブ！
    }

    public bool HasEnoughEnergy(int amount)
    {
        return currentEnergy >= amount;
    }

    // ===============================================
    // ★追加：現在のスタミナ状況と「今の時間」を保存する処理
    // ===============================================
    private void SaveEnergyData()
    {
        PlayerPrefs.SetInt(ENERGY_KEY, currentEnergy);
        PlayerPrefs.SetFloat(TIMER_KEY, currentTimer);
        PlayerPrefs.SetString(LAST_TIME_KEY, DateTime.Now.ToString()); // 今のリアルな時間を保存
        PlayerPrefs.Save();
    }

    // ★追加：シーン移動などでオブジェクトが消える直前にも確実にセーブする
    private void OnDestroy()
    {
        SaveEnergyData();
    }

    // ★追加：ゲームアプリ自体を終了した瞬間にも確実にセーブする
    private void OnApplicationQuit()
    {
        SaveEnergyData();
    }
}