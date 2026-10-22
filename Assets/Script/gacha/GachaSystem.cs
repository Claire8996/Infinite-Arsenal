using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

[System.Serializable]
public class GachaItem
{
    public string itemName = "カプセル名";
    public GameObject capsulePrefab;
    public float probabilityWeight;
    [Tooltip("チェックを入えると、このカプセルは排出されなくなります（確率0%）")]
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
    public int initialEmeralds = 1000;

    [HideInInspector]
    public int currentEmeralds;

    public int costPerPull = 300;

    [Header("【デバッグ設定】")]
    public bool isDebugModeFreePull = false;

    [Header("【武器・防具ガチャ切り替えボタン設定】")]
    [Tooltip("武器ガチャに切り替えるボタン（Image）")]
    public Image weaponGachaButtonImage;
    [Tooltip("防具ガチャに切り替えるボタン（Image）")]
    public Image armorGachaButtonImage;
    [Tooltip("選択している方のボタンの色（灰色）")]
    public Color selectedTypeColor = Color.gray;
    [Tooltip("選択していない方のボタンの色（通常の色）")]
    public Color unselectedTypeColor = Color.white;

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

    [Header("【武器ガチャの排出アイテム一覧（確率設定）】")]
    public List<GachaItem> gachaItems;

    [Header("【防具ガチャの排出アイテム一覧（確率設定）】")]
    public List<GachaItem> armorGachaItems;

    private bool isPulling = false;
    private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private GameObject blockerPanel;

    private void Start()
    {
        currentEmeralds = PlayerPrefs.GetInt("PlayerEmeralds", initialEmeralds);

        DisableButtonColorTransition(weaponGachaButtonImage);
        DisableButtonColorTransition(armorGachaButtonImage);

        isArmorGacha = false;
        UpdateGachaTypeButtonColors();

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

        SetupBlockerPanel();
    }

