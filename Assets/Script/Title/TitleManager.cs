using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("【ボタン設定】")]
    [Tooltip("「はじめから」ボタン")]
    public Button newGameButton;
    [Tooltip("「つづきから」ボタン")]
    public Button continueButton;
    [Tooltip("「もどる」ボタン")]
    public Button quitButton;

    [Header("【つづきからボタンのUI（灰色制御用）】")]
    [Tooltip("セーブデータがない時に灰色にしたい「つづきから」ボタンのImageコンポーネント")]
    public Image continueButtonImage;
    [Tooltip("セーブデータがある通常時の色")]
    public Color normalContinueColor = Color.white;
    [Tooltip("セーブデータがない時の灰色")]
    public Color disabledContinueColor = Color.gray;

    [Header("【上書き確認用オブジェクト（はい・いいえ）】")]
    [Tooltip("セーブデータがある状態で「はじめから」を押した時に表示する確認用UI（パネルなど）")]
    public GameObject confirmOverwritePanel;
    [Tooltip("確認パネル内の「はい」ボタン（データ初期化 ＆ チュートリアルへ）")]
    public Button confirmYesButton;
    [Tooltip("確認パネル内の「いいえ」ボタン（確認パネルを閉じる）")]
    public Button confirmNoButton;

    [Header("【シーン移動マネージャー連携】")]
    [Tooltip("シーン遷移を担当する SceneTransitionManager")]
    public SceneTransitionManager sceneTransitionManager;
    [Tooltip("つづきから押した時の移動先 Scene名")]
    public string homeSceneName = "HomeScene";
    [Tooltip("はじめから押した時（または初期化時）の移動先 Scene名")]
    public string tutorialSceneName = "TutorialScene";

    [Header("【初回起動時のフェード無効化設定】")]
    [Tooltip("タイトルシーンにあるフェード用のImageオブジェクト（初回起動時は自動で非表示にします）")]
    public GameObject titleFadePanelObject;

    // ==========================================
    // セーブデータ状態確認・初期化用チェックボックス
    // ==========================================
    [Header("【セーブデータ状態確認・初期化用】")]
    [Tooltip("セーブデータが存在するかどうかを表示します。このチェックを外すとセーブデータが初期化されます。")]
    [SerializeField]
    private bool hasSaveDataInspector = false;

    // セーブデータの存在を判定するためのキー
    private const string SaveKeyWeapons = "SavedInventory_Weapons_ExactCoords";
    private const string SaveKeyEmeralds = "PlayerEmeralds";

    // 起動後初回のタイトル画面かどうかを判定するためのstaticフラグ
    private static bool isFirstLaunchInSession = true;

    // 前回のチェックボックスの状態を保持し、ユーザーの手動変更を正確に検知するための一時変数
    [SerializeField, HideInInspector]
    private bool lastEditorCheckState = false;

    void Start()
    {
        if (isFirstLaunchInSession)
        {
            isFirstLaunchInSession = false;

            if (titleFadePanelObject != null)
            {
                titleFadePanelObject.SetActive(false);
            }
        }

        if (confirmOverwritePanel != null)
        {
            confirmOverwritePanel.SetActive(false);
        }

        if (sceneTransitionManager == null)
        {
            sceneTransitionManager = FindObjectOfType<SceneTransitionManager>();
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnClickNewGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnClickContinue);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnClickQuit);
        }

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.AddListener(OnClickConfirmYes);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.AddListener(OnClickConfirmNo);
        }

        // 起動時に現在のセーブ状態をチェックボックスと監視用に反映
        bool hasSave = HasSaveData();
        hasSaveDataInspector = hasSave;
        lastEditorCheckState = hasSave;

        UpdateContinueButtonState();
    }

    void OnValidate()
    {
        // 製作画面やインスペクター変更時に、ユーザーが「チェックを外した瞬間」を検知して初期化する
        bool actualHasSave = HasSaveData();

        if (!Application.isPlaying)
        {
            // 前回がオン（true）で、今回オフ（false）に手動変更された場合のみ初期化を実行
            if (lastEditorCheckState && !hasSaveDataInspector)
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("【インスペクター操作】チェックが外されたため、すべてのセーブデータを初期化しました。");
            }
            // 逆にセーブデータが存在するのにチェックが外れていた場合は、最新のセーブ状態に合わせてチェックを同期する
            else if (!lastEditorCheckState && actualHasSave && hasSaveDataInspector)
            {
                // そのまま維持
            }
            else
            {
                hasSaveDataInspector = actualHasSave;
            }

            lastEditorCheckState = hasSaveDataInspector;
        }
    }

    void Update()
    {
        // プレイ中にインスペクターのチェックボックスが手動で外された場合を監視する
        bool currentHasSave = HasSaveData();
        if (currentHasSave && !hasSaveDataInspector)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("【インスペクター操作】ゲーム実行中にチェックが外されたため、セーブデータを初期化しました。");
            UpdateContinueButtonState();
        }
    }

    private bool HasSaveData()
    {
        bool hasWeapons = PlayerPrefs.HasKey(SaveKeyWeapons);
        bool hasEmeralds = PlayerPrefs.HasKey(SaveKeyEmeralds);
        return hasWeapons || hasEmeralds;
    }

    private void UpdateContinueButtonState()
    {
        bool hasSave = HasSaveData();
        hasSaveDataInspector = hasSave;
        lastEditorCheckState = hasSave;

        if (continueButton != null)
        {
            continueButton.interactable = hasSave;
        }

        if (continueButtonImage != null)
        {
            Color targetColor = hasSave ? normalContinueColor : disabledContinueColor;
            continueButtonImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, continueButtonImage.color.a);
        }
    }

    public void OnClickNewGame()
    {
        if (HasSaveData())
        {
            if (confirmOverwritePanel != null)
            {
                confirmOverwritePanel.SetActive(true);
            }
        }
        else
        {
            LoadScene(tutorialSceneName);
        }
    }

    public void OnClickContinue()
    {
        if (HasSaveData())
        {
            LoadScene(homeSceneName);
        }
        else
        {
            Debug.Log("セーブデータが存在しません。");
        }
    }

    public void OnClickQuit()
    {
        Debug.Log("ゲームを終了します。");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnClickConfirmYes()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("すべてのセーブデータを初期化しました。");

        if (confirmOverwritePanel != null)
        {
            confirmOverwritePanel.SetActive(false);
        }

        UpdateContinueButtonState();
        LoadScene(tutorialSceneName);
    }

    public void OnClickConfirmNo()
    {
        if (confirmOverwritePanel != null)
        {
            confirmOverwritePanel.SetActive(false);
        }
    }

    private void LoadScene(string sceneName)
    {
        if (sceneTransitionManager != null)
        {
            sceneTransitionManager.LoadTargetScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}