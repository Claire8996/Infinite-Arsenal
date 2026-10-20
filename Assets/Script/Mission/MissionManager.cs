using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MissionCategory
{
    AutoDetectFromText, // ミッション内容の文章と敵Prefabの有無から自動判別
    TotalEnemyKills,    // 敵を合計〇体倒す
    TotalDamageDealt,   // 合計ダメージ数〇
    StagePlayCount,     // ステージを〇回挑戦する
    SpecificEnemyKills, // 特定の敵を〇体倒そう
    StaminaConsumed     // スタミナを〇消費しよう
}

[System.Serializable]
public class MissionEntry
{
    [Tooltip("ミッション内容のテキスト（例：「敵を合計15体倒す」「特定の敵を10体倒そう」）")]
    public string missionDescription = "敵を合計15体倒す";

    [Tooltip("特定の敵を倒すミッションの場合はここに敵のPrefabをセットしてください（何もアタッチしなければ特定の敵ミッションではないと自動認識します）")]
    public GameObject targetEnemyPrefab;

    [Tooltip("このミッションをクリアした際にもらえるエメラルド数")]
    public int rewardEmeralds = 100;

    [Header("【詳細設定（AutoDetectのままでも文章から自動判別します）】")]
    public MissionCategory category = MissionCategory.AutoDetectFromText;

    [Tooltip("目標回数・目標数値（0にしておくと、上のミッション内容の文章内の数字を自動で読み取ります）")]
    public int targetAmount = 0;
}

public class MissionManager : MonoBehaviour
{
    [Header("【ミッション画面の表示・非表示ボタン設定】")]
    [Tooltip("押したらミッション画面（とあるオブジェクト）を表示するボタン")]
    public Button openMissionButton;

    [Tooltip("押したらミッション画面（とあるオブジェクト）を非表示にするBackButton")]
    public Button backButton;

    [Tooltip("表示・非表示を切り替えるとあるオブジェクト（ミッション画面全体のパネルなど）")]
    public GameObject missionWindowObject;

    // ==========================================
    // ★追加：ミッションがクリア（ボタンが押せる状態）になった時にホーム画面に表示するUI
    // ==========================================
    [Header("【ホーム画面の通知UI設定】")]
    [Tooltip("クリアしたミッションの報酬受取ボタンが押せるようになったら表示し、すべて受け取って押せるボタンがなくなったら非表示にするホーム画面のUIオブジェクト")]
    public GameObject homeNotificationUI;

    [Header("【17時リセット＆カウントダウン表示設定】")]
    [Tooltip("17時までのカウントダウン数値を表示するテキスト（TextMeshPro）")]
    public TextMeshProUGUI resetCountdownTMP;

    [Tooltip("（標準Textを使う場合）17時までのカウントダウン数値を表示するテキスト")]
    public Text resetCountdownText;

    [Tooltip("リセットされる時刻（時）※デフォルトは17時（17）")]
    public int resetHour = 17;

    [Header("【ミッションUIのPrefab＆表示先設定】")]
    [Tooltip("ミッションのテキストやエメラルド数、ボタンを表示するPrefab（MissionItemUIが付いたPrefab）")]
    public GameObject missionItemPrefab;

    [Tooltip("Prefabを上から順々に並べて表示する親UI（ContentやVerticalLayoutGroupが付いたUI）")]
    public Transform missionListParent;

    [Tooltip("ミッションクリア時のボタンを押した後に切り替わるUI（Prefab側で設定している場合は空欄でOK）")]
    public GameObject completedUIPrefabOrObject;

    [Tooltip("ランダムで何個のミッションを表示するか（初期値：5個）")]
    public int displayMissionCount = 5;

