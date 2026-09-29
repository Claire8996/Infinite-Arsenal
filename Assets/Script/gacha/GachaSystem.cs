using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ガチャの排出アイテムと確率をセットにするためのクラス
[System.Serializable]
public class GachaItem
{
    public string itemName = "カプセル名";
    public GameObject capsulePrefab; // 排出されるカプセルのプレハブ
    public float probabilityWeight;  // 排出確率（重み）
}

public class GachaSystem : MonoBehaviour
{
    [Header("【エメラルド設定】")]
    public int currentEmeralds = 1000; // 現在の所持エメラルド
    public int costPerPull = 300;      // 1回引くのに必要なエメラルド

    [Header("【デバッグ設定】")]
    [Tooltip("チェックを入れるとエメラルド0でも引けます")]
    public bool isDebugModeFreePull = false;

    [Header("【演出オブジェクト設定】")]
    public RectTransform handleTransform; // ハンドルのUI
    public RectTransform startPos;        // カプセルの出現位置
    public RectTransform endPos;          // カプセルの転がり終点
    public RectTransform flyTargetPos;    // 最終的に飛んでくる画面中央などの位置
    public Transform capsuleParent;       // カプセルを生成する親（Canvasなど）

    [Header("【アニメーション時間・サイズ設定】")]
    public float handleRotateTime = 1.5f; // ハンドルが3回転する時間
    public float rollTime = 1.0f;         // 転がる時間
    public float flyTime = 0.8f;          // 飛んでくる時間
    public Vector3 finalScale = new Vector3(3f, 3f, 3f); // 飛んできた時の最終サイズ

    [Header("【排出アイテム一覧（確率設定）】")]
    public List<GachaItem> gachaItems;

    private bool isPulling = false; // ガチャ演出中かどうかのフラグ（連打防止）

    // ボタンに設定するメソッド
    public void OnClickPullGacha()
    {
        // 演出中なら何もしない（連打防止）
        if (isPulling) return;

        // エメラルドの消費チェック
        if (!isDebugModeFreePull)
        {
            if (currentEmeralds < costPerPull)
            {
                Debug.Log("エメラルドが足りません！");
                return;
            }
            currentEmeralds -= costPerPull; // 消費
        }
        else
        {
            Debug.Log("デバッグモード：無料で引きます！");
        }

        Debug.Log("ガチャ実行！ 残りエメラルド: " + currentEmeralds);

        // 演出開始
        StartCoroutine(GachaRoutine());
    }

    // ガチャの一連のアニメーション処理
    private IEnumerator GachaRoutine()
    {
        isPulling = true;

        // 【1】ハンドルを時計回りにゆっくり3回転（Z軸に -1080度）
        float elapsed = 0f;
        Vector3 startRot = handleTransform.localEulerAngles;
        Vector3 targetRot = startRot + new Vector3(0, 0, -1080f);

        while (elapsed < handleRotateTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / handleRotateTime;
            // ゆっくり回ってゆっくり止まる計算（イージング）
            float easeT = t * t * (3f - 2f * t);
            handleTransform.localEulerAngles = Vector3.Lerp(startRot, targetRot, easeT);
            yield return null; // 次のフレームまで待機
        }
        handleTransform.localEulerAngles = targetRot;

        // 【2】確率に基づいてアイテムを抽選
        GameObject selectedPrefab = DrawGacha();
        if (selectedPrefab == null)
        {
            Debug.LogError("ガチャの排出アイテムが設定されていません！");
            isPulling = false;
            yield break;
        }

        // 【3】カプセルをStartPosに生成
        GameObject capsule = Instantiate(selectedPrefab, startPos.position, Quaternion.identity, capsuleParent);
        RectTransform capRect = capsule.GetComponent<RectTransform>();

        // 【4】カプセルがStartPosからEndPosへ回転しながら転がる
        elapsed = 0f;
        Vector3 capStartRot = Vector3.zero;
        Vector3 capRollRot = new Vector3(0, 0, -720f); // 2回転しながら転がる

        while (elapsed < rollTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rollTime;
            if (capRect != null)
            {
                capRect.position = Vector3.Lerp(startPos.position, endPos.position, t);
                capRect.localEulerAngles = Vector3.Lerp(capStartRot, capRollRot, t);
            }
            yield return null;
        }

        if (capRect != null)
        {
            // カプセルをCanvasの直下に移動させ、ヒエラルキーの一番下に設定する（＝最前面になる）
            capRect.SetParent(capRect.GetComponentInParent<Canvas>().transform);
            capRect.SetAsLastSibling();
        }

        // 【5】EndPosからFlyTargetPosへ回転しながら飛び出し、徐々に大きくなる
        elapsed = 0f;
        Vector3 flyStartPos = endPos.position;
        Vector3 originalScale = capRect.localScale;
        Vector3 capCurrentRot = capRect.localEulerAngles;
        Vector3 capFlyRot = capCurrentRot + new Vector3(0, 0, -360f); // さらに1回転しながら飛ぶ

        while (elapsed < flyTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyTime;
            // 手前に飛び出してくる勢いをつける計算（イーズアウト）
            float easeOutT = Mathf.Sin(t * Mathf.PI * 0.5f);

            if (capRect != null)
            {
                capRect.position = Vector3.Lerp(flyStartPos, flyTargetPos.position, easeOutT);
                capRect.localScale = Vector3.Lerp(originalScale, finalScale, easeOutT);
                capRect.localEulerAngles = Vector3.Lerp(capCurrentRot, capFlyRot, easeOutT);
            }
            yield return null;
        }

        // 演出終了
        isPulling = false;
    }

    // 確率計算ロジック
    private GameObject DrawGacha()
    {
        float totalWeight = 0;
        foreach (var item in gachaItems)
        {
            totalWeight += item.probabilityWeight;
        }

        // 0 ～ 合計確率 の間でランダムな数値を出す
        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach (var item in gachaItems)
        {
            currentWeight += item.probabilityWeight;
            if (randomValue <= currentWeight)
            {
                return item.capsulePrefab;
            }
        }
        return null;
    }
}