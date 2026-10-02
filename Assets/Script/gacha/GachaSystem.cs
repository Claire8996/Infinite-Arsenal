using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class GachaItem
{
    public string itemName = "カプセル名";
    public GameObject capsulePrefab;
    public float probabilityWeight;
}

[System.Serializable]
public class PullLayout
{
    [Tooltip("このレイアウトを使用する連数（例：3、6、10など）")]
    public int pullCount;
    [Tooltip("その連数でカプセルが飛んでいく最終目的地のリスト")]
    public RectTransform[] targetPositions;
}

public class GachaSystem : MonoBehaviour
{
    [Header("【エメラルド設定】")]
    public int currentEmeralds = 1000;
    public int costPerPull = 300;

    [Header("【デバッグ設定】")]
    public bool isDebugModeFreePull = false;

    [Header("【ガチャボタンUI設定】")]
    public GameObject singlePullButton;
    // ★追加：1連用の矢印
    [Tooltip("1連ガチャ用の矢印UI（Image）")]
    public Image singlePullArrow;

    [Tooltip("インデックス番号＝連数として登録してください（0と1は空でOK、2に2連ボタン、10に10連ボタン）")]
    public GameObject[] multiPullButtons = new GameObject[11];
    // ★追加：複数連用の矢印
    [Tooltip("複数連用の矢印UI（ボタンと同じインデックス番号に登録）")]
    public Image[] multiPullArrows = new Image[11];

    [Header("【演出オブジェクト設定】")]
    public RectTransform handleTransform;
    public RectTransform startPos;
    public RectTransform endPos;
    public Transform initialCapsuleParent;

    [Header("【背景フェード演出設定】")]
    [Tooltip("結果表示時に後ろに表示する灰色の背景（半透明の黒など）")]
    public Image backgroundFadeImage;
    [Tooltip("背景がフェードインしきるまでの時間")]
    public float backgroundFadeTime = 0.5f;

    [Header("【一括開封ボタン設定】")]
    [Tooltip("一括開封ボタン（Canvas Groupコンポーネントを付けてください）")]
    public CanvasGroup openAllButtonCanvasGroup;
    [Tooltip("一括開封ボタンがフェードインする時間")]
    public float openAllButtonFadeTime = 0.5f;

    [Header("【ガチャ結果のレイアウト設定】")]
    public List<PullLayout> layouts;
    [Tooltip("1連ガチャの時にカプセルが飛んでいく場所")]
    public RectTransform singlePullTargetPos;

    [Header("【アニメーション設定】")]
    public float handleRotateTime = 1.5f;
    public float rollTime = 1.0f;
    public float flyTime = 0.8f;
    public Vector3 finalScale = new Vector3(3f, 3f, 3f);
    public float spawnInterval = 0.15f;

    [Header("【排出アイテム一覧（確率設定）】")]
    public List<GachaItem> gachaItems;

    private bool isPulling = false;
    private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void Start()
    {
        UpdateButtonVisibility();

        if (backgroundFadeImage != null)
        {
            Color c = backgroundFadeImage.color;
            c.a = 0f;
            backgroundFadeImage.color = c;
            backgroundFadeImage.gameObject.SetActive(false);
        }

        if (openAllButtonCanvasGroup != null)
        {
            openAllButtonCanvasGroup.alpha = 0f;
            openAllButtonCanvasGroup.interactable = false;
            openAllButtonCanvasGroup.blocksRaycasts = false;
        }
    }

    public void OnClickPullGacha(int pullCount)
    {
        if (isPulling) return;

        int totalCost = costPerPull * pullCount;

        if (!isDebugModeFreePull)
        {
            if (currentEmeralds < totalCost)
            {
                Debug.Log("エメラルドが足りません！");
                return;
            }
            currentEmeralds -= totalCost;
        }

        Debug.Log($"{pullCount}連ガチャ実行！ 残りエメラルド: {currentEmeralds}");

        UpdateButtonVisibility();

        if (backgroundFadeImage != null)
        {
            Color c = backgroundFadeImage.color;
            c.a = 0f;
            backgroundFadeImage.color = c;
            backgroundFadeImage.gameObject.SetActive(false);
        }

        if (openAllButtonCanvasGroup != null)
        {
            openAllButtonCanvasGroup.alpha = 0f;
            openAllButtonCanvasGroup.interactable = false;
            openAllButtonCanvasGroup.blocksRaycasts = false;
        }

        StartCoroutine(GachaRoutine(pullCount));
    }