    [Header("【ミッション内容の一覧配列（今後いつでも追加可能）】")]
    public MissionEntry[] missions = new MissionEntry[]
    {
        new MissionEntry { missionDescription = "敵を合計15体倒す", rewardEmeralds = 50 },
        new MissionEntry { missionDescription = "敵を合計30体倒す", rewardEmeralds = 100 },
        new MissionEntry { missionDescription = "敵を合計60体倒す", rewardEmeralds = 200 },
        new MissionEntry { missionDescription = "敵を合計120体倒す", rewardEmeralds = 400 },
        new MissionEntry { missionDescription = "合計ダメージ数5000", rewardEmeralds = 50 },
        new MissionEntry { missionDescription = "合計ダメージ数10000", rewardEmeralds = 100 },
        new MissionEntry { missionDescription = "合計ダメージ数20000", rewardEmeralds = 200 },
        new MissionEntry { missionDescription = "合計ダメージ数50000", rewardEmeralds = 400 },
        new MissionEntry { missionDescription = "ステージを3回挑戦する", rewardEmeralds = 50 },
        new MissionEntry { missionDescription = "ステージを5回挑戦する", rewardEmeralds = 100 },
        new MissionEntry { missionDescription = "ステージを10回挑戦する", rewardEmeralds = 200 },
        new MissionEntry { missionDescription = "ステージを15回挑戦する", rewardEmeralds = 300 },
        new MissionEntry { missionDescription = "特定の敵を10体倒そう", rewardEmeralds = 100 },
        new MissionEntry { missionDescription = "特定の敵を20体倒そう", rewardEmeralds = 200 },
        new MissionEntry { missionDescription = "特定の敵を30体倒そう", rewardEmeralds = 300 },
        new MissionEntry { missionDescription = "特定の敵を50体倒そう", rewardEmeralds = 500 },
        new MissionEntry { missionDescription = "スタミナを15消費しよう", rewardEmeralds = 50 },
        new MissionEntry { missionDescription = "スタミナを30消費しよう", rewardEmeralds = 100 },
        new MissionEntry { missionDescription = "スタミナを50消費しよう", rewardEmeralds = 200 }
    };

    // セーブデータ用のキー
    private const string KEY_TOTAL_KILLS = "Mission_TotalEnemyKills";
    private const string KEY_TOTAL_DAMAGE = "Mission_TotalDamageDealt";
    private const string KEY_STAGE_PLAYS = "Mission_StagePlayCount";
    private const string KEY_STAMINA_USED = "Mission_StaminaConsumed";
    private const string KEY_SPECIFIC_KILL_PREFIX = "Mission_SpecificEnemyKill_";
    private const string KEY_CLAIMED_PREFIX = "Mission_Claimed_";
    private const string KEY_SELECTED_INDICES = "Mission_SelectedRandomIndices";
    private const string KEY_LAST_RESET_CYCLE = "Mission_LastResetCycleId";

    // 現在ランダムに選ばれているミッションの配列インデックスリスト
    private List<int> activeMissionIndices = new List<int>();

    // 生成されたミッションUIとミッション番号のペア
    private class SpawnedMissionPair
    {
        public int missionIndex;
        public MissionItemUI itemUI;
    }
    private List<SpawnedMissionPair> spawnedMissionItems = new List<SpawnedMissionPair>();

    void Start()
    {
        if (openMissionButton != null)
        {
            openMissionButton.onClick.RemoveListener(OpenMissionWindow);
            openMissionButton.onClick.AddListener(OpenMissionWindow);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(CloseMissionWindow);
            backButton.onClick.AddListener(CloseMissionWindow);
        }

        CheckAndProcessDailyReset(false);

        SelectOrLoadRandomMissions();
        BuildMissionListUI();

        UpdateCountdownDisplay();
        UpdateHomeNotificationUIState(); // 初回起動時にも通知UIの表示状態をチェック
    }

    void Update()
    {
        CheckAndProcessDailyReset(true);
        UpdateCountdownDisplay();
    }

    private string GetCurrentResetCycleId()
    {
        DateTime now = DateTime.Now;
        int hour = Mathf.Clamp(resetHour, 0, 23);
        DateTime todayResetTime = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0);

