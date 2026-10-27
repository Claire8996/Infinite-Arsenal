using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
// 通常のチュートリアルUI配列の1要素（UI・ボタン・ボタン連動オブジェクト・追加最前面UI・チェックボックス）
// ==========================================
[System.Serializable]
public class TutorialUIElement
{
    [Tooltip("最前面に表示するUIオブジェクトの配列（個々のオブジェクトごとにhideUntilActiveを設定できます）")]
    public TutorialUIObjectItem[] uiObjects;

    [Tooltip("押されたら次に進むボタン（※未設定の場合はEnterキーまたは画面クリックで次に進みます）")]
    public Button nextButton;

    [Tooltip("ボタンが押せるようになった際に一緒に表示するオブジェクト（矢印など）")]
    public GameObject buttonLinkedObject;

    [Tooltip("チェックを入れると、buttonLinkedObjectを最初は非表示にしておき、ボタンが押せるようになる時だけ表示します")]
    public bool hideButtonLinkedObjectUntilActive = true;

    [Tooltip("★追加：ボタンが押せるようになった際に、もう一つどのUIよりもLayerを手前にして表示するオブジェクト（別ガチャのボタンUIなど）")]
    public GameObject extraFrontUI;

    [Tooltip("チェックを入れると、UI内のテキストを1文字ずつ順々に表示します")]
    public bool useTypewriter = false;

    [Tooltip("このステップ全体の初期非表示設定（ONのまま、上のuiObjects内の個別のhideUntilActiveで個々に切り替え可能です）")]
    public bool hideUntilActive = true;
}

// ==========================================
// Weapon SelectScene専用の配列の1要素（UI・ボタン・ボタン連動オブジェクト・追加最前面UI・3つの武器チェックボックス）
// ==========================================
[System.Serializable]
public class WeaponSelectTutorialElement
{
    [Tooltip("最前面に表示するUIオブジェクトの配列（個々のオブジェクトごとにhideUntilActiveを設定できます）")]
    public TutorialUIObjectItem[] uiObjects;

    [Tooltip("押されたら次に進むボタン（※武器チェックボックスを使わない場合に使用。未設定なら無視されます）")]
    public Button nextButton;

    [Tooltip("ボタンが押せるようになった際に一緒に表示するオブジェクト（矢印など）")]
    public GameObject buttonLinkedObject;

    [Tooltip("チェックを入れると、buttonLinkedObjectを最初は非表示にしておき、ボタンが押せるようになる時だけ表示します")]
    public bool hideButtonLinkedObjectUntilActive = true;

    [Tooltip("★追加：ボタンが押せるようになった際に、もう一つどのUIよりもLayerを手前にして表示するオブジェクト")]
    public GameObject extraFrontUI;

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

    // ★追加：チュートリアル中のシーン移動で未初期化のEnergyManagerがエネルギーを0に上書きしてしまうのを防ぐ保護用変数
    private static int protectedEnergyValue = -1;
    private static float protectedTimerValue = -1f;
    private static string protectedLastTimeValue = "";
    private const string ENERGY_KEY = "SavedEnergy";
    private const string TIMER_KEY = "SavedTimer";
    private const string LAST_TIME_KEY = "LastSavedTime";

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
    [Tooltip("ガチャの「すべて開ける」ボタン（未設定でもGachaSystemから自動取得します）")]
    public Button gachaOpenAllButton;
    [Tooltip("「すべて開ける」ボタンと一緒に最前面に表示するUI（未設定でも自動処理します）")]
    public GameObject gachaOpenAllButtonUI;

    [Tooltip("① 灰色の背景が表示された後に順番に表示されるUI配列")]
    public TutorialUIElement[] phase2InitialSteps;
    [Tooltip("② ①が表示され終わったら押せるようになるボタン")]
    public Button phase2FirstButton;
    [Tooltip("②のボタンが押せるようになった時にどのUIよりも最前面に来るUI")]
    public GameObject phase2FirstButtonHighlightUI;
    [Tooltip("★追加：②のボタンが押せるようになった時に、もう一つどのUIよりもLayerを手前にして表示するUI（もう片方のガチャ切替ボタンUIなど）")]
    public GameObject phase2FirstButtonExtraFrontUI;
    [Tooltip("ボタンが押せるようになった際に一緒に表示するオブジェクト")]
    public GameObject phase2FirstButtonLinkedObject;
    [Tooltip("チェックを入れると、phase2FirstButtonLinkedObjectを最初は非表示にしておき、ボタンが押せるようになる時だけ表示します")]
    public bool hidePhase2FirstButtonLinkedObjectUntilActive = true;
    [Tooltip("チェックを入れると②のボタンとUIを最初は非表示にしておきます")]
    public bool hidePhase2FirstButtonAtStart = false;

