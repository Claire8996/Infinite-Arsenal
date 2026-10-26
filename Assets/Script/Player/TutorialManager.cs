using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ==========================================
// 1回で表示するuiObjects配列の個々のオブジェクト設定クラス
// ==========================================
[System.Serializable]
public class TutorialUIObjectItem
{
    [Tooltip("最前面に表示する対象のUIオブジェクト")]
    public GameObject uiObject;

    [Tooltip("チェックを入れると、このオブジェクトを最初は非表示にしておき、順番が来て最前面になる時だけ表示します")]
    public bool hideUntilActive = true;
}

// ==========================================
// 通常のチュートリアルUI配列の1要素（UI・ボタン・チェックボックス2種）
// ==========================================
[System.Serializable]
public class TutorialUIElement
{
    [Tooltip("最前面に表示するUIオブジェクトの配列（個々のオブジェクトごとにhideUntilActiveを設定できます）")]
    public TutorialUIObjectItem[] uiObjects;

    [Tooltip("押されたら次に進むボタン（※未設定の場合はEnterキーまたは画面クリックで次に進みます）")]
    public Button nextButton;

    [Tooltip("チェックを入れると、UI内のテキストを1文字ずつ順々に表示します")]
    public bool useTypewriter = false;

    [Tooltip("このステップ全体の初期非表示設定（ONのまま、上のuiObjects内の個別のhideUntilActiveで個々に切り替え可能です）")]
    public bool hideUntilActive = true;
}

// ==========================================
// Weapon SelectScene専用の配列の1要素（UI・ボタン・3つの武器チェックボックス）
// ==========================================
[System.Serializable]
public class WeaponSelectTutorialElement
{
    [Tooltip("最前面に表示するUIオブジェクトの配列（個々のオブジェクトごとにhideUntilActiveを設定できます）")]
    public TutorialUIObjectItem[] uiObjects;

    [Tooltip("押されたら次に進むボタン（※武器チェックボックスを使わない場合に使用。未設定なら無視されます）")]
    public Button nextButton;

    [Tooltip("Content内の3つの武器Prefabのうち、どれを最前面にして選択待ちにするか（要素0＝1つ目、要素1＝2つ目、要素2＝3つ目）")]
    public bool[] weaponCheckboxes = new bool[3];

    [Tooltip("チェックを入れると、UI内のテキストを1文字ずつ順々に表示します")]
    public bool useTypewriter = false;

    [Tooltip("このステップ全体の初期非表示設定（ONのまま、上のuiObjects内の個別のhideUntilActiveで個々に切り替え可能です）")]
    public bool hideUntilActive = true;
}

public class TutorialManager : MonoBehaviour
{
    // ==========================================
    // シーンをまたいで進行状況を保持する static 変数
    // ==========================================
    public static bool IsTutorialActive = false;
    public static int CurrentPhase = 0;
    // Phase 1: TutorialScene -> HomeScene (初回)
    // Phase 2: weapon gacha Scene (1連ガチャ)
    // Phase 3: weapon gacha Scene (3連ガチャ)
    // Phase 4: HomeScene (ガチャから戻り)
    // Phase 5: Weapon SelectScene (武器選択)
    // Phase 6: HomeScene (セレクトから戻り・最終)

    // ガチャシステムへ受け渡す確定排出データ
    public static bool ForceTutorialGacha = false;
    public static List<GameObject> ForcedCapsulePrefabs = new List<GameObject>();
    public static List<GameObject> ForcedRewardPrefabs = new List<GameObject>();

    [Header("【デバッグ・テスト用設定】")]
    [Tooltip("TutorialSceneを通らずに今のシーンから直接チュートリアルをテストしたい場合はチェックを入れてください")]
    public bool debugStartFromThisScene = false;
    [Tooltip("テスト開始時のフェーズ番号（1〜6）")]
    public int debugStartPhase = 1;

    [Header("【このシーンの共通UI設定】")]
    [Tooltip("シーン移動時の演出用FadeImage（Alpha値が0になったら灰色の背景を表示します）")]
    public Image fadeImage;
    [Tooltip("FadeImageのAlpha値がこの数値以下になったら0とみなすしきい値")]
    public float fadeAlphaThreshold = 0.02f;

    [Tooltip("チュートリアル中に表示する灰色の背景（※シーン移動ボタン以外では非表示になりません）")]
    public GameObject grayBackground;

    [Tooltip("テキストを1文字ずつ表示する際の文字送り速度（秒）")]
    public float typeDelay = 0.05f;

    // =====================================================
    // 【フェーズ1】HomeScene（TutorialSceneから移動してきた時）
    // =====================================================
    [Header("【フェーズ1：HomeScene（初回移動時）】")]
    [Tooltip("灰色の背景が表示された後に順番に最前面で表示されるUIとボタンの配列")]
    public TutorialUIElement[] phase1HomeSteps;
    [Tooltip("フェーズ1が終わった時に移動するガチャシーンの名前")]
    public string gachaSceneName = "weapon gacha Scene";