    public void UpdateButtonVisibility()
    {
        if (singlePullButton != null)
        {
            bool canAfford = currentEmeralds >= costPerPull || isDebugModeFreePull;
            SetButtonState(singlePullButton, canAfford);

            // ★追加：1連用の矢印の色を変える
            if (singlePullArrow != null)
            {
                singlePullArrow.color = canAfford ? Color.white : disabledColor;
            }
        }

        int affordablePulls = Mathf.Clamp(currentEmeralds / costPerPull, 0, 10);
        int targetButtonIndex = affordablePulls >= 2 ? affordablePulls : 2;

        for (int i = 2; i <= 10; i++)
        {
            if (multiPullButtons.Length > i && multiPullButtons[i] != null)
            {
                if (i == targetButtonIndex)
                {
                    multiPullButtons[i].SetActive(true);

                    // ★追加：ボタンが表示されるなら、対応する矢印も表示する
                    if (multiPullArrows.Length > i && multiPullArrows[i] != null)
                    {
                        multiPullArrows[i].gameObject.SetActive(true);
                    }

                    bool canAfford = (currentEmeralds >= (costPerPull * i)) || isDebugModeFreePull;
                    SetButtonState(multiPullButtons[i], canAfford);

                    // ★追加：複数連用の矢印の色を変える
                    if (multiPullArrows.Length > i && multiPullArrows[i] != null)
                    {
                        multiPullArrows[i].color = canAfford ? Color.white : disabledColor;
                    }
                }
                else
                {
                    multiPullButtons[i].SetActive(false);

                    // ★追加：ボタンが非表示なら、対応する矢印も非表示にする
                    if (multiPullArrows.Length > i && multiPullArrows[i] != null)
                    {
                        multiPullArrows[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void SetButtonState(GameObject buttonObj, bool isInteractable)
    {
        Image btnImage = buttonObj.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = isInteractable ? Color.white : disabledColor;
        }

        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = isInteractable;
        }
    }

    private IEnumerator GachaRoutine(int pullCount)
    {
        isPulling = true;

        float elapsed = 0f;
        Vector3 startRot = handleTransform.localEulerAngles;
        Vector3 targetRot = startRot + new Vector3(0, 0, -1080f);

        while (elapsed < handleRotateTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / handleRotateTime;
            float easeT = t * t * (3f - 2f * t);
            handleTransform.localEulerAngles = Vector3.Lerp(startRot, targetRot, easeT);
            yield return null;
        }
        handleTransform.localEulerAngles = targetRot;

        RectTransform[] currentTargetPositions = null;

        if (pullCount == 1)
        {
            currentTargetPositions = new RectTransform[] { singlePullTargetPos };
        }
        else
        {
            foreach (var layout in layouts)
            {
                if (layout.pullCount == pullCount)
                {
                    currentTargetPositions = layout.targetPositions;
                    break;
                }
            }
        }

        List<Coroutine> capsuleCoroutines = new List<Coroutine>();

        for (int i = 0; i < pullCount; i++)
        {
            capsuleCoroutines.Add(StartCoroutine(AnimateSingleCapsule(i, currentTargetPositions, pullCount)));
            yield return new WaitForSeconds(spawnInterval);
        }

        yield return new WaitForSeconds(rollTime + flyTime);

        if (backgroundFadeImage != null)
        {
            backgroundFadeImage.gameObject.SetActive(true);
            float fadeElapsed = 0f;
            Color fadeColor = backgroundFadeImage.color;
            float targetAlpha = 0.8f;

            while (fadeElapsed < backgroundFadeTime)
            {
                fadeElapsed += Time.deltaTime;
                fadeColor.a = Mathf.Lerp(0f, targetAlpha, fadeElapsed / backgroundFadeTime);
                backgroundFadeImage.color = fadeColor;
                yield return null;
            }
            fadeColor.a = targetAlpha;
            backgroundFadeImage.color = fadeColor;
        }

        GameObject[] spawnedCapsules = GameObject.FindGameObjectsWithTag("GachaCapsule");
        foreach (GameObject cap in spawnedCapsules)
        {
            cap.transform.SetAsLastSibling();

            Button btn = cap.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = true;
            }
        }

        if (openAllButtonCanvasGroup != null)
        {
            openAllButtonCanvasGroup.transform.SetAsLastSibling();

            float btnFadeElapsed = 0f;
            while (btnFadeElapsed < openAllButtonFadeTime)
            {
                btnFadeElapsed += Time.deltaTime;
                openAllButtonCanvasGroup.alpha = Mathf.Lerp(0f, 1f, btnFadeElapsed / openAllButtonFadeTime);
                yield return null;
            }
            openAllButtonCanvasGroup.alpha = 1f;

            openAllButtonCanvasGroup.interactable = true;
            openAllButtonCanvasGroup.blocksRaycasts = true;
        }

        isPulling = false;

        StartCoroutine(WaitUntilAllCapsulesOpened());
    }

    private IEnumerator WaitUntilAllCapsulesOpened()
    {
        yield return new WaitForSeconds(0.5f);

        while (GameObject.FindGameObjectsWithTag("GachaCapsule").Length > 0)
        {
            yield return new WaitForSeconds(0.2f);
        }

        if (openAllButtonCanvasGroup != null && openAllButtonCanvasGroup.alpha > 0f)
        {
            openAllButtonCanvasGroup.interactable = false;
            openAllButtonCanvasGroup.blocksRaycasts = false;

            float btnFadeElapsed = 0f;
            while (btnFadeElapsed < openAllButtonFadeTime)
            {
                btnFadeElapsed += Time.deltaTime;
                openAllButtonCanvasGroup.alpha = Mathf.Lerp(1f, 0f, btnFadeElapsed / openAllButtonFadeTime);
                yield return null;
            }
            openAllButtonCanvasGroup.alpha = 0f;
        }

        GameObject[] resultItems = GameObject.FindGameObjectsWithTag("GachaResultItem");
        foreach (GameObject item in resultItems)
        {
            Destroy(item);
        }

        if (backgroundFadeImage != null)
        {
            float fadeElapsed = 0f;
            Color fadeColor = backgroundFadeImage.color;
            float startAlpha = fadeColor.a;

            while (fadeElapsed < backgroundFadeTime)
            {
                fadeElapsed += Time.deltaTime;
                fadeColor.a = Mathf.Lerp(startAlpha, 0f, fadeElapsed / backgroundFadeTime);
                backgroundFadeImage.color = fadeColor;
                yield return null;
            }
            fadeColor.a = 0f;
            backgroundFadeImage.color = fadeColor;
            backgroundFadeImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimateSingleCapsule(int index, RectTransform[] targetPositionsArray, int totalPulls)
    {
        GameObject selectedPrefab = DrawGacha();
        if (selectedPrefab == null) yield break;

        GameObject capsule = Instantiate(selectedPrefab, startPos.position, Quaternion.identity, initialCapsuleParent);
        RectTransform capRect = capsule.GetComponent<RectTransform>();

        float elapsed = 0f;
        Vector3 capStartRot = Vector3.zero;
        Vector3 capRollRot = new Vector3(0, 0, -720f);

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
            Transform rootCanvas = capRect.GetComponentInParent<Canvas>().transform;
            capRect.SetParent(rootCanvas);
            capRect.SetAsLastSibling();
        }

        RectTransform targetPos = endPos;

        if (targetPositionsArray != null && targetPositionsArray.Length > index && targetPositionsArray[index] != null)
        {
            targetPos = targetPositionsArray[index];
        }
        else
        {
            Debug.LogWarning($"【警告】{index + 1}個目のカプセルの飛ぶ位置が設定されていません！そのまま落下位置に留まります。");
        }

        elapsed = 0f;
        Vector3 flyStartPos = endPos.position;
        Vector3 originalScale = capRect.localScale;
        Vector3 capCurrentRot = capRect.localEulerAngles;
        Vector3 capFlyRot = capCurrentRot + new Vector3(0, 0, -360f);

        while (elapsed < flyTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyTime;
            float easeOutT = Mathf.Sin(t * Mathf.PI * 0.5f);

            if (capRect != null)
            {
                capRect.position = Vector3.Lerp(flyStartPos, targetPos.position, easeOutT);
                capRect.localScale = Vector3.Lerp(originalScale, finalScale, easeOutT);
                capRect.localEulerAngles = Vector3.Lerp(capCurrentRot, capFlyRot, easeOutT);
            }
            yield return null;
        }
    }

    private GameObject DrawGacha()
    {
        float totalWeight = 0;
        foreach (var item in gachaItems)
        {
            totalWeight += item.probabilityWeight;
        }

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