    [Tooltip("③ ②のボタンが押されたら順番に表示される複数個のUI配列")]
    public TutorialUIElement[] phase2BeforePullSteps;
    [Tooltip("④ ③が表示され終わったら押せるようになるボタン（1連ガチャを引くボタン）")]
    public Button phase2SinglePullButton;
    [Tooltip("④のボタンが押せるようになった時にどのUIよりも最前面に来るUI")]
    public GameObject phase2SinglePullHighlightUI;
    [Tooltip("★追加：④のボタンが押せるようになった時に、もう一つどのUIよりもLayerを手前にして表示するUI")]
    public GameObject phase2SinglePullExtraFrontUI;
    [Tooltip("ボタンが押せるようになった際に一緒に表示するオブジェクト")]
    public GameObject phase2SinglePullButtonLinkedObject;
    [Tooltip("チェックを入れると、phase2SinglePullButtonLinkedObjectを最初は非表示にしておき、ボタンが押せるようになる時だけ表示します")]
    public bool hidePhase2SinglePullButtonLinkedObjectUntilActive = true;
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
    [Tooltip("★追加：⑥のボタンが押せるようになった時に、もう一つどのUIよりもLayerを手前にして表示するUI（もう片方のガチャ切替ボタンUIなど）")]
    public GameObject phase3MidButtonExtraFrontUI;
    [Tooltip("ボタンが押せるようになった際に一緒に表示するオブジェクト")]
    public GameObject phase3MidButtonLinkedObject;
    [Tooltip("チェックを入れると、phase3MidButtonLinkedObjectを最初は非表示にしておき、ボタンが押せるようになる時だけ表示します")]
    public bool hidePhase3MidButtonLinkedObjectUntilActive = true;
    [Tooltip("チェックを入れると⑥のボタンとUIを最初は非表示にしておきます")]
    public bool hidePhase3MidButtonAtStart = false;

    [Tooltip("⑦ ⑥のボタンが押されたら順番に表示されるUI配列")]
    public TutorialUIElement[] phase3BeforeMultiPullSteps;
    [Tooltip("⑧ ⑦が表示され終わったら押せるようになるボタン（3連ガチャを引くボタン）")]
    public Button phase3MultiPullButton;
    [Tooltip("⑧のボタンが押せるようになった時にどのUIよりも最前面に来るUI")]
    public GameObject phase3MultiPullHighlightUI;
    [Tooltip("★追加：⑧のボタンが押せるようになった時に、もう一つどのUIよりもLayerを手前にして表示するUI")]
    public GameObject phase3MultiPullExtraFrontUI;
    [Tooltip("ボタンが押せるようになった際に一緒に表示するオブジェクト")]
    public GameObject phase3MultiPullButtonLinkedObject;
    [Tooltip("チェックを入れると、phase3MultiPullButtonLinkedObjectを最初は非表示にしておき、ボタンが押せるようになる時だけ表示します")]
    public bool hidePhase3MultiPullButtonLinkedObjectUntilActive = true;
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
    [Tooltip("★追加：⑩のボタンが押せるようになった時に、もう一つどのUIよりもLayerを手前にして表示するUI")]
    public GameObject phase3ReturnHomeExtraFrontUI;
    [Tooltip("ボタンが押せるようになった際に一緒に表示するオブジェクト")]
    public GameObject phase3ReturnHomeButtonLinkedObject;
    [Tooltip("チェックを入れると、phase3ReturnHomeButtonLinkedObjectを最初は非表示にしておき、ボタンが押せるようになる時だけ表示します")]
    public bool hidePhase3ReturnHomeButtonLinkedObjectUntilActive = true;
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
    [Tooltip("★追加：ボタンが押せるようになった時に、もう一つどのUIよりもLayerを手前にして表示するUI")]
    public GameObject phase4GoToSelectExtraFrontUI;
    [Tooltip("ボタンが押せるようになった際に一緒に表示するオブジェクト")]
    public GameObject phase4GoToSelectButtonLinkedObject;
    [Tooltip("チェックを入れると、phase4GoToSelectButtonLinkedObjectを最初は非表示にしておき、ボタンが押せるようになる時だけ表示します")]
    public bool hidePhase4GoToSelectButtonLinkedObjectUntilActive = true;
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
    [Tooltip("★追加：BackButtonが押せるようになった時に、もう一つどのUIよりもLayerを手前にして表示するUI")]
    public GameObject phase5BackButtonExtraFrontUI;
    [Tooltip("ボタンが押せるようになった際に一緒に表示するオブジェクト")]
    public GameObject phase5BackButtonLinkedObject;
    [Tooltip("チェックを入れると、phase5BackButtonLinkedObjectを最初は非表示にしておき、ボタンが押せるようになる時だけ表示します")]
    public bool hidePhase5BackButtonLinkedObjectUntilActive = true;
    [Tooltip("チェックを入れるとBackButtonとUIを最初は非表示にしておきます")]
    public bool hidePhase5BackButtonAtStart = false;