    // =====================================================
    // 【フェーズ2】weapon gacha Scene（1回目：1連ガチャ）
    // =====================================================
    [Header("【フェーズ2：weapon gacha Scene（1連ガチャ）】")]
    [Tooltip("同じシーン内のGachaSystem（未設定でも自動検索します）")]
    public GachaSystem gachaSystem;
    [Tooltip("ガチャ結果表示時に出るBackGroundFadeオブジェクト（未設定ならGachaSystemのbackgroundFadeImageを使用）")]
    public GameObject backGroundFadeObject;

    [Tooltip("① 灰色の背景が表示された後に順番に表示されるUI配列")]
    public TutorialUIElement[] phase2InitialSteps;
    [Tooltip("② ①が表示され終わったら押せるようになるボタン")]
    public Button phase2FirstButton;
    [Tooltip("②のボタンが押せるようになった時にどのUIよりも最前面に来るUI")]
    public GameObject phase2FirstButtonHighlightUI;
    [Tooltip("チェックを入れると②のボタンとUIを最初は非表示にしておきます")]
    public bool hidePhase2FirstButtonAtStart = false;

    [Tooltip("③ ②のボタンが押されたら順番に表示される複数個のUI配列")]
    public TutorialUIElement[] phase2BeforePullSteps;
    [Tooltip("④ ③が表示され終わったら押せるようになるボタン（1連ガチャを引くボタン）")]
    public Button phase2SinglePullButton;
    [Tooltip("④のボタンが押せるようになった時にどのUIよりも最前面に来るUI")]
    public GameObject phase2SinglePullHighlightUI;
    [Tooltip("チェックを入れると④のボタンとUIを最初は非表示にしておきます")]
    public bool hidePhase2SinglePullAtStart = false;

    [Tooltip("★1連ガチャで排出されるガチャ玉（カプセル）のPrefab")]
    public GameObject phase2SingleCapsulePrefab;
    [Tooltip("★1連ガチャで抽選される中身（武器・防具）のPrefab")]
    public GameObject phase2SingleRewardPrefab;

    // =====================================================
    // 【フェーズ3】weapon gacha Scene（2回目：3連ガチャ）
    // =====================================================
    [Header("【フェーズ3：weapon gacha Scene（3連ガチャ）】")]
    [Tooltip("⑤ 1連ガチャが終わりBackGroundFadeが非表示になったら順番に表示される複数個のUI配列")]
    public TutorialUIElement[] phase3AfterSingleSteps;
    [Tooltip("⑥ ⑤が表示され終わったら押せるようになるボタン")]
    public Button phase3MidButton;
    [Tooltip("⑥のボタンが押せるようになった時にどのUIよりも最前面に来るUI")]
    public GameObject phase3MidButtonHighlightUI;
    [Tooltip("チェックを入れると⑥のボタンとUIを最初は非表示にしておきます")]
    public bool hidePhase3MidButtonAtStart = false;

    [Tooltip("⑦ ⑥のボタンが押されたら順番に表示されるUI配列")]
    public TutorialUIElement[] phase3BeforeMultiPullSteps;
    [Tooltip("⑧ ⑦が表示され終わったら押せるようになるボタン（3連ガチャを引くボタン）")]
    public Button phase3MultiPullButton;
    [Tooltip("⑧のボタンが押せるようになった時にどのUIよりも最前面に来るUI")]
    public GameObject phase3MultiPullHighlightUI;
    [Tooltip("チェックを入れると⑧のボタンとUIを最初は非表示にしておきます")]
    public bool hidePhase3MultiPullAtStart = false;

    [Tooltip("★3連ガチャで排出されるガチャ玉（カプセル）のPrefab（3個分）")]
    public GameObject[] phase3MultiCapsulePrefabs = new GameObject[3];
    [Tooltip("★3連ガチャで抽選される中身（武器・防具）のPrefab（3個分）")]
    public GameObject[] phase3MultiRewardPrefabs = new GameObject[3];

    [Tooltip("⑨ 3連ガチャが終わりBackGroundFadeが非表示になったら順番に表示される複数個のUI配列")]
    public TutorialUIElement[] phase3AfterMultiSteps;
    [Tooltip("⑩ ⑨が表示され終わったら押せるようになるボタン（HomeSceneに戻るボタン）")]
    public Button phase3ReturnHomeButton;
    [Tooltip("⑩のボタンが押せるようになった時にどのUIよりも最前面に来るUI")]
    public GameObject phase3ReturnHomeHighlightUI;
    [Tooltip("チェックを入れると⑩のボタンとUIを最初は非表示にしておきます")]
    public bool hidePhase3ReturnHomeAtStart = false;
    [Tooltip("戻る先のHomeSceneの名前")]
    public string homeSceneName = "HomeScene";