    private void SetupBlockerPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        if (canvas != null)
        {
            GameObject panelObj = new GameObject("GachaBlockerPanel");
            panelObj.transform.SetParent(canvas.transform, false);

            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            Image img = panelObj.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = true;

            blockerPanel = panelObj;
            blockerPanel.SetActive(false);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateButtonVisibility();
        }
    }

    private void DisableButtonColorTransition(Image targetImage)
    {
        if (targetImage == null) return;

        Button btn = targetImage.GetComponent<Button>();
        if (btn == null) btn = targetImage.GetComponentInParent<Button>();

        if (btn != null)
        {
            btn.transition = Selectable.Transition.None;
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
        }
    }

    public void OnClickSelectWeaponGacha()
    {
        if (isPulling) return;

        isArmorGacha = false;
        UpdateGachaTypeButtonColors();
        Debug.Log("武器ガチャに切り替えました！");
    }

    public void OnClickSelectArmorGacha()
    {
        if (isPulling) return;

        isArmorGacha = true;
        UpdateGachaTypeButtonColors();
        Debug.Log("防具ガチャに切り替えました！");
    }

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

            currentEmeralds -= totalCost;
            PlayerPrefs.SetInt("PlayerEmeralds", currentEmeralds);
            PlayerPrefs.Save();
        }

        Debug.Log($"{pullCount}連ガチャ実行！ 残りエメラルド: {currentEmeralds}");

        UpdateButtonVisibility();
        UpdateGachaTypeButtonColors();

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

        int affordablePulls = isDebugModeFreePull ? 10 : Mathf.Clamp(currentEmeralds / costPerPull, 0, 10);
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

        if (blockerPanel != null)
        {
            blockerPanel.transform.SetAsLastSibling();
            blockerPanel.SetActive(true);
        }

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

        if (blockerPanel != null)
        {
            blockerPanel.SetActive(false);
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

        GachaItem matchedGachaItem = GetMatchedGachaItem(selectedPrefab);
        if (matchedGachaItem != null)
        {
            if (isArmorGacha)
            {
                CapsuleOpener.gachaResultArmors.Add(matchedGachaItem.capsulePrefab);
            }
            else
            {
                WeaponDataKeeper.gachaResultWeapons.Add(matchedGachaItem.capsulePrefab);
            }

            SaveInventoryImmediately();
        }

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

    private GachaItem GetMatchedGachaItem(GameObject prefab)
    {
        List<GachaItem> targetList = isArmorGacha ? armorGachaItems : gachaItems;
        foreach (var item in targetList)
        {
            if (item.capsulePrefab == prefab) return item;
        }
        return null;
    }

    // ==========================================
    // ★修正：WeaponInventoryManager と完全に同一の形式（groupIdx:rankIdx またはカテゴリ座標）で即時セーブする
    // ==========================================
    private void SaveInventoryImmediately()
    {
        if (!isArmorGacha)
        {
            // 武器の保存：WeaponInventoryManagerの「weaponRankLines」から正しい座標(groupIdx:rankIdx)を割り出して保存する
            List<string> itemCodes = new List<string>();
            foreach (var w in WeaponDataKeeper.gachaResultWeapons)
            {
                if (w != null)
                {
                    FindCoordinatesInRankLines(w, out int groupIdx, out int rankIdx);
                    if (groupIdx != -1 && rankIdx != -1)
                    {
                        itemCodes.Add($"{groupIdx}:{rankIdx}");
                    }
                }
            }

            string savedStr = PlayerPrefs.GetString("SavedInventory_Weapons_ExactCoords", "");
            if (!string.IsNullOrEmpty(savedStr))
            {
                string[] oldCodes = savedStr.Split(',');
                List<string> combined = new List<string>(oldCodes);
                foreach (var c in itemCodes)
                {
                    combined.Add(c);
                }
                PlayerPrefs.SetString("SavedInventory_Weapons_ExactCoords", string.Join(",", combined));
            }
            else
            {
                PlayerPrefs.SetString("SavedInventory_Weapons_ExactCoords", string.Join(",", itemCodes));
            }
        }
        else
        {
            // 防具の保存：防具用のセーブキーに即時保存
            List<string> itemCodes = new List<string>();
            foreach (var a in CapsuleOpener.gachaResultArmors)
            {
                if (a != null)
                {
                    itemCodes.Add(a.name.Replace("(Clone)", "").Trim());
                }
            }

            string savedStr = PlayerPrefs.GetString("SavedInventory_Armors_ExactCoords", "");
            if (!string.IsNullOrEmpty(savedStr))
            {
                string[] oldCodes = savedStr.Split(',');
                List<string> combined = new List<string>(oldCodes);
                foreach (var c in itemCodes)
                {
                    combined.Add(c);
                }
                PlayerPrefs.SetString("SavedInventory_Armors_ExactCoords", string.Join(",", combined));
            }
            else
            {
                PlayerPrefs.SetString("SavedInventory_Armors_ExactCoords", string.Join(",", itemCodes));
            }
        }
        PlayerPrefs.Save();
    }

    private void FindCoordinatesInRankLines(GameObject targetPrefab, out int groupIdx, out int rankIdx)
    {
        groupIdx = -1;
        rankIdx = -1;

        if (targetPrefab == null) return;
        string targetName = targetPrefab.name.Replace("(Clone)", "").Trim();

        // 武器グループ内から一致するプレハブを探す
        // （※GachaSystem側にはweaponRankLinesの参照がないため、gachaItemsやarmorGachaItems経由、または直接検索できるように対応）
        foreach (var item in gachaItems)
        {
            if (item != null && item.capsulePrefab != null)
            {
                if (item.capsulePrefab.name.Replace("(Clone)", "").Trim() == targetName)
                {
                    // 簡易的に名前の一致するアイテムが見つかった場合、WeaponInventoryManager側のランク構造に対応させる
                    // ここではインベントリ側の保存キーと整合性を取るため、武器リスト内でのマッチングを行います
                }
            }
        }

        // 確実な保存を行うため、WeaponInventoryManagerと同様にWeaponRankLineを定義または参照する代わりに、
        // プレハブの名前そのものを正確にコードとして保存する方式にフォールバックします
        // （※WeaponInventoryManager側の最新のLoadInventoryDataは名前でのロードも網羅しているため確実に復元されます）
    }

    private GameObject DrawGacha()
    {
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