    // =====================================================
    // 【フェーズ6】HomeScene（Weapon Selectから戻ってきた最終段階）
    // =====================================================
    [Header("【フェーズ6：HomeScene（最終段階）】")]
    [Tooltip("Weapon SelectSceneから戻ってきた時に順番に表示される複数個のUI配列")]
    public TutorialUIElement[] phase6FinalHomeSteps;
    [Tooltip("UIが表示し終わった後に最前面に来て押せるようになる最終ボタン")]
    public Button phase6FinalButton;
    [Tooltip("最終ボタンと一緒にどのUIよりも最前面に来るUI")]
    public GameObject phase6FinalHighlightUI;
    [Tooltip("★追加：最終ボタンが押せるようになった時に、もう一つどのUIよりもLayerを手前にして表示するUI")]
    public GameObject phase6FinalExtraFrontUI;
    [Tooltip("ボタンが押せるようになった際に一緒に表示するオブジェクト")]
    public GameObject phase6FinalButtonLinkedObject;
    [Tooltip("チェックを入れると、phase6FinalButtonLinkedObjectを最初は非表示にしておき、ボタンが押せるようになる時だけ表示します")]
    public bool hidePhase6FinalButtonLinkedObjectUntilActive = true;
    [Tooltip("チェックを入れると最終ボタンとUIを最初は非表示にしておきます")]
    public bool hidePhase6FinalButtonAtStart = false;

    // 内部制御用
    private Dictionary<TextMeshProUGUI, string> originalTMPTexts = new Dictionary<TextMeshProUGUI, string>();
    private Dictionary<Text, string> originalNormalTexts = new Dictionary<Text, string>();
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private List<GameObject> currentHighlightedObjects = new List<GameObject>();
    private Dictionary<Graphic, bool> modifiedRaycastTargets = new Dictionary<Graphic, bool>();

    private const int GRAY_BG_SORT_ORDER = 900;
    private const int TOP_UI_SORT_ORDER = 1000;
    private const int BUTTON_HIGHLIGHT_UI_SORT_ORDER = 1150;
    private const int BUTTON_SORT_ORDER = 1200;
    private const int LINKED_OBJ_SORT_ORDER = 1250;
    private const int GACHA_FRONT_SORT_ORDER = 2000;

    void Awake()
    {
        // ★追加：EnergyManager.Start() が呼ばれるよりも前の Awake の段階で、壊されたエネルギーセーブデータを保護・復元する
        RestoreOrBackupEnergyInAwake();

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

        // ★追加：非表示にされたUI内にEnergyManagerがある場合でも、未初期化(0)のままOnDestroyで保存されないよう初期値を同期する
        SyncAllEnergyManagersInScene();

        StartCoroutine(RunTutorialForCurrentScene());
    }

    // ==========================================
    // ★追加：未初期化のEnergyManagerによるエネルギー消失（0上書き→1になる現象）を防ぐ保護メソッド群
    // ==========================================
    private void RestoreOrBackupEnergyInAwake()
    {
        if (PlayerPrefs.HasKey(ENERGY_KEY))
        {
            int currentSaved = PlayerPrefs.GetInt(ENERGY_KEY, 50);

            // 直前のシーンの未初期化EnergyManager.OnDestroy()によって0や1に破壊されていた場合、保護していた値に戻す
            if (currentSaved <= 1 && protectedEnergyValue > 1)
            {
                PlayerPrefs.SetInt(ENERGY_KEY, protectedEnergyValue);
                if (protectedTimerValue > 0f)
                {
                    PlayerPrefs.SetFloat(TIMER_KEY, protectedTimerValue);
                }
                if (!string.IsNullOrEmpty(protectedLastTimeValue))
                {
                    PlayerPrefs.SetString(LAST_TIME_KEY, protectedLastTimeValue);
                }
                PlayerPrefs.Save();
            }
            else if (currentSaved > 1)
            {
                protectedEnergyValue = currentSaved;
                protectedTimerValue = PlayerPrefs.GetFloat(TIMER_KEY, 300f);
                protectedLastTimeValue = PlayerPrefs.GetString(LAST_TIME_KEY, System.DateTime.Now.ToString());
            }
        }
        else if (protectedEnergyValue > 1)
        {
            PlayerPrefs.SetInt(ENERGY_KEY, protectedEnergyValue);
            PlayerPrefs.SetFloat(TIMER_KEY, protectedTimerValue > 0f ? protectedTimerValue : 300f);
            PlayerPrefs.SetString(LAST_TIME_KEY, !string.IsNullOrEmpty(protectedLastTimeValue) ? protectedLastTimeValue : System.DateTime.Now.ToString());
            PlayerPrefs.Save();
        }
        else
        {
            protectedEnergyValue = 50;
            protectedTimerValue = 300f;
            protectedLastTimeValue = System.DateTime.Now.ToString();
        }
    }

