using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // もしTextMeshProを使っている場合はこれが必要です

public class EmeraldDisplay : MonoBehaviour
{
    // ==========================================
    // ★追加：別シーンの EmeraldLinkedText と数値を紐づけるための共有データ・メソッド
    // ==========================================
    public const string EMERALD_SAVE_KEY = "PlayerEmeralds";
    private static int cachedDefaultEmeralds = 1000;
    private static bool hasInitializedDefault = false;

    /// <summary>
    /// 別シーンのスクリプトから現在のエメラルド数を安全に取得するための連動メソッド
    /// </summary>
    public static int GetCurrentEmeralds(int fallbackDefault = 1000)
    {
        int defVal = hasInitializedDefault ? cachedDefaultEmeralds : fallbackDefault;
        return PlayerPrefs.GetInt(EMERALD_SAVE_KEY, defVal);
    }

    [Header("【エメラルドの数を表示するテキスト】")]
    [Tooltip("標準のTextコンポーネント用")]
    public Text normalText;

    [Tooltip("TextMeshProコンポーネント用（使っている方に登録してください）")]
    public TextMeshProUGUI tmpText;

    // ==========================================
    // リアルタイムで同じ数値に増える別のテキスト（＆UIが飛んでいく目標先）
    // ==========================================
    [Header("【新しく追加するエメラルド表示テキスト（2つ目）】")]
    [Tooltip("新しく追加する標準のTextコンポーネント用")]
    public Text secondNormalText;

    [Tooltip("新しく追加するTextMeshProコンポーネント用（UIが飛んでいく目標先にもなります）")]
    public TextMeshProUGUI secondTmpText;

    [Header("【ミッション報酬獲得時の演出設定】")]
    [Tooltip("エメラルドの数値が増える時間（秒）※1秒かけて増えます")]
    public float countUpDuration = 1.0f;

    [Tooltip("Prefab内のUIがテキストに向けて飛んでいく時間（秒）※数値を大きくするほどゆっくり飛びます")]
    public float flyDuration = 1.2f;

    [Tooltip("チェックを入れると、UIがテキストに到着してから1秒かけて数値が増えます（外すと飛び始めると同時に増えます）")]
    public bool countUpAfterFlyArrives = false;

    [Header("【表示設定】")]
    [Tooltip("数字の前後に文字を入れたい場合（例：「× 」と入れると「× 1000」になる）")]
    public string prefix = "";
    public string suffix = "";

    // 万が一セーブデータがまだ無い時のために、GachaSystemで設定した初期値をここにも入れておく
    [Header("セーブデータが無い場合の初期値")]
    public int defaultEmeralds = 1000;

    // カウントアップ演出中かどうかを判定するフラグ
    private bool isCountingUp = false;
    private int currentDisplayedValue = 0;
    private Coroutine effectCoroutine;

    void Awake()
    {
        // 初期値を別シーンにも共有できるよう記録
        cachedDefaultEmeralds = defaultEmeralds;
        hasInitializedDefault = true;
    }

    void Start()
    {
        currentDisplayedValue = PlayerPrefs.GetInt(EMERALD_SAVE_KEY, defaultEmeralds);
        UpdateEmeraldDisplay();
    }

    // もしこのシーンを開いたままエメラルドが増減することがある場合は、
    // Updateで常に監視するか、このメソッドを外部から呼び出してください。
    void Update()
    {
        // カウントアップ演出中でなければ、常に最新のセーブデータ状態を両方のテキストに反映させる
        if (!isCountingUp)
        {
            UpdateEmeraldDisplay();
        }
    }

    public void UpdateEmeraldDisplay()
    {
        // カウントアップ演出中はコルーチン側でリアルタイム更新するためスキップ
        if (isCountingUp) return;

        // GachaSystemで保存している「PlayerEmeralds」のセーブデータを読み込む
        int currentEmeralds = PlayerPrefs.GetInt(EMERALD_SAVE_KEY, defaultEmeralds);
        currentDisplayedValue = currentEmeralds;

        SetAllTextsValue(currentEmeralds);
    }

    // 元のテキストと新しく追加したテキストの両方にリアルタイムで同じ数値を反映するメソッド
    private void SetAllTextsValue(int emeraldValue)
    {
        string displayText = prefix + emeraldValue.ToString() + suffix;

        // 1. 元々あるエメラルドテキスト
        if (normalText != null)
        {
            normalText.text = displayText;
        }

        if (tmpText != null)
        {
            tmpText.text = displayText;
        }

        // 2. 新しく追加した別のエメラルドテキスト（同じ数値をリアルタイム表示）
        if (secondNormalText != null)
        {
            secondNormalText.text = displayText;
        }

        if (secondTmpText != null)
        {
            secondTmpText.text = displayText;
        }
    }

