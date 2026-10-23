using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinDisplay : MonoBehaviour
{
    [Header("【コインの数を表示するテキスト】")]
    [Tooltip("標準のTextコンポーネント用")]
    public Text normalText;

    [Tooltip("TextMeshProコンポーネント用（使っている方に登録してください）")]
    public TextMeshProUGUI tmpText;

    // ==========================================
    // リアルタイムで同じ数値に増える別のテキスト（＆UIが飛んでいく目標先）
    // ==========================================
    [Header("【新しく追加するコイン表示テキスト（2つ目）】")]
    [Tooltip("新しく追加する標準のTextコンポーネント用")]
    public Text secondNormalText;

    [Tooltip("新しく追加するTextMeshProコンポーネント用（UIが飛んでいく目標先にもなります）")]
    public TextMeshProUGUI secondTmpText;

    [Header("【報酬獲得時の演出設定】")]
    [Tooltip("コインの数値が増える時間（秒）※1秒かけて増えます")]
    public float countUpDuration = 1.0f;

    [Tooltip("Prefab内のUIがテキストに向けて飛んでいく時間（秒）※数値を大きくするほどゆっくり飛びます")]
    public float flyDuration = 1.2f;

    [Tooltip("チェックを入れると、UIがテキストに到着してから1秒かけて数値が増えます（外すと飛び始めると同時に増えます）")]
    public bool countUpAfterFlyArrives = false;

    [Header("【表示設定】")]
    [Tooltip("数字の前後に文字を入れたい場合（例：「× 」と入れると「× 1000」になる）")]
    public string prefix = "";
    public string suffix = "";

    [Header("セーブデータが無い場合の初期値")]
    public int defaultCoins = 1000;

    // カウントアップ演出中かどうかを判定するフラグ
    private bool isCountingUp = false;
    private int currentDisplayedValue = 0;
    private Coroutine effectCoroutine;

    // セーブデータ用のキー
    public const string SaveKeyCoins = "PlayerCoins";

    void Start()
    {
        currentDisplayedValue = PlayerPrefs.GetInt(SaveKeyCoins, defaultCoins);
        UpdateCoinDisplay();
    }

    void Update()
    {
        // カウントアップ演出中でなければ、常に最新のセーブデータ状態を両方のテキストに反映させる
        if (!isCountingUp)
        {
            UpdateCoinDisplay();
        }
    }

    public void UpdateCoinDisplay()
    {
        // カウントアップ演出中はコルーチン側でリアルタイム更新するためスキップ
        if (isCountingUp) return;

        // 保存されている「PlayerCoins」のセーブデータを読み込む
        int currentCoins = PlayerPrefs.GetInt(SaveKeyCoins, defaultCoins);
        currentDisplayedValue = currentCoins;

        SetAllTextsValue(currentCoins);
    }

    // 元のテキストと新しく追加したテキストの両方にリアルタイムで同じ数値を反映するメソッド
    private void SetAllTextsValue(int coinValue)
    {
        string displayText = prefix + coinValue.ToString() + suffix;

        // 1. 元々あるコインテキスト
        if (normalText != null)
        {
            normalText.text = displayText;
        }

        if (tmpText != null)
        {
            tmpText.text = displayText;
        }

        // 2. 新しく追加した別のコインテキスト（同じ数値をリアルタイム表示）
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
    // ミッション完了時や報酬獲得時に呼ばれる演出メソッド
    // （Prefab内のUIを新しいテキストに向けて飛ばし、1秒かけて数値を増やす）
    // ==========================================
    public void PlayMissionCoinAcquireEffect(int startCoins, int targetCoins, GameObject flyingUISource)
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            startCoins = currentDisplayedValue;
        }

        effectCoroutine = StartCoroutine(MissionCoinEffectRoutine(startCoins, targetCoins, flyingUISource));
    }

    private IEnumerator MissionCoinEffectRoutine(int startCoins, int targetCoins, GameObject flyingUISource)
    {
        isCountingUp = true;
        currentDisplayedValue = startCoins;
        SetAllTextsValue(startCoins);

        // 飛んでいく先のターゲット（新しく追加したテキストを優先し、無ければ元のテキスト）を取得
        Transform targetTransform = GetFlyTargetTransform();

        // 1. もし「飛び始めると同時に1秒かけて数値を増やす」設定なら、先にカウントアップを開始する
        Coroutine countCoroutine = null;
        if (!countUpAfterFlyArrives)
        {
            countCoroutine = StartCoroutine(CountUpRoutine(startCoins, targetCoins));
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
            yield return StartCoroutine(CountUpRoutine(startCoins, targetCoins));
        }
        else if (countCoroutine != null)
        {
            yield return countCoroutine;
        }

        // 最終的な数値を確実にセットして通常監視モードへ戻す
        int finalCoins = PlayerPrefs.GetInt(SaveKeyCoins, targetCoins);
        currentDisplayedValue = finalCoins;
        SetAllTextsValue(finalCoins);

        isCountingUp = false;
        effectCoroutine = null;
    }

    // 1秒かけてコインのテキストと新しいテキストをリアルタイムで同じ数値ずつ増やすコルーチン
    private IEnumerator CountUpRoutine(int startCoins, int targetCoins)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, countUpDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            currentDisplayedValue = Mathf.RoundToInt(Mathf.Lerp(startCoins, targetCoins, t));
            SetAllTextsValue(currentDisplayedValue);

            yield return null;
        }

        currentDisplayedValue = targetCoins;
        SetAllTextsValue(targetCoins);
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