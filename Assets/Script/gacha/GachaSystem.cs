using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

    // 別シーンの WeaponInventoryManager の座標変換データを保持するためのキャッシュ
    [SerializeField, HideInInspector]
    private WeaponRankLine[] cachedWeaponRankLines;

    [SerializeField, HideInInspector]
    private ContentCategoryButton[] cachedCategoryButtons;

    [SerializeField, HideInInspector]
    private GameObject[] cachedInitialWeapons;

    [SerializeField, HideInInspector]
    private GameObject[] cachedInitialArmors;

    private bool isPulling = false;
    private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private GameObject blockerPanel;

    private const string SAVE_KEY_WEAPONS_COORDS = "SavedInventory_Weapons_ExactCoords";
    private const string SAVE_KEY_ARMORS_COORDS = "SavedInventory_Armors_ExactCoords";

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

        // 起動時にインベントリのマスター座標データ（weaponRankLines / categoryButtons）を準備する
        StartCoroutine(InitializeInventoryReferenceData());
    }

    private void Update()
    {
        // カプセルが開封されて WeaponDataKeeper や CapsuleOpener に武器・防具が入った瞬間を監視し、即座に永続セーブする
        if ((WeaponDataKeeper.gachaResultWeapons != null && WeaponDataKeeper.gachaResultWeapons.Count > 0) ||
            (CapsuleOpener.gachaResultArmors != null && CapsuleOpener.gachaResultArmors.Count > 0))
        {
            SaveInventoryImmediately();
        }
    }

    private void OnDisable()
    {
        // カプセルを開ける前にゲームを終了したりシーンを移動した場合でも、残っているカプセルの中身を回収してセーブする
        FlushUnopenedCapsulesAndSave();
    }

    private void OnApplicationQuit()
    {
        FlushUnopenedCapsulesAndSave();
    }

    private void FlushUnopenedCapsulesAndSave()
    {
        GameObject[] remainingCapsules = GameObject.FindGameObjectsWithTag("GachaCapsule");
        if (remainingCapsules != null && remainingCapsules.Length > 0)
        {
            foreach (GameObject cap in remainingCapsules)
            {
                if (cap == null) continue;

                ExtractItemFromUnopenedCapsule(cap);
            }
        }

        if ((WeaponDataKeeper.gachaResultWeapons != null && WeaponDataKeeper.gachaResultWeapons.Count > 0) ||
            (CapsuleOpener.gachaResultArmors != null && CapsuleOpener.gachaResultArmors.Count > 0))
        {
            SaveInventoryImmediately();
        }
    }

    private void ExtractItemFromUnopenedCapsule(GameObject capsuleObj)
    {
        if (capsuleObj == null) return;

        int beforeWeapons = WeaponDataKeeper.gachaResultWeapons != null ? WeaponDataKeeper.gachaResultWeapons.Count : 0;
        int beforeArmors = CapsuleOpener.gachaResultArmors != null ? CapsuleOpener.gachaResultArmors.Count : 0;

        // ButtonのonClickに開封メソッドが登録されていれば呼び出す
        Button btn = capsuleObj.GetComponent<Button>();
        if (btn != null && btn.onClick != null)
        {
            btn.onClick.Invoke();
        }

        int afterWeapons = WeaponDataKeeper.gachaResultWeapons != null ? WeaponDataKeeper.gachaResultWeapons.Count : 0;
        int afterArmors = CapsuleOpener.gachaResultArmors != null ? CapsuleOpener.gachaResultArmors.Count : 0;

        if (afterWeapons > beforeWeapons || afterArmors > beforeArmors)
        {
            return;
        }

        // onClickで追加されなかった場合、CapsuleOpener等のコンポーネントのフィールドから中身のPrefab参照を取得する
        MonoBehaviour[] scripts = capsuleObj.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour script in scripts)
        {
            if (script == null) continue;
            System.Type type = script.GetType();
            if (type == typeof(Button) || type == typeof(Image) || type == typeof(GachaSystem)) continue;

            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var f in fields)
            {
                if (f.FieldType == typeof(GameObject))
                {
                    GameObject val = f.GetValue(script) as GameObject;
                    if (val != null && IsRegisteredInventoryPrefab(val, out bool isArmor))
                    {
                        if (isArmor) CapsuleOpener.gachaResultArmors.Add(val);
                        else WeaponDataKeeper.gachaResultWeapons.Add(val);
                        return;
                    }
                }
                else if (typeof(IList<GameObject>).IsAssignableFrom(f.FieldType))
                {
                    IList<GameObject> list = f.GetValue(script) as IList<GameObject>;
                    if (list != null && list.Count > 0)
                    {
                        List<GameObject> validItems = new List<GameObject>();
                        bool listIsArmor = false;
                        foreach (var item in list)
                        {
                            if (item != null && IsRegisteredInventoryPrefab(item, out bool isArmor))
                            {
                                validItems.Add(item);
                                listIsArmor = isArmor;
                            }
                        }
                        if (validItems.Count > 0)
                        {
                            GameObject chosen = validItems[Random.Range(0, validItems.Count)];
                            if (listIsArmor) CapsuleOpener.gachaResultArmors.Add(chosen);
                            else WeaponDataKeeper.gachaResultWeapons.Add(chosen);
                            return;
                        }
                    }
                }
            }
        }
    }

    private bool IsRegisteredInventoryPrefab(GameObject prefab, out bool isArmor)
    {
        isArmor = false;
        if (prefab == null) return false;

        FindCoordinatesInRankLines(prefab, out int gIdx, out int rIdx);
        if (gIdx != -1 && rIdx != -1)
        {
            isArmor = false;
            return true;
        }

        FindCoordinatesInCategories(prefab, out int cIdx, out int iIdx);
        if (cIdx != -1 && iIdx != -1)
        {
            isArmor = true;
            return true;
        }

        return false;
    }

    private IEnumerator InitializeInventoryReferenceData()
    {
        // 1. 同じシーン内に WeaponInventoryManager がある場合はそこから取得
        WeaponInventoryManager invManager = FindObjectOfType<WeaponInventoryManager>(true);
        if (invManager != null)
        {
            CacheDataFromManager(invManager);
            yield break;
        }

        // 2. 既に有効なキャッシュがあれば完了
        if (HasValidCachedData())
        {
            yield break;
        }

        // 3. 別シーン（Weapon SelectScene 等）をバックグラウンドで一時的に読み込んでマスターデータを取得する
#if UNITY_EDITOR
        string[] sceneGuids = UnityEditor.AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in sceneGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            if (path == SceneManager.GetActiveScene().path) continue;

            if (path.IndexOf("Select", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("Inventory", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                yield return StartCoroutine(LoadSceneAndExtractDataEditor(path));
                if (HasValidCachedData()) yield break;
            }
        }
#endif

        // Build Settings 内のシーンから検索（ビルド時用）
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path) || path == SceneManager.GetActiveScene().path) continue;

            if (path.IndexOf("Select", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("Inventory", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(i, LoadSceneMode.Additive);
                if (asyncLoad != null)
                {
                    yield return asyncLoad;
                    Scene loadedScene = SceneManager.GetSceneByBuildIndex(i);
                    if (loadedScene.IsValid() && loadedScene.isLoaded)
                    {
                        ExtractAndDeactivateScene(loadedScene);
                        yield return SceneManager.UnloadSceneAsync(loadedScene);
                    }
                    if (HasValidCachedData()) yield break;
                }
            }
        }
    }