    // ==========================================
    // ミッション完了ボタンを押した時に呼ばれる演出メソッド
    // （Prefab内のUIを新しいテキストに向けて飛ばし、1秒かけて数値を増やす）
    // ==========================================
    public void PlayMissionEmeraldAcquireEffect(int startEmeralds, int targetEmeralds, GameObject flyingUISource)
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            startEmeralds = currentDisplayedValue;
        }

        effectCoroutine = StartCoroutine(MissionEmeraldEffectRoutine(startEmeralds, targetEmeralds, flyingUISource));
    }

    private IEnumerator MissionEmeraldEffectRoutine(int startEmeralds, int targetEmeralds, GameObject flyingUISource)
    {
        isCountingUp = true;
        currentDisplayedValue = startEmeralds;
        SetAllTextsValue(startEmeralds);

        // 飛んでいく先のターゲット（新しく追加したテキストを優先し、無ければ元のテキスト）を取得
        Transform targetTransform = GetFlyTargetTransform();

        // 1. もし「飛び始めると同時に1秒かけて数値を増やす」設定なら、先にカウントアップを開始する
        Coroutine countCoroutine = null;
        if (!countUpAfterFlyArrives)
        {
            countCoroutine = StartCoroutine(CountUpRoutine(startEmeralds, targetEmeralds));
        }

        // 2. Prefab内のとあるUIをテキストに向けてゆっくり飛ばす処理
        if (flyingUISource != null && targetTransform != null)
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = FindObjectOfType<Canvas>();
            }

            Transform flyParent = (rootCanvas != null) ? rootCanvas.transform : this.transform;

            // ボタン削除やリスト並び替えの影響を受けないよう、Canvas最前面にクローンを生成して飛ばす
            GameObject flyingClone = Instantiate(flyingUISource, flyParent);
            flyingClone.SetActive(true);
            flyingClone.transform.position = flyingUISource.transform.position;
            flyingClone.transform.localScale = flyingUISource.transform.lossyScale;
            flyingClone.transform.SetAsLastSibling();

            // 元のPrefab内のUIは飛んでいったように見せるため非表示にする
            flyingUISource.SetActive(false);

            // 飛んでいくUIがボタン操作の邪魔をしないようにRaycastを無効化
            Graphic[] cloneGraphics = flyingClone.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in cloneGraphics)
            {
                g.raycastTarget = false;
            }

            Vector3 startPos = flyingClone.transform.position;
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, flyDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // ゆったりと自然に移動するイージング（SmoothStep）
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (flyingClone != null && targetTransform != null)
                {
                    flyingClone.transform.position = Vector3.Lerp(startPos, targetTransform.position, smoothT);
                }

                yield return null;
            }

            if (flyingClone != null)
            {
                Destroy(flyingClone);
            }
        }

        // 3. もし「UIが到着してから1秒かけて数値を増やす」設定なら、ここでカウントアップを実行する
        if (countUpAfterFlyArrives)
        {
            yield return StartCoroutine(CountUpRoutine(startEmeralds, targetEmeralds));
        }
        else if (countCoroutine != null)
        {
            yield return countCoroutine;
        }

        // 最終的な数値を確実にセットして通常監視モードへ戻す
        int finalEmeralds = PlayerPrefs.GetInt(EMERALD_SAVE_KEY, targetEmeralds);
        currentDisplayedValue = finalEmeralds;
        SetAllTextsValue(finalEmeralds);

        isCountingUp = false;
        effectCoroutine = null;
    }

    // 1秒かけてエメラルドのテキストと新しいテキストをリアルタイムで同じ数値ずつ増やすコルーチン
    private IEnumerator CountUpRoutine(int startEmeralds, int targetEmeralds)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, countUpDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            currentDisplayedValue = Mathf.RoundToInt(Mathf.Lerp(startEmeralds, targetEmeralds, t));
            SetAllTextsValue(currentDisplayedValue);

            yield return null;
        }

        currentDisplayedValue = targetEmeralds;
        SetAllTextsValue(targetEmeralds);
    }

    // UIが飛んでいく目標先のテキストTransformを取得する補助メソッド
    private Transform GetFlyTargetTransform()
    {
        if (secondTmpText != null) return secondTmpText.transform;
        if (secondNormalText != null) return secondNormalText.transform;
        if (tmpText != null) return tmpText.transform;
        if (normalText != null) return normalText.transform;
        return this.transform;
    }
}