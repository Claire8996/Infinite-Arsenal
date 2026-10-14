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
    [Tooltip("チェックを入れると、このカプセルは排出されなくなります（確率0%）")]
    public bool isProbabilityZero;
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
    [Tooltip("Inspectorでの初期値（※ゲーム内で一度でも保存された後は、この数値よりセーブデータが優先されます）")]
    public int initialEmeralds = 1000; // 💡 名前を変更し、あくまで「初期値」として扱います

    // 💡 実際の現在のエメラルド数（Inspectorから直接いじらないように隠します）
    [HideInInspector]
    public int currentEmeralds;

    public int costPerPull = 300;

    [Header("【デバッグ設定】")]
    public bool isDebugModeFreePull = false;

    // ==========================================
    // ★追加：武器ガチャ／防具ガチャ切り替えボタン設定
    // ==========================================
    [Header("【武器・防具ガチャ切り替えボタン設定】")]
    [Tooltip("武器ガチャに切り替えるボタン（Image）")]
    public Image weaponGachaButtonImage;
    [Tooltip("防具ガチャに切り替えるボタン（Image）")]
    public Image armorGachaButtonImage;
    [Tooltip("選択している方のボタンの色（通常の色）")]
    public Color selectedTypeColor = Color.white;
    [Tooltip("選択していない方のボタンの色（灰色）")]
    public Color unselectedTypeColor = Color.gray;

    // 現在防具ガチャかどうかを判定するフラグ（false = 武器ガチャ, true = 防具ガチャ）
    private bool isArmorGacha = false;

    [Header("【ガチャボタンUI設定】")]
    public GameObject singlePullButton;
    [Tooltip("1連ガチャ用の矢印UI（Image）")]
    public Image singlePullArrow;

    [Tooltip("インデックス番号＝連数として登録してください（0と1は空でOK、2に2連ボタン、10に10連ボタン）")]
    public GameObject[] multiPullButtons = new GameObject[11];
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

    // ==========================================
    // ★変更・追加：武器ガチャ用と防具ガチャ用の排出リスト
    // （※既存のインスペクター設定が消えないよう、武器側は gachaItems の名前にしています）
    // ==========================================
    [Header("【武器ガチャの排出アイテム一覧（確率設定）】")]
    public List<GachaItem> gachaItems;

    [Header("【防具ガチャの排出アイテム一覧（確率設定）】")]
    public List<GachaItem> armorGachaItems;

    private bool isPulling = false;
    private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void Start()
    {
        // ==========================================
        // 💡 追加：エメラルドのセーブデータを読み込む
        // （セーブデータが無ければ、initialEmeralds を初期値にする）
        // ==========================================
        currentEmeralds = PlayerPrefs.GetInt("PlayerEmeralds", initialEmeralds);

        // ==========================================
        // ★追加：Sceneに来た時は必ず「武器ガチャ」の方から始まるようにする
        // ==========================================
        OnClickSelectWeaponGacha();

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

    // ==========================================
    // ★追加：武器ガチャに切り替えるボタン用メソッド
    // ==========================================
    public void OnClickSelectWeaponGacha()
    {
        if (isPulling) return; // ガチャ演出中は切り替え不可

        isArmorGacha = false;
        UpdateGachaTypeButtonColors();
        Debug.Log("武器ガチャに切り替えました！");
    }

    // ==========================================
    // ★追加：防具ガチャに切り替えるボタン用メソッド
    // ==========================================
    public void OnClickSelectArmorGacha()
    {
        if (isPulling) return; // ガチャ演出中は切り替え不可

        isArmorGacha = true;
        UpdateGachaTypeButtonColors();
        Debug.Log("防具ガチャに切り替えました！");
    }

    // ==========================================
    // ★追加：選択中の方を通常の色、選択していない方を灰色にする処理
    // ==========================================
    private void UpdateGachaTypeButtonColors()
    {
        if (weaponGachaButtonImage != null)
        {
            weaponGachaButtonImage.color = !isArmorGacha ? selectedTypeColor : unselectedTypeColor;
        }

        if (armorGachaButtonImage != null)
        {
            armorGachaButtonImage.color = isArmorGacha ? selectedTypeColor : unselectedTypeColor;
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

            // ==========================================
            // 💡 追加：エメラルドを減らしたらすぐにセーブする
            // ==========================================
            currentEmeralds -= totalCost;
            PlayerPrefs.SetInt("PlayerEmeralds", currentEmeralds);
            PlayerPrefs.Save();
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

                    if (multiPullArrows.Length > i && multiPullArrows[i] != null)
                    {
                        multiPullArrows[i].gameObject.SetActive(true);
                    }

                    bool canAfford = (currentEmeralds >= (costPerPull * i)) || isDebugModeFreePull;
                    SetButtonState(multiPullButtons[i], canAfford);

                    if (multiPullArrows.Length > i && multiPullArrows[i] != null)
                    {
                        multiPullArrows[i].color = canAfford ? Color.white : disabledColor;
                    }
                }
                else
                {
                    multiPullButtons[i].SetActive(false);

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

    // ==========================================
    // ★変更：現在選択中のガチャ（武器 or 防具）のリストから抽選する処理
    // ==========================================
    private GameObject DrawGacha()
    {
        // 防具ガチャモードなら armorGachaItems、武器ガチャモードなら gachaItems を使用する
        List<GachaItem> targetGachaList = isArmorGacha ? armorGachaItems : gachaItems;

        if (targetGachaList == null || targetGachaList.Count == 0)
        {
            Debug.LogWarning(isArmorGacha ? "防具ガチャの排出リストが空です！" : "武器ガチャの排出リストが空です！");
            return null;
        }

        float totalWeight = 0;
        foreach (var item in targetGachaList)
        {
            if (!item.isProbabilityZero)
            {
                totalWeight += item.probabilityWeight;
            }
        }

        if (totalWeight <= 0f) return null;

        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach (var item in targetGachaList)
        {
            if (item.isProbabilityZero) continue;

            currentWeight += item.probabilityWeight;
            if (randomValue <= currentWeight)
            {
                return item.capsulePrefab;
            }
        }
        return null;
    }
}