#if UNITY_EDITOR
    private IEnumerator LoadSceneAndExtractDataEditor(string scenePath)
    {
        var parameters = new LoadSceneParameters(LoadSceneMode.Additive);
        AsyncOperation asyncLoad = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, parameters);
        if (asyncLoad != null)
        {
            yield return asyncLoad;
            Scene loadedScene = SceneManager.GetSceneByPath(scenePath);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                ExtractAndDeactivateScene(loadedScene);
                yield return SceneManager.UnloadSceneAsync(loadedScene);
            }
        }
    }
#endif

    private void ExtractAndDeactivateScene(Scene scene)
    {
        GameObject[] rootObjs = scene.GetRootGameObjects();
        foreach (GameObject root in rootObjs)
        {
            if (root == null) continue;

            root.SetActive(false);

            WeaponInventoryManager manager = root.GetComponentInChildren<WeaponInventoryManager>(true);
            if (manager != null)
            {
                CacheDataFromManager(manager);
            }
        }
    }

    private void CacheDataFromManager(WeaponInventoryManager manager)
    {
        if (manager == null) return;

        if (manager.weaponRankLines != null && manager.weaponRankLines.Length > 0)
        {
            cachedWeaponRankLines = manager.weaponRankLines;
        }

        if (manager.categoryButtons != null && manager.categoryButtons.Length > 0)
        {
            cachedCategoryButtons = manager.categoryButtons;
        }

        if (manager.testWeaponPrefabs != null && manager.testWeaponPrefabs.Length > 0)
        {
            cachedInitialWeapons = manager.testWeaponPrefabs;
        }

        if (manager.armorPrefabs != null && manager.armorPrefabs.Length > 0)
        {
            cachedInitialArmors = manager.armorPrefabs;
        }
    }

    private bool HasValidCachedData()
    {
        bool hasWeapons = cachedWeaponRankLines != null && cachedWeaponRankLines.Length > 0;
        bool hasArmors = cachedCategoryButtons != null && cachedCategoryButtons.Length > 0;
        return hasWeapons || hasArmors;
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
#if UNITY_EDITOR
        else
        {
            WeaponInventoryManager invManager = FindObjectOfType<WeaponInventoryManager>(true);
            if (invManager != null)
            {
                CacheDataFromManager(invManager);
            }
        }
#endif
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

        // カプセルがすべて開けられた時点で確実にセーブを実行する
        SaveInventoryImmediately();

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

        // もし capsulePrefab 自体が直接武器・防具Prefabとして登録されている場合はその場で追加する
        if (IsRegisteredInventoryPrefab(selectedPrefab, out bool isDirectArmor))
        {
            if (isDirectArmor)
            {
                CapsuleOpener.gachaResultArmors.Add(selectedPrefab);
            }
            else
            {
                WeaponDataKeeper.gachaResultWeapons.Add(selectedPrefab);
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

    // ==========================================
    // WeaponInventoryManager と完全に同一の形式（groupIdx:rankIdx / categoryIdx:itemIdx）で即時セーブする
    // ==========================================
    private void SaveInventoryImmediately()
    {
        if (!HasValidCachedData()) return;

        bool isSaved = false;

        // 1. 武器の保存
        if (WeaponDataKeeper.gachaResultWeapons != null && WeaponDataKeeper.gachaResultWeapons.Count > 0)
        {
            List<string> validCodes = GetExistingValidCodes(SAVE_KEY_WEAPONS_COORDS, false);
            bool weaponSaved = false;

            for (int i = WeaponDataKeeper.gachaResultWeapons.Count - 1; i >= 0; i--)
            {
                GameObject w = WeaponDataKeeper.gachaResultWeapons[i];
                if (w != null)
                {
                    FindCoordinatesInRankLines(w, out int groupIdx, out int rankIdx);
                    if (groupIdx != -1 && rankIdx != -1)
                    {
                        validCodes.Add($"{groupIdx}:{rankIdx}");
                        WeaponDataKeeper.gachaResultWeapons.RemoveAt(i);
                        weaponSaved = true;
                        isSaved = true;
                    }
                }
                else
                {
                    WeaponDataKeeper.gachaResultWeapons.RemoveAt(i);
                }
            }

            if (weaponSaved)
            {
                PlayerPrefs.SetString(SAVE_KEY_WEAPONS_COORDS, string.Join(",", validCodes));
            }
        }

        // 2. 防具の保存
        if (CapsuleOpener.gachaResultArmors != null && CapsuleOpener.gachaResultArmors.Count > 0)
        {
            List<string> validCodes = GetExistingValidCodes(SAVE_KEY_ARMORS_COORDS, true);
            bool armorSaved = false;

            for (int i = CapsuleOpener.gachaResultArmors.Count - 1; i >= 0; i--)
            {
                GameObject a = CapsuleOpener.gachaResultArmors[i];
                if (a != null)
                {
                    FindCoordinatesInCategories(a, out int categoryIdx, out int itemIdx);
                    if (categoryIdx != -1 && itemIdx != -1)
                    {
                        validCodes.Add($"{categoryIdx}:{itemIdx}");
                        CapsuleOpener.gachaResultArmors.RemoveAt(i);
                        armorSaved = true;
                        isSaved = true;
                    }
                }
                else
                {
                    CapsuleOpener.gachaResultArmors.RemoveAt(i);
                }
            }

            if (armorSaved)
            {
                PlayerPrefs.SetString(SAVE_KEY_ARMORS_COORDS, string.Join(",", validCodes));
            }
        }

        if (isSaved)
        {
            PlayerPrefs.Save();
        }
    }

    private List<string> GetExistingValidCodes(string saveKey, bool isArmor)
    {
        List<string> validCodes = new List<string>();

        if (PlayerPrefs.HasKey(saveKey))
        {
            string savedStr = PlayerPrefs.GetString(saveKey, "");
            if (!string.IsNullOrEmpty(savedStr))
            {
                string[] parts = savedStr.Split(',');
                foreach (string p in parts)
                {
                    string[] kv = p.Split(':');
                    if (kv.Length == 2 && int.TryParse(kv[0], out _) && int.TryParse(kv[1], out _))
                    {
                        validCodes.Add(p);
                    }
                }
            }
        }
        else
        {
            if (!isArmor && cachedInitialWeapons != null)
            {
                foreach (var w in cachedInitialWeapons)
                {
                    if (w != null)
                    {
                        FindCoordinatesInRankLines(w, out int gIdx, out int rIdx);
                        if (gIdx != -1 && rIdx != -1) validCodes.Add($"{gIdx}:{rIdx}");
                    }
                }
            }
            else if (isArmor && cachedInitialArmors != null)
            {
                foreach (var a in cachedInitialArmors)
                {
                    if (a != null)
                    {
                        FindCoordinatesInCategories(a, out int cIdx, out int iIdx);
                        if (cIdx != -1 && iIdx != -1) validCodes.Add($"{cIdx}:{iIdx}");
                    }
                }
            }
        }

        return validCodes;
    }

    // ★修正：名前による比較を行わず、Prefabの直接参照（==）のみで厳密に座標を特定する
    private void FindCoordinatesInRankLines(GameObject targetPrefab, out int groupIdx, out int rankIdx)
    {
        groupIdx = -1;
        rankIdx = -1;

        if (cachedWeaponRankLines == null || targetPrefab == null) return;

        for (int g = 0; g < cachedWeaponRankLines.Length; g++)
        {
            var line = cachedWeaponRankLines[g];
            if (line != null && line.ranks != null)
            {
                for (int r = 0; r < line.ranks.Length; r++)
                {
                    if (line.ranks[r] == targetPrefab)
                    {
                        groupIdx = g;
                        rankIdx = r;
                        return;
                    }
                }
            }
        }
    }

    // ★修正：名前による比較を行わず、Prefabの直接参照（==）のみで厳密に座標を特定する
    private void FindCoordinatesInCategories(GameObject targetPrefab, out int categoryIdx, out int itemIdx)
    {
        categoryIdx = -1;
        itemIdx = -1;

        if (cachedCategoryButtons == null || targetPrefab == null) return;

        for (int c = 0; c < cachedCategoryButtons.Length; c++)
        {
            var cat = cachedCategoryButtons[c];
            if (cat != null && cat.displayTargetPrefabs != null)
            {
                for (int d = 0; d < cat.displayTargetPrefabs.Length; d++)
                {
                    var elem = cat.displayTargetPrefabs[d];
                    if (elem != null && elem.targetPrefab == targetPrefab)
                    {
                        categoryIdx = c;
                        itemIdx = d;
                        return;
                    }
                }
            }
        }
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