    // =====================================================
    // 【フェーズ4】HomeScene（ガチャから戻ってきた時）
    // =====================================================
    [Header("【フェーズ4：HomeScene（ガチャから戻った時）】")]
    [Tooltip("HomeSceneに戻ってきた時に順番に表示される複数個のUI配列")]
    public TutorialUIElement[] phase4HomeReturnSteps;
    [Tooltip("UIが表示し終わったら押せるようになるボタン（Weapon SelectSceneへ移動するボタン）")]
    public Button phase4GoToSelectButton;
    [Tooltip("ボタンが押せるようになった時にどのUIよりも最前面に来るUI")]
    public GameObject phase4GoToSelectHighlightUI;
    [Tooltip("チェックを入れるとボタンとUIを最初は非表示にしておきます")]
    public bool hidePhase4GoToSelectAtStart = false;
    [Tooltip("移動先のWeapon SelectSceneの名前")]
    public string weaponSelectSceneName = "Weapon SelectScene";

    // =====================================================
    // 【フェーズ5】Weapon SelectScene
    // =====================================================
    [Header("【フェーズ5：Weapon SelectScene】")]
    [Tooltip("同じシーン内のWeaponInventoryManager（未設定でも自動検索します）")]
    public WeaponInventoryManager inventoryManager;
    [Tooltip("UI・ボタン・3つの武器チェックボックスを設定できる配列")]
    public WeaponSelectTutorialElement[] phase5SelectSteps;
    [Tooltip("配列がすべて表示され終わった後に最前面に来て押せるようになるBackButton")]
    public Button phase5BackButton;
    [Tooltip("BackButtonと一緒にどのUIよりも最前面に来るUI")]
    public GameObject phase5BackButtonUI;
    [Tooltip("チェックを入れるとBackButtonとUIを最初は非表示にしておきます")]
    public bool hidePhase5BackButtonAtStart = false;

    // =====================================================
    // 【フェーズ6】HomeScene（Weapon Selectから戻ってきた最終段階）
    // =====================================================
    [Header("【フェーズ6：HomeScene（最終段階）】")]
    public TutorialUIElement[] phase6FinalHomeSteps;
    [Tooltip("UIが表示され終わった後に最前面に来て押せるようになる最終ボタン")]
    public Button phase6FinalButton;
    [Tooltip("最終ボタンと一緒にどのUIよりも最前面に来るUI")]
    public GameObject phase6FinalHighlightUI;
    [Tooltip("チェックを入れると最終ボタンとUIを最初は非表示にしておきます")]
    public bool hidePhase6FinalButtonAtStart = false;

    // 内部制御用
    private Dictionary<TextMeshProUGUI, string> originalTMPTexts = new Dictionary<TextMeshProUGUI, string>();
    private Dictionary<Text, string> originalNormalTexts = new Dictionary<Text, string>();
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private List<GameObject> currentHighlightedObjects = new List<GameObject>();

    private const int GRAY_BG_SORT_ORDER = 900;
    private const int BUTTON_SORT_ORDER = 1050;
    private const int TOP_UI_SORT_ORDER = 1100;
    private const int BUTTON_HIGHLIGHT_UI_SORT_ORDER = 1200;

    void Awake()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName.IndexOf("Tutorial", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            IsTutorialActive = true;
            CurrentPhase = 1;
            return;
        }