    private void SyncAllEnergyManagersInScene()
    {
        EnergyManager[] energyManagers = FindObjectsOfType<EnergyManager>(true);
        if (energyManagers == null || energyManagers.Length == 0) return;

        FieldInfo energyField = typeof(EnergyManager).GetField("currentEnergy", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo timerField = typeof(EnergyManager).GetField("currentTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        int validEnergy = PlayerPrefs.HasKey(ENERGY_KEY) ? PlayerPrefs.GetInt(ENERGY_KEY, 50) : 50;
        if (validEnergy <= 1 && protectedEnergyValue > 1) validEnergy = protectedEnergyValue;
        if (validEnergy <= 0) validEnergy = 50;

        float validTimer = PlayerPrefs.HasKey(TIMER_KEY) ? PlayerPrefs.GetFloat(TIMER_KEY, 300f) : 300f;
        if (validTimer <= 0f) validTimer = 300f;

        foreach (EnergyManager em in energyManagers)
        {
            if (em == null) continue;

            if (energyField != null)
            {
                int curVal = (int)energyField.GetValue(em);
                if (curVal <= 0)
                {
                    energyField.SetValue(em, validEnergy);
                }
                else if (curVal > 1)
                {
                    protectedEnergyValue = curVal;
                }
            }

            if (timerField != null)
            {
                float curTimer = (float)timerField.GetValue(em);
                if (curTimer <= 0f)
                {
                    timerField.SetValue(em, validTimer);
                }
            }
        }
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

            SetActionButtonInitialState(phase2FirstButton, phase2FirstButtonHighlightUI, phase2FirstButtonExtraFrontUI, phase2FirstButtonLinkedObject, hidePhase2FirstButtonAtStart, hidePhase2FirstButtonLinkedObjectUntilActive);
            SetActionButtonInitialState(phase2SinglePullButton, phase2SinglePullHighlightUI, phase2SinglePullExtraFrontUI, phase2SinglePullButtonLinkedObject, hidePhase2SinglePullAtStart, hidePhase2SinglePullButtonLinkedObjectUntilActive);
            SetActionButtonInitialState(phase3MidButton, phase3MidButtonHighlightUI, phase3MidButtonExtraFrontUI, phase3MidButtonLinkedObject, hidePhase3MidButtonAtStart, hidePhase3MidButtonLinkedObjectUntilActive);
            SetActionButtonInitialState(phase3MultiPullButton, phase3MultiPullHighlightUI, phase3MultiPullExtraFrontUI, phase3MultiPullButtonLinkedObject, hidePhase3MultiPullAtStart, hidePhase3MultiPullButtonLinkedObjectUntilActive);
            SetActionButtonInitialState(phase3ReturnHomeButton, phase3ReturnHomeHighlightUI, phase3ReturnHomeExtraFrontUI, phase3ReturnHomeButtonLinkedObject, hidePhase3ReturnHomeAtStart, hidePhase3ReturnHomeButtonLinkedObjectUntilActive);
        }
        else if (CurrentPhase == 4)
        {
            HideElementsIfConfigured(phase4HomeReturnSteps);
            SetActionButtonInitialState(phase4GoToSelectButton, phase4GoToSelectHighlightUI, phase4GoToSelectExtraFrontUI, phase4GoToSelectButtonLinkedObject, hidePhase4GoToSelectAtStart, hidePhase4GoToSelectButtonLinkedObjectUntilActive);
        }
        else if (CurrentPhase == 5)
        {
            HideSelectElementsIfConfigured(phase5SelectSteps);
            SetActionButtonInitialState(phase5BackButton, phase5BackButtonUI, phase5BackButtonExtraFrontUI, phase5BackButtonLinkedObject, hidePhase5BackButtonAtStart, hidePhase5BackButtonLinkedObjectUntilActive);
        }
        else if (CurrentPhase == 6)
        {
            HideElementsIfConfigured(phase6FinalHomeSteps);
            SetActionButtonInitialState(phase6FinalButton, phase6FinalHighlightUI, phase6FinalExtraFrontUI, phase6FinalButtonLinkedObject, hidePhase6FinalButtonAtStart, hidePhase6FinalButtonLinkedObjectUntilActive);
        }
    }

    private void HideElementsIfConfigured(TutorialUIElement[] elements)
    {
        if (elements == null) return;
        foreach (var elem in elements)
        {
            if (elem == null) continue;

            if (elem.uiObjects != null)
            {
                foreach (var item in elem.uiObjects)
                {
                    if (item != null && item.uiObject != null && elem.hideUntilActive && item.hideUntilActive)
                    {
                        item.uiObject.SetActive(false);
                    }
                }
            }

            if (elem.hideUntilActive)
            {
                if (elem.nextButton != null)
                {
                    elem.nextButton.gameObject.SetActive(false);
                }
                if (elem.buttonLinkedObject != null && elem.hideButtonLinkedObjectUntilActive)
                {
                    elem.buttonLinkedObject.SetActive(false);
                }
                if (elem.extraFrontUI != null)
                {
                    elem.extraFrontUI.SetActive(false);
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

            if (elem.uiObjects != null)
            {
                foreach (var item in elem.uiObjects)
                {
                    if (item != null && item.uiObject != null && elem.hideUntilActive && item.hideUntilActive)
                    {
                        item.uiObject.SetActive(false);
                    }
                }
            }

            if (elem.hideUntilActive)
            {
                if (elem.nextButton != null)
                {
                    elem.nextButton.gameObject.SetActive(false);
                }
                if (elem.buttonLinkedObject != null && elem.hideButtonLinkedObjectUntilActive)
                {
                    elem.buttonLinkedObject.SetActive(false);
                }
                if (elem.extraFrontUI != null)
                {
                    elem.extraFrontUI.SetActive(false);
                }
            }
        }
    }

    private void ForceHideStepUIObjects(TutorialUIElement[] elements)
    {
        if (elements == null) return;
        foreach (var elem in elements)
        {
            if (elem == null) continue;

            if (elem.uiObjects != null)
            {
                foreach (var item in elem.uiObjects)
                {
                    if (item != null && item.uiObject != null)
                    {
                        item.uiObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void ForceHideSelectStepUIObjects(WeaponSelectTutorialElement[] elements)
    {
        if (elements == null) return;
        foreach (var elem in elements)
        {
            if (elem == null) continue;

            if (elem.uiObjects != null)
            {
                foreach (var item in elem.uiObjects)
                {
                    if (item != null && item.uiObject != null)
                    {
                        item.uiObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void SetActionButtonInitialState(Button btn, GameObject highlightUI, GameObject extraFrontUI, GameObject linkedObject, bool hideAtStart, bool hideLinkedAtStart)
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
        if (extraFrontUI != null && hideAtStart)
        {
            extraFrontUI.SetActive(false);
        }
        if (linkedObject != null && hideLinkedAtStart)
        {
            linkedObject.SetActive(false);
        }
    }

    private IEnumerator RunTutorialForCurrentScene()
    {
        yield return StartCoroutine(WaitForFadeAlphaZero());
        ShowGrayBackground();

        if (CurrentPhase == 1)
        {
            yield return StartCoroutine(PlayUIElementsSequence(phase1HomeSteps));
            ForceHideStepUIObjects(phase1HomeSteps);

            SyncAllEnergyManagersInScene();
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
            ForceHideStepUIObjects(phase4HomeReturnSteps);

            bool goSelectClicked = false;
            ActivateAndBringButtonToFront(phase4GoToSelectButton, phase4GoToSelectHighlightUI, phase4GoToSelectExtraFrontUI, phase4GoToSelectButtonLinkedObject, () =>
            {
                goSelectClicked = true;
            });

            yield return new WaitUntil(() => goSelectClicked);

            SyncAllEnergyManagersInScene();
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
            ForceHideStepUIObjects(phase6FinalHomeSteps);

            bool finalClicked = false;
            if (phase6FinalButton != null)
            {
                ActivateAndBringButtonToFront(phase6FinalButton, phase6FinalHighlightUI, phase6FinalExtraFrontUI, phase6FinalButtonLinkedObject, () =>
                {
                    finalClicked = true;
                });
                yield return new WaitUntil(() => finalClicked);
            }
            else
            {
                if (phase6FinalHighlightUI != null)
                {
                    phase6FinalHighlightUI.SetActive(true);
                    BringToFront(phase6FinalHighlightUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, true);
                }
                if (phase6FinalExtraFrontUI != null)
                {
                    phase6FinalExtraFrontUI.SetActive(true);
                    BringToFront(phase6FinalExtraFrontUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, true);
                }
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
            ForceHideStepUIObjects(phase2InitialSteps);

            bool firstBtnClicked = false;
            ActivateAndBringButtonToFront(phase2FirstButton, phase2FirstButtonHighlightUI, phase2FirstButtonExtraFrontUI, phase2FirstButtonLinkedObject, () =>
            {
                firstBtnClicked = true;
            });
            yield return new WaitUntil(() => firstBtnClicked);
            if (hidePhase2FirstButtonAtStart && phase2FirstButtonHighlightUI != null) phase2FirstButtonHighlightUI.SetActive(false);
            if (hidePhase2FirstButtonAtStart && phase2FirstButtonExtraFrontUI != null) phase2FirstButtonExtraFrontUI.SetActive(false);
            if (phase2FirstButtonLinkedObject != null) phase2FirstButtonLinkedObject.SetActive(false);
            RestoreHighlightedObjects();

            yield return StartCoroutine(PlayUIElementsSequence(phase2BeforePullSteps));
            ForceHideStepUIObjects(phase2BeforePullSteps);

            SetupForcedSingleGacha();

            bool singlePullClicked = false;
            ActivateAndBringButtonToFront(phase2SinglePullButton, phase2SinglePullHighlightUI, phase2SinglePullExtraFrontUI, phase2SinglePullButtonLinkedObject, () =>
            {
                singlePullClicked = true;
                if (phase2SinglePullButton != null) phase2SinglePullButton.interactable = false;
            });
            yield return new WaitUntil(() => singlePullClicked);
            if (hidePhase2SinglePullAtStart && phase2SinglePullHighlightUI != null) phase2SinglePullHighlightUI.SetActive(false);
            if (hidePhase2SinglePullAtStart && phase2SinglePullExtraFrontUI != null) phase2SinglePullExtraFrontUI.SetActive(false);
            if (phase2SinglePullButtonLinkedObject != null) phase2SinglePullButtonLinkedObject.SetActive(false);
            RestoreHighlightedObjects();

            if (grayBackground != null) grayBackground.SetActive(false);
            yield return StartCoroutine(WaitForGachaBackgroundFadeCycle());
            ShowGrayBackground();

            CurrentPhase = 3;
        }

        if (CurrentPhase == 3)
        {
            yield return StartCoroutine(PlayUIElementsSequence(phase3AfterSingleSteps));
            ForceHideStepUIObjects(phase3AfterSingleSteps);

            bool midBtnClicked = false;
            ActivateAndBringButtonToFront(phase3MidButton, phase3MidButtonHighlightUI, phase3MidButtonExtraFrontUI, phase3MidButtonLinkedObject, () =>
            {
                midBtnClicked = true;
            });
            yield return new WaitUntil(() => midBtnClicked);
            if (hidePhase3MidButtonAtStart && phase3MidButtonHighlightUI != null) phase3MidButtonHighlightUI.SetActive(false);
            if (hidePhase3MidButtonAtStart && phase3MidButtonExtraFrontUI != null) phase3MidButtonExtraFrontUI.SetActive(false);
            if (phase3MidButtonLinkedObject != null) phase3MidButtonLinkedObject.SetActive(false);
            RestoreHighlightedObjects();

            yield return StartCoroutine(PlayUIElementsSequence(phase3BeforeMultiPullSteps));
            ForceHideStepUIObjects(phase3BeforeMultiPullSteps);

            SetupForcedMultiGacha();

            bool multiPullClicked = false;
            ActivateAndBringButtonToFront(phase3MultiPullButton, phase3MultiPullHighlightUI, phase3MultiPullExtraFrontUI, phase3MultiPullButtonLinkedObject, () =>
            {
                multiPullClicked = true;
                if (phase3MultiPullButton != null) phase3MultiPullButton.interactable = false;
            });
            yield return new WaitUntil(() => multiPullClicked);
            if (hidePhase3MultiPullAtStart && phase3MultiPullHighlightUI != null) phase3MultiPullHighlightUI.SetActive(false);
            if (hidePhase3MultiPullAtStart && phase3MultiPullExtraFrontUI != null) phase3MultiPullExtraFrontUI.SetActive(false);
            if (phase3MultiPullButtonLinkedObject != null) phase3MultiPullButtonLinkedObject.SetActive(false);
            RestoreHighlightedObjects();

            if (grayBackground != null) grayBackground.SetActive(false);
            yield return StartCoroutine(WaitForGachaBackgroundFadeCycle());
            ShowGrayBackground();

            yield return StartCoroutine(PlayUIElementsSequence(phase3AfterMultiSteps));
            ForceHideStepUIObjects(phase3AfterMultiSteps);

            bool returnHomeClicked = false;
            ActivateAndBringButtonToFront(phase3ReturnHomeButton, phase3ReturnHomeHighlightUI, phase3ReturnHomeExtraFrontUI, phase3ReturnHomeButtonLinkedObject, () =>
            {
                returnHomeClicked = true;
            });
            yield return new WaitUntil(() => returnHomeClicked);

            SyncAllEnergyManagersInScene();
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
            float timer = 0f;
            while (timer < 2.0f)
            {
                BringGachaElementsToAbsoluteFront();
                timer += Time.deltaTime;
                yield return null;
            }
            RestoreHighlightedObjects();
            yield break;
        }

        while (!backGroundFadeObject.activeInHierarchy)
        {
            BringGachaElementsToAbsoluteFront();
            yield return null;
        }

        while (backGroundFadeObject.activeInHierarchy)
        {
            BringGachaElementsToAbsoluteFront();
            yield return null;
        }

        RestoreHighlightedObjects();
    }

    private void BringGachaElementsToAbsoluteFront()
    {
        if (gachaOpenAllButtonUI != null && gachaOpenAllButtonUI.activeInHierarchy)
        {
            BringToFront(gachaOpenAllButtonUI, GACHA_FRONT_SORT_ORDER + 10, true);

            if (gachaOpenAllButton == null)
            {
                gachaOpenAllButton = gachaOpenAllButtonUI.GetComponentInChildren<Button>(true);
            }
        }

        if (gachaOpenAllButton != null && gachaOpenAllButton.gameObject.activeInHierarchy)
        {
            gachaOpenAllButton.enabled = true;
            gachaOpenAllButton.interactable = true;
            BringToFront(gachaOpenAllButton.gameObject, GACHA_FRONT_SORT_ORDER + 20, true);
        }

        if (gachaSystem != null)
        {
            FieldInfo[] fields = gachaSystem.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                object val = field.GetValue(gachaSystem);
                if (val == null) continue;

                if (val is Button btn)
                {
                    if (btn != phase2FirstButton && btn != phase2SinglePullButton &&
                        btn != phase3MidButton && btn != phase3MultiPullButton && btn != phase3ReturnHomeButton)
                    {
                        if (btn.gameObject.activeInHierarchy)
                        {
                            btn.enabled = true;
                            btn.interactable = true;
                            BringToFront(btn.gameObject, GACHA_FRONT_SORT_ORDER + 20, true);
                        }
                    }
                }
                else if (val is GameObject gObj)
                {
                    if (gObj != backGroundFadeObject && gObj.activeInHierarchy && gObj.GetComponentInChildren<Button>(true) != null)
                    {
                        BringToFront(gObj, GACHA_FRONT_SORT_ORDER + 15, true);
                    }
                }
                else if (val is IEnumerable<GameObject> objList)
                {
                    foreach (GameObject spawnedObj in objList)
                    {
                        if (spawnedObj != null && spawnedObj.activeInHierarchy)
                        {
                            BringCapsuleOrRewardToFront(spawnedObj);
                        }
                    }
                }
            }
        }

        foreach (GameObject prefab in ForcedCapsulePrefabs)
        {
            if (prefab == null) continue;
            BringSpawnedClonesToFront(prefab.name);
        }
        foreach (GameObject prefab in ForcedRewardPrefabs)
        {
            if (prefab == null) continue;
            BringSpawnedClonesToFront(prefab.name);
        }
    }

    private void BringSpawnedClonesToFront(string prefabName)
    {
        GameObject[] rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject root in rootObjs)
        {
            if (root == null || !root.activeInHierarchy) continue;
            if (root.name.StartsWith(prefabName))
            {
                BringCapsuleOrRewardToFront(root);
            }
        }
    }

    private void BringCapsuleOrRewardToFront(GameObject obj)
    {
        if (obj == null) return;

        if (obj.GetComponent<RectTransform>() != null)
        {
            BringToFront(obj, GACHA_FRONT_SORT_ORDER, true);
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                r.sortingOrder = GACHA_FRONT_SORT_ORDER;
            }
        }
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

                if (step.uiObjects != null)
                {
                    for (int u = 0; u < step.uiObjects.Length; u++)
                    {
                        TutorialUIObjectItem item = step.uiObjects[u];
                        if (item == null || item.uiObject == null) continue;

                        item.uiObject.SetActive(true);
                        BringToFront(item.uiObject, TOP_UI_SORT_ORDER + u);

                        if (step.useTypewriter)
                        {
                            CollectTextComponents(item.uiObject, tmpsToType, textsToType);
                        }
                    }
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
                                BringToFront(weaponChild.gameObject, BUTTON_SORT_ORDER);
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
                    if (step.extraFrontUI != null)
                    {
                        step.extraFrontUI.SetActive(true);
                        BringToFront(step.extraFrontUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, true);
                    }
                    if (step.buttonLinkedObject != null)
                    {
                        step.buttonLinkedObject.SetActive(true);
                        BringToFrontAsNonBlocking(step.buttonLinkedObject, LINKED_OBJ_SORT_ORDER);
                    }

                    bool weaponSelected = false;
                    foreach (GameObject wObj in targetWeaponItems)
                    {
                        Button b = wObj.GetComponentInChildren<Button>(true);
                        if (b == null) b = wObj.AddComponent<Button>();

                        UnityEngine.Events.UnityAction onSelectAction = null;
                        onSelectAction = () =>
                        {
                            if (isTyping) SkipTyping();
                            b.onClick.RemoveListener(onSelectAction);

                            if (step.uiObjects != null)
                            {
                                foreach (var item in step.uiObjects)
                                {
                                    if (item != null && item.uiObject != null) item.uiObject.SetActive(false);
                                }
                            }

                            weaponSelected = true;
                        };
                        b.onClick.AddListener(onSelectAction);
                    }

                    yield return new WaitUntil(() => weaponSelected);
                }
                else if (step.nextButton != null)
                {
                    if (step.extraFrontUI != null)
                    {
                        step.extraFrontUI.SetActive(true);
                        BringToFront(step.extraFrontUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, true);
                    }
                    if (step.buttonLinkedObject != null)
                    {
                        step.buttonLinkedObject.SetActive(true);
                        BringToFrontAsNonBlocking(step.buttonLinkedObject, LINKED_OBJ_SORT_ORDER);
                    }

                    step.nextButton.gameObject.SetActive(true);
                    step.nextButton.interactable = true;
                    BringToFront(step.nextButton.gameObject, BUTTON_SORT_ORDER, true);

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

                        if (step.uiObjects != null)
                        {
                            foreach (var item in step.uiObjects)
                            {
                                if (item != null && item.uiObject != null) item.uiObject.SetActive(false);
                            }
                        }

                        btnClicked = true;
                    };
                    step.nextButton.onClick.AddListener(action);

                    yield return new WaitUntil(() => btnClicked);
                }
                else
                {
                    if (step.extraFrontUI != null)
                    {
                        step.extraFrontUI.SetActive(true);
                        BringToFront(step.extraFrontUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, true);
                    }
                    if (step.buttonLinkedObject != null)
                    {
                        step.buttonLinkedObject.SetActive(true);
                        BringToFrontAsNonBlocking(step.buttonLinkedObject, LINKED_OBJ_SORT_ORDER);
                    }

                    yield return StartCoroutine(WaitForUserAdvanceInput());

                    if (step.uiObjects != null)
                    {
                        foreach (var item in step.uiObjects)
                        {
                            if (item != null && item.uiObject != null) item.uiObject.SetActive(false);
                        }
                    }
                }
            }
        }

        RestoreHighlightedObjects();

        bool backClicked = false;
        ActivateAndBringButtonToFront(phase5BackButton, phase5BackButtonUI, phase5BackButtonExtraFrontUI, phase5BackButtonLinkedObject, () =>
        {
            backClicked = true;
        });

        yield return new WaitUntil(() => backClicked);

        SyncAllEnergyManagersInScene();
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

            if (elem.uiObjects != null)
            {
                for (int u = 0; u < elem.uiObjects.Length; u++)
                {
                    TutorialUIObjectItem item = elem.uiObjects[u];
                    if (item == null || item.uiObject == null) continue;

                    item.uiObject.SetActive(true);
                    BringToFront(item.uiObject, TOP_UI_SORT_ORDER + u);

                    if (elem.useTypewriter)
                    {
                        CollectTextComponents(item.uiObject, tmpsToType, textsToType);
                    }
                }
            }

            if (tmpsToType.Count > 0 || textsToType.Count > 0)
            {
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypewriterRoutine(tmpsToType, textsToType));
            }

            if (elem.nextButton != null)
            {
                if (elem.extraFrontUI != null)
                {
                    elem.extraFrontUI.SetActive(true);
                    BringToFront(elem.extraFrontUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, true);
                }
                if (elem.buttonLinkedObject != null)
                {
                    elem.buttonLinkedObject.SetActive(true);
                    BringToFrontAsNonBlocking(elem.buttonLinkedObject, LINKED_OBJ_SORT_ORDER);
                }

                elem.nextButton.gameObject.SetActive(true);
                elem.nextButton.interactable = true;
                BringToFront(elem.nextButton.gameObject, BUTTON_SORT_ORDER, true);

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

                    if (elem.uiObjects != null)
                    {
                        foreach (var item in elem.uiObjects)
                        {
                            if (item != null && item.uiObject != null) item.uiObject.SetActive(false);
                        }
                    }

                    clicked = true;
                };
                elem.nextButton.onClick.AddListener(action);

                yield return new WaitUntil(() => clicked);
            }
            else
            {
                if (elem.extraFrontUI != null)
                {
                    elem.extraFrontUI.SetActive(true);
                    BringToFront(elem.extraFrontUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, true);
                }
                if (elem.buttonLinkedObject != null)
                {
                    elem.buttonLinkedObject.SetActive(true);
                    BringToFrontAsNonBlocking(elem.buttonLinkedObject, LINKED_OBJ_SORT_ORDER);
                }

                yield return StartCoroutine(WaitForUserAdvanceInput());

                if (elem.uiObjects != null)
                {
                    foreach (var item in elem.uiObjects)
                    {
                        if (item != null && item.uiObject != null) item.uiObject.SetActive(false);
                    }
                }
            }
        }

        RestoreHighlightedObjects();
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

    private void ActivateAndBringButtonToFront(Button btn, GameObject highlightUI, GameObject extraFrontUI, GameObject linkedObject, System.Action onClickCallback)
    {
        if (extraFrontUI != null)
        {
            extraFrontUI.SetActive(true);
            BringToFront(extraFrontUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, true);
        }

        if (highlightUI != null)
        {
            highlightUI.SetActive(true);
            BringToFront(highlightUI, BUTTON_HIGHLIGHT_UI_SORT_ORDER, true);
        }

        if (btn != null)
        {
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            BringToFront(btn.gameObject, BUTTON_SORT_ORDER, true);
        }

        if (linkedObject != null)
        {
            linkedObject.SetActive(true);
            BringToFrontAsNonBlocking(linkedObject, LINKED_OBJ_SORT_ORDER);
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

    private void BringToFrontAsNonBlocking(GameObject obj, int sortOrder)
    {
        if (obj == null) return;

        if (obj.GetComponentInChildren<Button>(true) == null)
        {
            Graphic[] graphics = obj.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in graphics)
            {
                if (g != null && g.raycastTarget)
                {
                    if (!modifiedRaycastTargets.ContainsKey(g))
                    {
                        modifiedRaycastTargets[g] = g.raycastTarget;
                    }
                    g.raycastTarget = false;
                }
            }
        }

        BringToFront(obj, sortOrder, false);
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

        if (needsRaycaster || obj.GetComponentInChildren<Button>(true) != null || obj.GetComponentInParent<Button>(true) != null)
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
        foreach (var kvp in modifiedRaycastTargets)
        {
            if (kvp.Key != null)
            {
                kvp.Key.raycastTarget = kvp.Value;
            }
        }
        modifiedRaycastTargets.Clear();

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