        DateTime currentCycleStart = (now >= todayResetTime) ? todayResetTime : todayResetTime.AddDays(-1);
        return currentCycleStart.ToString("yyyy-MM-dd");
    }

    private DateTime GetNextResetDateTime()
    {
        DateTime now = DateTime.Now;
        int hour = Mathf.Clamp(resetHour, 0, 23);
        DateTime todayResetTime = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0);

        return (now >= todayResetTime) ? todayResetTime.AddDays(1) : todayResetTime;
    }

    private void CheckAndProcessDailyReset(bool rebuildUIIfReset)
    {
        string currentCycleId = GetCurrentResetCycleId();
        string savedCycleId = PlayerPrefs.GetString(KEY_LAST_RESET_CYCLE, "");

        if (savedCycleId != currentCycleId)
        {
            if (!string.IsNullOrEmpty(savedCycleId))
            {
                ResetAllMissionProgressAndSelection();
            }

            PlayerPrefs.SetString(KEY_LAST_RESET_CYCLE, currentCycleId);
            PlayerPrefs.Save();

            if (rebuildUIIfReset)
            {
                SelectOrLoadRandomMissions();
                BuildMissionListUI();
                UpdateHomeNotificationUIState();
                Debug.Log($"{resetHour}時になったため、ミッションがリセットされランダムに切り替わりました！");
            }
        }
    }

    private void ResetAllMissionProgressAndSelection()
    {
        PlayerPrefs.DeleteKey(KEY_TOTAL_KILLS);
        PlayerPrefs.DeleteKey(KEY_TOTAL_DAMAGE);
        PlayerPrefs.DeleteKey(KEY_STAGE_PLAYS);
        PlayerPrefs.DeleteKey(KEY_STAMINA_USED);
        PlayerPrefs.DeleteKey(KEY_SELECTED_INDICES);

        if (missions != null)
        {
            for (int i = 0; i < missions.Length; i++)
            {
                PlayerPrefs.DeleteKey(KEY_CLAIMED_PREFIX + i);

                if (missions[i] != null && missions[i].targetEnemyPrefab != null)
                {
                    string enemyKey = CleanPrefabName(missions[i].targetEnemyPrefab.name);
                    PlayerPrefs.DeleteKey(KEY_SPECIFIC_KILL_PREFIX + enemyKey);
                }
            }
        }

        PlayerPrefs.Save();
    }

    private void UpdateCountdownDisplay()
    {
        if (resetCountdownTMP == null && resetCountdownText == null) return;

        DateTime nextReset = GetNextResetDateTime();
        TimeSpan remaining = nextReset - DateTime.Now;

        if (remaining.TotalSeconds < 0)
        {
            remaining = TimeSpan.Zero;
        }

        int hours = (int)remaining.TotalHours;
        int minutes = remaining.Minutes;
        int seconds = remaining.Seconds;

        string timeText = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);

        if (resetCountdownTMP != null)
        {
            resetCountdownTMP.text = timeText;
        }

        if (resetCountdownText != null)
        {
            resetCountdownText.text = timeText;
        }
    }

    public void OpenMissionWindow()
    {
        CheckAndProcessDailyReset(true);

        if (missionWindowObject != null)
        {
            missionWindowObject.SetActive(true);
        }
        RefreshAllMissionUIs();
        UpdateCountdownDisplay();
    }

    public void CloseMissionWindow()
    {
        if (missionWindowObject != null)
        {
            missionWindowObject.SetActive(false);
        }
        // ミッション画面を閉じた時にもホーム画面の通知UIの状態を再確認して更新する
        UpdateHomeNotificationUIState();
    }

    private void SelectOrLoadRandomMissions()
    {
        activeMissionIndices.Clear();
        if (missions == null || missions.Length == 0) return;

        int countToSelect = Mathf.Clamp(displayMissionCount, 1, missions.Length);

        if (PlayerPrefs.HasKey(KEY_SELECTED_INDICES))
        {
            string savedStr = PlayerPrefs.GetString(KEY_SELECTED_INDICES, "");
            if (!string.IsNullOrEmpty(savedStr))
            {
                string[] parts = savedStr.Split(',');
                foreach (string p in parts)
                {
                    if (int.TryParse(p, out int idx) && idx >= 0 && idx < missions.Length)
                    {
                        if (!activeMissionIndices.Contains(idx))
                        {
                            activeMissionIndices.Add(idx);
                        }
                    }
                }
            }
        }

        if (activeMissionIndices.Count != countToSelect)
        {
            activeMissionIndices.Clear();
            List<int> candidatePool = new List<int>();

            for (int i = 0; i < missions.Length; i++)
            {
                candidatePool.Add(i);
            }

            for (int i = 0; i < countToSelect && candidatePool.Count > 0; i++)
            {
                int randIdx = UnityEngine.Random.Range(0, candidatePool.Count);
                activeMissionIndices.Add(candidatePool[randIdx]);
                candidatePool.RemoveAt(randIdx);
            }

            SaveSelectedMissionIndices();
        }
    }

    private void SaveSelectedMissionIndices()
    {
        List<string> strList = new List<string>();
        foreach (int idx in activeMissionIndices)
        {
            strList.Add(idx.ToString());
        }
        PlayerPrefs.SetString(KEY_SELECTED_INDICES, string.Join(",", strList));
        PlayerPrefs.Save();
    }

    private void BuildMissionListUI()
    {
        if (missionListParent == null || missionItemPrefab == null) return;

        foreach (Transform child in missionListParent)
        {
            Destroy(child.gameObject);
        }
        spawnedMissionItems.Clear();

        foreach (int mIndex in activeMissionIndices)
        {
            if (mIndex < 0 || mIndex >= missions.Length) continue;

            MissionEntry entry = missions[mIndex];
            GameObject itemObj = Instantiate(missionItemPrefab, missionListParent);

            MissionItemUI itemUI = itemObj.GetComponent<MissionItemUI>();
            if (itemUI == null)
            {
                itemUI = itemObj.AddComponent<MissionItemUI>();
            }

            MissionCategory actualCategory = DetermineCategory(entry);
            int currentProgress = GetCurrentProgress(entry, actualCategory);
            int targetAmount = GetRequiredAmount(entry);
            bool isCleared = IsMissionCleared(entry);
            bool isClaimed = IsMissionClaimed(mIndex);

            int capturedIndex = mIndex;
            itemUI.Setup(
                entry.missionDescription,
                entry.rewardEmeralds,
                currentProgress,
                targetAmount,
                isCleared,
                isClaimed,
                completedUIPrefabOrObject,
                () => OnClaimMissionReward(capturedIndex)
            );

            spawnedMissionItems.Add(new SpawnedMissionPair
            {
                missionIndex = mIndex,
                itemUI = itemUI
            });
        }

        SortMissionUIOrder();
        UpdateHomeNotificationUIState();
    }

    public void RefreshAllMissionUIs()
    {
        foreach (var pair in spawnedMissionItems)
        {
            if (pair.itemUI == null) continue;
            MissionEntry entry = missions[pair.missionIndex];
            MissionCategory actualCategory = DetermineCategory(entry);
            int currentProgress = GetCurrentProgress(entry, actualCategory);
            int targetAmount = GetRequiredAmount(entry);
            bool isCleared = IsMissionCleared(entry);
            bool isClaimed = IsMissionClaimed(pair.missionIndex);

            pair.itemUI.RefreshState(currentProgress, targetAmount, isCleared, isClaimed);
        }

        SortMissionUIOrder();
        UpdateHomeNotificationUIState();
    }

    private void OnClaimMissionReward(int missionIndex)
    {
        if (missionIndex < 0 || missionIndex >= missions.Length) return;
        if (IsMissionClaimed(missionIndex)) return;

        MissionEntry entry = missions[missionIndex];
        if (!IsMissionCleared(entry)) return;

        PlayerPrefs.SetInt(KEY_CLAIMED_PREFIX + missionIndex, 1);

        EmeraldDisplay emeraldDisplay = FindObjectOfType<EmeraldDisplay>();
        int defaultEmeralds = (emeraldDisplay != null) ? emeraldDisplay.defaultEmeralds : 1000;
        int oldEmeralds = PlayerPrefs.GetInt("PlayerEmeralds", defaultEmeralds);
        int newEmeralds = oldEmeralds + entry.rewardEmeralds;
        PlayerPrefs.SetInt("PlayerEmeralds", newEmeralds);
        PlayerPrefs.Save();

        MissionItemUI claimedItemUI = null;
        GameObject flyingUISource = null;
        foreach (var pair in spawnedMissionItems)
        {
            if (pair.missionIndex == missionIndex && pair.itemUI != null)
            {
                claimedItemUI = pair.itemUI;
                flyingUISource = pair.itemUI.flyingUIObject;
                break;
            }
        }

        if (emeraldDisplay != null)
        {
            emeraldDisplay.PlayMissionEmeraldAcquireEffect(oldEmeralds, newEmeralds, flyingUISource);
        }

        Debug.Log($"ミッション「{entry.missionDescription}」クリア報酬としてエメラルドを {entry.rewardEmeralds} 個獲得しました！（現在: {newEmeralds}個）");

        if (claimedItemUI != null)
        {
            MissionCategory actualCategory = DetermineCategory(entry);
            int currentProgress = GetCurrentProgress(entry, actualCategory);
            int targetAmount = GetRequiredAmount(entry);
            claimedItemUI.RefreshState(currentProgress, targetAmount, true, true);
        }

        SortMissionUIOrder();
        UpdateHomeNotificationUIState(); // 報酬を受け取ったのでホーム画面の通知UI状態を再チェック
    }

    private void SortMissionUIOrder()
    {
        int siblingOrder = 0;

        foreach (var pair in spawnedMissionItems)
        {
            if (pair.itemUI != null && !pair.itemUI.IsClaimed)
            {
                pair.itemUI.transform.SetSiblingIndex(siblingOrder);
                siblingOrder++;
            }
        }

        foreach (var pair in spawnedMissionItems)
        {
            if (pair.itemUI != null && pair.itemUI.IsClaimed)
            {
                pair.itemUI.transform.SetSiblingIndex(siblingOrder);
                siblingOrder++;
            }
        }
    }

    // ==========================================
    // ★追加：現在表示されているミッションの中に「クリアしていてまだ報酬を受け取っていない（＝ボタンが押せる）」ものが1つでもあるかチェックし、
    // ホーム画面の通知UIの表示・非表示を切り替えるメソッド
    // ==========================================
    private void UpdateHomeNotificationUIState()
    {
        if (homeNotificationUI == null) return;

        bool hasClaimableMission = false;

        foreach (var pair in spawnedMissionItems)
        {
            if (pair.itemUI != null)
            {
                // クリアしている（IsCleared == true）かつ、まだ報酬を受け取っていない（IsClaimed == false）ならボタンが押せる状態
                if (pair.itemUI.IsCleared && !pair.itemUI.IsClaimed)
                {
                    hasClaimableMission = true;
                    break;
                }
            }
        }

        // 押せるボタンが1つでもあれば表示、なければ非表示にする
        homeNotificationUI.SetActive(hasClaimableMission);
    }

    private bool IsMissionCleared(MissionEntry entry)
    {
        if (entry == null) return false;

        MissionCategory actualCategory = DetermineCategory(entry);
        int requiredAmount = GetRequiredAmount(entry);

        if (actualCategory == MissionCategory.SpecificEnemyKills && entry.targetEnemyPrefab == null)
        {
            return false;
        }

        int currentProgress = GetCurrentProgress(entry, actualCategory);
        return currentProgress >= requiredAmount;
    }

    private bool IsMissionClaimed(int missionIndex)
    {
        return PlayerPrefs.GetInt(KEY_CLAIMED_PREFIX + missionIndex, 0) == 1;
    }

    private MissionCategory DetermineCategory(MissionEntry entry)
    {
        if (entry.targetEnemyPrefab != null)
        {
            return MissionCategory.SpecificEnemyKills;
        }

        if (entry.category != MissionCategory.AutoDetectFromText && entry.category != MissionCategory.SpecificEnemyKills)
        {
            return entry.category;
        }

        string desc = entry.missionDescription ?? "";

        if (desc.Contains("ダメージ"))
        {
            return MissionCategory.TotalDamageDealt;
        }
        if (desc.Contains("ステージ") || desc.Contains("挑戦"))
        {
            return MissionCategory.StagePlayCount;
        }
        if (desc.Contains("スタミナ") || desc.Contains("消費"))
        {
            return MissionCategory.StaminaConsumed;
        }
        if (desc.Contains("特定の敵"))
        {
            return MissionCategory.SpecificEnemyKills;
        }

        return MissionCategory.TotalEnemyKills;
    }

    private int GetRequiredAmount(MissionEntry entry)
    {
        if (entry.targetAmount > 0)
        {
            return entry.targetAmount;
        }

        if (!string.IsNullOrEmpty(entry.missionDescription))
        {
            Match match = Regex.Match(entry.missionDescription, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int parsedValue))
            {
                return Mathf.Max(1, parsedValue);
            }
        }

        return 1;
    }

    private int GetCurrentProgress(MissionEntry entry, MissionCategory category)
    {
        switch (category)
        {
            case MissionCategory.TotalEnemyKills:
                return PlayerPrefs.GetInt(KEY_TOTAL_KILLS, 0);

            case MissionCategory.TotalDamageDealt:
                return PlayerPrefs.GetInt(KEY_TOTAL_DAMAGE, 0);

            case MissionCategory.StagePlayCount:
                return PlayerPrefs.GetInt(KEY_STAGE_PLAYS, 0);

            case MissionCategory.StaminaConsumed:
                return PlayerPrefs.GetInt(KEY_STAMINA_USED, 0);

            case MissionCategory.SpecificEnemyKills:
                if (entry.targetEnemyPrefab == null) return 0;
                string enemyKey = CleanPrefabName(entry.targetEnemyPrefab.name);
                return PlayerPrefs.GetInt(KEY_SPECIFIC_KILL_PREFIX + enemyKey, 0);

            default:
                return 0;
        }
    }

    private static string CleanPrefabName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return "";
        return rawName.Replace("(Clone)", "").Trim();
    }

    public static void AddEnemyKill(GameObject enemyObjectOrPrefab)
    {
        int totalKills = PlayerPrefs.GetInt(KEY_TOTAL_KILLS, 0) + 1;
        PlayerPrefs.SetInt(KEY_TOTAL_KILLS, totalKills);

        if (enemyObjectOrPrefab != null)
        {
            string enemyKey = CleanPrefabName(enemyObjectOrPrefab.name);
            int specificKills = PlayerPrefs.GetInt(KEY_SPECIFIC_KILL_PREFIX + enemyKey, 0) + 1;
            PlayerPrefs.SetInt(KEY_SPECIFIC_KILL_PREFIX + enemyKey, specificKills);
        }

        PlayerPrefs.Save();
    }

    public static void AddDamage(int damageAmount)
    {
        if (damageAmount <= 0) return;

        int totalDamage = PlayerPrefs.GetInt(KEY_TOTAL_DAMAGE, 0);
        long nextDamage = (long)totalDamage + damageAmount;
        if (nextDamage > int.MaxValue) nextDamage = int.MaxValue;

        PlayerPrefs.SetInt(KEY_TOTAL_DAMAGE, (int)nextDamage);
        PlayerPrefs.Save();
    }

    public static void AddStagePlay(int count = 1)
    {
        int stagePlays = PlayerPrefs.GetInt(KEY_STAGE_PLAYS, 0) + count;
        PlayerPrefs.SetInt(KEY_STAGE_PLAYS, stagePlays);
        PlayerPrefs.Save();
    }

    public static void AddStaminaUsed(int staminaAmount)
    {
        if (staminaAmount <= 0) return;

        int totalStamina = PlayerPrefs.GetInt(KEY_STAMINA_USED, 0) + staminaAmount;
        PlayerPrefs.SetInt(KEY_STAMINA_USED, totalStamina);
        PlayerPrefs.Save();
    }
}