        if (debugStartFromThisScene && !IsTutorialActive)
        {
            IsTutorialActive = true;
            CurrentPhase = debugStartPhase;
        }
    }

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.IndexOf("Tutorial", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return;
        }

        if (!IsTutorialActive)
        {
            if (grayBackground != null) grayBackground.SetActive(false);
            return;
        }

        if (grayBackground != null)
        {
            grayBackground.SetActive(false);
        }

        InitializeHideStatesForCurrentPhase();
        StartCoroutine(RunTutorialForCurrentScene());
    }

    private void InitializeHideStatesForCurrentPhase()
    {
        if (CurrentPhase == 1)
        {
            HideElementsIfConfigured(phase1HomeSteps);
        }
        else if (CurrentPhase == 2 || CurrentPhase == 3)
        {
            HideElementsIfConfigured(phase2InitialSteps);
            HideElementsIfConfigured(phase2BeforePullSteps);
            HideElementsIfConfigured(phase3AfterSingleSteps);
            HideElementsIfConfigured(phase3BeforeMultiPullSteps);
            HideElementsIfConfigured(phase3AfterMultiSteps);

            SetActionButtonInitialState(phase2FirstButton, phase2FirstButtonHighlightUI, hidePhase2FirstButtonAtStart);
            SetActionButtonInitialState(phase2SinglePullButton, phase2SinglePullHighlightUI, hidePhase2SinglePullAtStart);
            SetActionButtonInitialState(phase3MidButton, phase3MidButtonHighlightUI, hidePhase3MidButtonAtStart);
            SetActionButtonInitialState(phase3MultiPullButton, phase3MultiPullHighlightUI, hidePhase3MultiPullAtStart);
            SetActionButtonInitialState(phase3ReturnHomeButton, phase3ReturnHomeHighlightUI, hidePhase3ReturnHomeAtStart);
        }
        else if (CurrentPhase == 4)
        {
            HideElementsIfConfigured(phase4HomeReturnSteps);
            SetActionButtonInitialState(phase4GoToSelectButton, phase4GoToSelectHighlightUI, hidePhase4GoToSelectAtStart);
        }
        else if (CurrentPhase == 5)
        {
            HideSelectElementsIfConfigured(phase5SelectSteps);
            SetActionButtonInitialState(phase5BackButton, phase5BackButtonUI, hidePhase5BackButtonAtStart);
        }
        else if (CurrentPhase == 6)
        {
            HideElementsIfConfigured(phase6FinalHomeSteps);
            SetActionButtonInitialState(phase6FinalButton, phase6FinalHighlightUI, hidePhase6FinalButtonAtStart);
        }
    }

    private void HideElementsIfConfigured(TutorialUIElement[] elements)
    {
        if (elements == null) return;
        foreach (var elem in elements)
        {
            if (elem == null) continue;

            // ボタンが参照されているElementは、最後のUIが表示されるまでボタンの機能を無効化しておく
            if (elem.nextButton != null)
            {
                elem.nextButton.interactable = false;
            }

            if (elem.uiObjects != null)
            {
                foreach (var item in elem.uiObjects)
                {
                    if (item != null && item.uiObject != null)
                    {
                        Button itemBtn = item.uiObject.GetComponentInChildren<Button>(true);
                        if (itemBtn != null)
                        {
                            itemBtn.interactable = false;
                        }

                        if (elem.hideUntilActive && item.hideUntilActive)
                        {
                            item.uiObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    private void HideSelectElementsIfConfigured(WeaponSelectTutorialElement[] elements)
    {
        if (elements == null) return;
        foreach (var elem in elements)
        {
            if (elem == null) continue;

            if (elem.nextButton != null)
            {
                elem.nextButton.interactable = false;
            }

            if (elem.uiObjects != null)
            {
                foreach (var item in elem.uiObjects)
                {
                    if (item != null && item.uiObject != null)
                    {
                        Button itemBtn = item.uiObject.GetComponentInChildren<Button>(true);
                        if (itemBtn != null)
                        {
                            itemBtn.interactable = false;
                        }

                        if (elem.hideUntilActive && item.hideUntilActive)
                        {
                            item.uiObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    private void SetActionButtonInitialState(Button btn, GameObject highlightUI, bool hideAtStart)
    {
        if (btn != null)
        {
            btn.interactable = false;
            if (hideAtStart) btn.gameObject.SetActive(false);
        }
        if (highlightUI != null && hideAtStart)
        {
            highlightUI.SetActive(false);
        }
    }

    private IEnumerator RunTutorialForCurrentScene()
    {
        yield return StartCoroutine(WaitForFadeAlphaZero());
        ShowGrayBackground();

        if (CurrentPhase == 1)
        {
            yield return StartCoroutine(PlayUIElementsSequence(phase1HomeSteps));

            CurrentPhase = 2;
            if (!string.IsNullOrEmpty(gachaSceneName))
            {
                SceneManager.LoadScene(gachaSceneName);
            }
        }
        else if (CurrentPhase == 2 || CurrentPhase == 3)
        {
            yield return StartCoroutine(RunGachaSceneTutorial());
        }
        else if (CurrentPhase == 4)
        {
            yield return StartCoroutine(PlayUIElementsSequence(phase4HomeReturnSteps));

            bool goSelectClicked = false;
            ActivateAndBringButtonToFront(phase4GoToSelectButton, phase4GoToSelectHighlightUI, () =>
            {
                goSelectClicked = true;
            });

            yield return new WaitUntil(() => goSelectClicked);

            CurrentPhase = 5;
            if (!string.IsNullOrEmpty(weaponSelectSceneName))
            {
                SceneManager.LoadScene(weaponSelectSceneName);
            }
        }
        else if (CurrentPhase == 5)
        {
            yield return StartCoroutine(RunWeaponSelectSceneTutorial());
        }
        else if (CurrentPhase == 6)
        {
            yield return StartCoroutine(PlayUIElementsSequence(phase6FinalHomeSteps));

            bool finalClicked = false;
            if (phase6FinalButton != null)
            {
                ActivateAndBringButtonToFront(phase6FinalButton, phase6FinalHighlightUI, () =>
                {
                    finalClicked = true;
                });
                yield return new WaitUntil(() => finalClicked);
            }
            else if (phase6FinalHighlightUI != null)
            {
                phase6FinalHighlightUI.SetActive(true);
                BringToFront(phase6FinalHighlightUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, false);
            }

            CompleteTutorial();
        }
    }

    private IEnumerator RunGachaSceneTutorial()
    {
        if (gachaSystem == null) gachaSystem = FindObjectOfType<GachaSystem>();
        if (backGroundFadeObject == null && gachaSystem != null && gachaSystem.backgroundFadeImage != null)
        {
            backGroundFadeObject = gachaSystem.backgroundFadeImage.gameObject;
        }

        if (CurrentPhase == 2)
        {
            yield return StartCoroutine(PlayUIElementsSequence(phase2InitialSteps));

            bool firstBtnClicked = false;
            ActivateAndBringButtonToFront(phase2FirstButton, phase2FirstButtonHighlightUI, () =>
            {
                firstBtnClicked = true;
            });
            yield return new WaitUntil(() => firstBtnClicked);
            if (hidePhase2FirstButtonAtStart && phase2FirstButtonHighlightUI != null) phase2FirstButtonHighlightUI.SetActive(false);
            RestoreHighlightedObjects();

            yield return StartCoroutine(PlayUIElementsSequence(phase2BeforePullSteps));

            SetupForcedSingleGacha();

            bool singlePullClicked = false;
            ActivateAndBringButtonToFront(phase2SinglePullButton, phase2SinglePullHighlightUI, () =>
            {
                singlePullClicked = true;
            });
            yield return new WaitUntil(() => singlePullClicked);
            if (hidePhase2SinglePullAtStart && phase2SinglePullHighlightUI != null) phase2SinglePullHighlightUI.SetActive(false);
            RestoreHighlightedObjects();

            yield return StartCoroutine(WaitForGachaBackgroundFadeCycle());

            CurrentPhase = 3;
        }

        if (CurrentPhase == 3)
        {
            yield return StartCoroutine(PlayUIElementsSequence(phase3AfterSingleSteps));

            bool midBtnClicked = false;
            ActivateAndBringButtonToFront(phase3MidButton, phase3MidButtonHighlightUI, () =>
            {
                midBtnClicked = true;
            });
            yield return new WaitUntil(() => midBtnClicked);
            if (hidePhase3MidButtonAtStart && phase3MidButtonHighlightUI != null) phase3MidButtonHighlightUI.SetActive(false);
            RestoreHighlightedObjects();

            yield return StartCoroutine(PlayUIElementsSequence(phase3BeforeMultiPullSteps));

            SetupForcedMultiGacha();

            bool multiPullClicked = false;
            ActivateAndBringButtonToFront(phase3MultiPullButton, phase3MultiPullHighlightUI, () =>
            {
                multiPullClicked = true;
            });
            yield return new WaitUntil(() => multiPullClicked);
            if (hidePhase3MultiPullAtStart && phase3MultiPullHighlightUI != null) phase3MultiPullHighlightUI.SetActive(false);
            RestoreHighlightedObjects();

            yield return StartCoroutine(WaitForGachaBackgroundFadeCycle());

            yield return StartCoroutine(PlayUIElementsSequence(phase3AfterMultiSteps));

            bool returnHomeClicked = false;
            ActivateAndBringButtonToFront(phase3ReturnHomeButton, phase3ReturnHomeHighlightUI, () =>
            {
                returnHomeClicked = true;
            });
            yield return new WaitUntil(() => returnHomeClicked);

            CurrentPhase = 4;
            if (!string.IsNullOrEmpty(homeSceneName))
            {
                SceneManager.LoadScene(homeSceneName);
            }
        }
    }

    private void SetupForcedSingleGacha()
    {
        ForcedCapsulePrefabs.Clear();
        ForcedRewardPrefabs.Clear();

        if (phase2SingleCapsulePrefab != null) ForcedCapsulePrefabs.Add(phase2SingleCapsulePrefab);
        if (phase2SingleRewardPrefab != null) ForcedRewardPrefabs.Add(phase2SingleRewardPrefab);

        ForceTutorialGacha = (ForcedCapsulePrefabs.Count > 0 || ForcedRewardPrefabs.Count > 0);
    }

    private void SetupForcedMultiGacha()
    {
        ForcedCapsulePrefabs.Clear();
        ForcedRewardPrefabs.Clear();

        for (int i = 0; i < 3; i++)
        {
            if (phase3MultiCapsulePrefabs != null && phase3MultiCapsulePrefabs.Length > 0)
            {
                int capIdx = Mathf.Min(i, phase3MultiCapsulePrefabs.Length - 1);
                if (phase3MultiCapsulePrefabs[capIdx] != null)
                {
                    ForcedCapsulePrefabs.Add(phase3MultiCapsulePrefabs[capIdx]);
                }
            }

            if (phase3MultiRewardPrefabs != null && phase3MultiRewardPrefabs.Length > 0)
            {
                int rewIdx = Mathf.Min(i, phase3MultiRewardPrefabs.Length - 1);
                if (phase3MultiRewardPrefabs[rewIdx] != null)
                {
                    ForcedRewardPrefabs.Add(phase3MultiRewardPrefabs[rewIdx]);
                }
            }
        }

        ForceTutorialGacha = (ForcedCapsulePrefabs.Count > 0 || ForcedRewardPrefabs.Count > 0);
    }

    private IEnumerator WaitForGachaBackgroundFadeCycle()
    {
        if (backGroundFadeObject == null)
        {
            yield return new WaitForSeconds(2.0f);
            yield break;
        }

        yield return new WaitUntil(() => backGroundFadeObject.activeInHierarchy);
        yield return new WaitUntil(() => !backGroundFadeObject.activeInHierarchy);
    }

    private IEnumerator RunWeaponSelectSceneTutorial()
    {
        if (inventoryManager == null) inventoryManager = FindObjectOfType<WeaponInventoryManager>();

        yield return null;

        if (phase5SelectSteps != null)
        {
            for (int i = 0; i < phase5SelectSteps.Length; i++)
            {
                WeaponSelectTutorialElement step = phase5SelectSteps[i];
                if (step == null) continue;

                RestoreHighlightedObjects();

                List<TextMeshProUGUI> tmpsToType = new List<TextMeshProUGUI>();
                List<Text> textsToType = new List<Text>();

                int totalItems = step.uiObjects != null ? step.uiObjects.Length : 0;

                if (step.uiObjects != null)
                {
                    for (int u = 0; u < totalItems; u++)
                    {
                        TutorialUIObjectItem item = step.uiObjects[u];
                        if (item != null && item.uiObject != null)
                        {
                            item.uiObject.SetActive(true);
                            BringToFront(item.uiObject, TOP_UI_SORT_ORDER + u, false);

                            if (step.useTypewriter)
                            {
                                CollectTextComponents(item.uiObject, tmpsToType, textsToType);
                            }
                        }

                        // 最後のElementが表示されたタイミングで、一緒にボタンや武器選択を有効化する
                        if (u == totalItems - 1)
                        {
                            EnableStepButtons(step.uiObjects, step.nextButton);
                        }
                    }
                }
                else
                {
                    EnableStepButtons(null, step.nextButton);
                }

                List<GameObject> targetWeaponItems = new List<GameObject>();
                if (step.weaponCheckboxes != null && inventoryManager != null && inventoryManager.contentPanel != null)
                {
                    int childCount = inventoryManager.contentPanel.childCount;
                    for (int wIdx = 0; wIdx < 3 && wIdx < step.weaponCheckboxes.Length; wIdx++)
                    {
                        if (step.weaponCheckboxes[wIdx] && wIdx < childCount)
                        {
                            Transform weaponChild = inventoryManager.contentPanel.GetChild(wIdx);
                            if (weaponChild != null)
                            {
                                BringToFront(weaponChild.gameObject, BUTTON_SORT_ORDER, true);
                                targetWeaponItems.Add(weaponChild.gameObject);
                            }
                        }
                    }
                }

                if (tmpsToType.Count > 0 || textsToType.Count > 0)
                {
                    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                    typingCoroutine = StartCoroutine(TypewriterRoutine(tmpsToType, textsToType));
                }

                if (targetWeaponItems.Count > 0)
                {
                    bool weaponSelected = false;
                    foreach (GameObject wObj in targetWeaponItems)
                    {
                        Button b = wObj.GetComponentInChildren<Button>(true);
                        if (b == null) b = wObj.AddComponent<Button>();
                        b.enabled = true;
                        b.interactable = true;

                        UnityEngine.Events.UnityAction onSelectAction = null;
                        onSelectAction = () =>
                        {
                            if (isTyping) SkipTyping();
                            b.onClick.RemoveListener(onSelectAction);
                            weaponSelected = true;
                        };
                        b.onClick.AddListener(onSelectAction);
                    }

                    yield return new WaitUntil(() => weaponSelected);
                }
                else if (step.nextButton != null)
                {
                    EnableStepButtons(step.uiObjects, step.nextButton);

                    bool btnClicked = false;
                    UnityEngine.Events.UnityAction action = null;
                    action = () =>
                    {
                        if (isTyping)
                        {
                            SkipTyping();
                            return;
                        }
                        step.nextButton.onClick.RemoveListener(action);
                        btnClicked = true;
                    };
                    step.nextButton.onClick.AddListener(action);

                    yield return new WaitUntil(() => btnClicked);
                }
                else
                {
                    yield return StartCoroutine(WaitForUserAdvanceInput());
                }

                if (step.uiObjects != null)
                {
                    foreach (TutorialUIObjectItem item in step.uiObjects)
                    {
                        if (item != null && item.uiObject != null && step.hideUntilActive && item.hideUntilActive)
                        {
                            item.uiObject.SetActive(false);
                        }
                    }
                }
            }
        }

        RestoreHighlightedObjects();

        bool backClicked = false;
        ActivateAndBringButtonToFront(phase5BackButton, phase5BackButtonUI, () =>
        {
            backClicked = true;
        });

        yield return new WaitUntil(() => backClicked);

        CurrentPhase = 6;
        if (!string.IsNullOrEmpty(homeSceneName))
        {
            SceneManager.LoadScene(homeSceneName);
        }
    }

    private IEnumerator PlayUIElementsSequence(TutorialUIElement[] elements)
    {
        if (elements == null || elements.Length == 0) yield break;

        for (int i = 0; i < elements.Length; i++)
        {
            TutorialUIElement elem = elements[i];
            if (elem == null) continue;

            RestoreHighlightedObjects();

            List<TextMeshProUGUI> tmpsToType = new List<TextMeshProUGUI>();
            List<Text> textsToType = new List<Text>();

            int totalItems = elem.uiObjects != null ? elem.uiObjects.Length : 0;

            if (elem.uiObjects != null && totalItems > 0)
            {
                for (int u = 0; u < totalItems; u++)
                {
                    TutorialUIObjectItem item = elem.uiObjects[u];
                    if (item != null && item.uiObject != null)
                    {
                        item.uiObject.SetActive(true);
                        BringToFront(item.uiObject, TOP_UI_SORT_ORDER + u, false);

                        if (elem.useTypewriter)
                        {
                            CollectTextComponents(item.uiObject, tmpsToType, textsToType);
                        }
                    }

                    // ★最後のElementのUIが表示されたときに、一緒にボタン等も有効化する
                    if (u == totalItems - 1)
                    {
                        EnableStepButtons(elem.uiObjects, elem.nextButton);
                    }
                }
            }
            else
            {
                EnableStepButtons(null, elem.nextButton);
            }

            if (tmpsToType.Count > 0 || textsToType.Count > 0)
            {
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypewriterRoutine(tmpsToType, textsToType));
            }

            if (elem.nextButton != null)
            {
                EnableStepButtons(elem.uiObjects, elem.nextButton);

                bool clicked = false;
                UnityEngine.Events.UnityAction action = null;
                action = () =>
                {
                    if (isTyping)
                    {
                        SkipTyping();
                        return;
                    }
                    elem.nextButton.onClick.RemoveListener(action);
                    clicked = true;
                };
                elem.nextButton.onClick.AddListener(action);

                yield return new WaitUntil(() => clicked);
            }
            else
            {
                yield return StartCoroutine(WaitForUserAdvanceInput());
            }

            // 各オブジェクトの hideUntilActive に応じて個別に非表示に戻す
            if (elem.uiObjects != null)
            {
                foreach (TutorialUIObjectItem item in elem.uiObjects)
                {
                    if (item != null && item.uiObject != null && elem.hideUntilActive && item.hideUntilActive)
                    {
                        item.uiObject.SetActive(false);
                    }
                }
            }
        }

        RestoreHighlightedObjects();
    }

    // 最後に表示されるUIと一緒にボタンを有効化＆最前面化するメソッド
    private void EnableStepButtons(TutorialUIObjectItem[] uiItems, Button targetNextButton)
    {
        if (uiItems != null)
        {
            for (int u = 0; u < uiItems.Length; u++)
            {
                var item = uiItems[u];
                if (item == null || item.uiObject == null) continue;

                Button[] innerBtns = item.uiObject.GetComponentsInChildren<Button>(true);
                foreach (Button b in innerBtns)
                {
                    if (b != null)
                    {
                        b.gameObject.SetActive(true);
                        b.enabled = true;
                        b.interactable = true;
                    }
                }
            }
        }

        if (targetNextButton != null)
        {
            targetNextButton.gameObject.SetActive(true);
            targetNextButton.enabled = true;
            targetNextButton.interactable = true;
            BringToFront(targetNextButton.gameObject, BUTTON_SORT_ORDER, true);
        }
    }

    private IEnumerator WaitForUserAdvanceInput()
    {
        yield return null;

        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0))
            {
                if (isTyping)
                {
                    SkipTyping();
                    yield return null;
                    continue;
                }
                break;
            }
            yield return null;
        }
    }

    private void CollectTextComponents(GameObject root, List<TextMeshProUGUI> tmps, List<Text> normalTexts)
    {
        TextMeshProUGUI[] foundTmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in foundTmps)
        {
            if (t == null) continue;
            if (!originalTMPTexts.ContainsKey(t)) originalTMPTexts[t] = t.text;
            tmps.Add(t);
        }

        Text[] foundTexts = root.GetComponentsInChildren<Text>(true);
        foreach (var t in foundTexts)
        {
            if (t == null) continue;
            if (!originalNormalTexts.ContainsKey(t)) originalNormalTexts[t] = t.text;
            normalTexts.Add(t);
        }
    }

    private IEnumerator TypewriterRoutine(List<TextMeshProUGUI> tmps, List<Text> normalTexts)
    {
        isTyping = true;

        int maxLen = 0;
        foreach (var t in tmps)
        {
            t.text = "";
            if (originalTMPTexts[t].Length > maxLen) maxLen = originalTMPTexts[t].Length;
        }
        foreach (var t in normalTexts)
        {
            t.text = "";
            if (originalNormalTexts[t].Length > maxLen) maxLen = originalNormalTexts[t].Length;
        }

        for (int i = 0; i < maxLen; i++)
        {
            foreach (var t in tmps)
            {
                if (t != null && i < originalTMPTexts[t].Length)
                {
                    t.text += originalTMPTexts[t][i];
                }
            }
            foreach (var t in normalTexts)
            {
                if (t != null && i < originalNormalTexts[t].Length)
                {
                    t.text += originalNormalTexts[t][i];
                }
            }
            yield return new WaitForSeconds(typeDelay);
        }

        isTyping = false;
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        foreach (var kvp in originalTMPTexts)
        {
            if (kvp.Key != null && kvp.Key.gameObject.activeInHierarchy)
            {
                kvp.Key.text = kvp.Value;
            }
        }
        foreach (var kvp in originalNormalTexts)
        {
            if (kvp.Key != null && kvp.Key.gameObject.activeInHierarchy)
            {
                kvp.Key.text = kvp.Value;
            }
        }

        isTyping = false;
    }

    private IEnumerator WaitForFadeAlphaZero()
    {
        if (fadeImage == null) yield break;

        while (fadeImage != null && fadeImage.gameObject.activeInHierarchy && fadeImage.color.a > fadeAlphaThreshold)
        {
            yield return null;
        }
    }

    private void ShowGrayBackground()
    {
        if (grayBackground == null) return;

        grayBackground.SetActive(true);
        Canvas bgCanvas = grayBackground.GetComponent<Canvas>();
        if (bgCanvas == null)
        {
            bgCanvas = grayBackground.AddComponent<Canvas>();
            grayBackground.AddComponent<GraphicRaycaster>();
        }
        bgCanvas.overrideSorting = true;
        bgCanvas.sortingOrder = GRAY_BG_SORT_ORDER;
    }

    private void ActivateAndBringButtonToFront(Button btn, GameObject highlightUI, System.Action onClickCallback)
    {
        if (btn != null)
        {
            btn.gameObject.SetActive(true);
            btn.enabled = true;
            btn.interactable = true;
            BringToFront(btn.gameObject, BUTTON_SORT_ORDER, true);
        }

        if (highlightUI != null)
        {
            highlightUI.SetActive(true);
            BringToFront(highlightUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, false);
        }

        if (btn != null)
        {
            UnityEngine.Events.UnityAction wrapper = null;
            wrapper = () =>
            {
                btn.onClick.RemoveListener(wrapper);
                onClickCallback?.Invoke();
            };
            btn.onClick.AddListener(wrapper);
        }
        else
        {
            onClickCallback?.Invoke();
        }
    }

    private void BringToFront(GameObject obj, int sortOrder, bool needsRaycaster = true)
    {
        if (obj == null) return;

        Transform current = obj.transform;
        while (current != null && !current.gameObject.activeSelf)
        {
            current.gameObject.SetActive(true);
            current = current.parent;
        }

        Canvas canvas = obj.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = obj.AddComponent<Canvas>();
            if (!currentHighlightedObjects.Contains(obj))
            {
                currentHighlightedObjects.Add(obj);
            }
        }

        if (needsRaycaster || obj.GetComponentInChildren<Button>(true) != null)
        {
            if (obj.GetComponent<GraphicRaycaster>() == null)
            {
                obj.AddComponent<GraphicRaycaster>();
            }
        }

        Canvas.ForceUpdateCanvases();
        canvas.enabled = true;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortOrder;
    }

    private void RestoreHighlightedObjects()
    {
        foreach (GameObject obj in currentHighlightedObjects)
        {
            if (obj == null) continue;
            GraphicRaycaster raycaster = obj.GetComponent<GraphicRaycaster>();
            if (raycaster != null) Destroy(raycaster);
            Canvas canvas = obj.GetComponent<Canvas>();
            if (canvas != null) Destroy(canvas);
        }
        currentHighlightedObjects.Clear();
    }

    private void CompleteTutorial()
    {
        RestoreHighlightedObjects();
        if (grayBackground != null) grayBackground.SetActive(false);

        IsTutorialActive = false;
        CurrentPhase = 0;
        ForceTutorialGacha = false;
        ForcedCapsulePrefabs.Clear();
        ForcedRewardPrefabs.Clear();

        Debug.Log("チュートリアルがすべて完了しました！通常プレイに移行します。");
    }
}