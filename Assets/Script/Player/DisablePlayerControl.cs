using UnityEngine;
using UnityEngine.UI;

// チュートリアルの1ステップ（ページ）を管理するためのクラス
[System.Serializable]
public class TutorialStep
{
    [Tooltip("このステップで『暗転パネルの手前に出したい』UIオブジェクト")]
    public GameObject[] stepUIElements;
}

public class DisablePlayerControl : MonoBehaviour
{
    [Header("UI設定")]
    [Tooltip("画面を暗くするためのパネル（UI）")]
    public GameObject darkPanel;

    [Tooltip("チュートリアルの進行（各ステップごとに手前に出すUIを設定）")]
    public TutorialStep[] tutorialSteps;

    [Header("敵の設定")]
    [Tooltip("チュートリアル終了時に出現させるスライムのプレハブ")]
    public GameObject slimePrefab; // ★変更：プレハブを割り当てる枠に変更

    [Tooltip("スライムを出現させる位置（空のオブジェクトなどを指定）")]
    public Transform slimeSpawnPosition;

    private int currentStepIndex = 0;
    private bool isTutorialActive = false;

    private PlayerMovement moveScript;
    private MouseLook lookScript;

    void Start()
    {
        // 1. プレイヤーの操作を無効化し、スクリプトを変数に保存
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            moveScript = player.GetComponent<PlayerMovement>();
            if (moveScript != null) moveScript.enabled = false;

            lookScript = player.GetComponentInChildren<MouseLook>();
            if (lookScript != null) lookScript.enabled = false;
        }

        // 2. マウスカーソルを表示する
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. 暗転パネルを表示
        if (darkPanel != null) darkPanel.SetActive(true);

        // 4. チュートリアル開始
        if (tutorialSteps != null && tutorialSteps.Length > 0)
        {
            isTutorialActive = true;
            HighlightCurrentStepUI();
        }
        else
        {
            EndTutorial();
        }
    }

    void Update()
    {
        // チュートリアル中、Enterキーが押されたら次のステップへ進む
        if (isTutorialActive && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            NextStep();
        }
    }

    void NextStep()
    {
        // 今手前に出しているUIを、元の位置（暗転パネルの奥）に戻す
        RemoveHighlightCurrentStepUI();

        // ステップを1つ進める
        currentStepIndex++;

        // まだ次のステップがある場合はそれを手前に出す
        if (currentStepIndex < tutorialSteps.Length)
        {
            HighlightCurrentStepUI();
        }
        else
        {
            // 全てのステップが終了した場合はチュートリアルを終える
            EndTutorial();
        }
    }

    void HighlightCurrentStepUI()
    {
        foreach (GameObject ui in tutorialSteps[currentStepIndex].stepUIElements)
        {
            if (ui != null)
            {
                Canvas canvas = ui.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = ui.AddComponent<Canvas>();
                    ui.AddComponent<GraphicRaycaster>();
                }
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
            }
        }
    }

    void RemoveHighlightCurrentStepUI()
    {
        foreach (GameObject ui in tutorialSteps[currentStepIndex].stepUIElements)
        {
            if (ui != null)
            {
                GraphicRaycaster raycaster = ui.GetComponent<GraphicRaycaster>();
                if (raycaster != null) Destroy(raycaster);

                Canvas canvas = ui.GetComponent<Canvas>();
                if (canvas != null) Destroy(canvas);
            }
        }
    }

    // チュートリアル終了時の処理
    void EndTutorial()
    {
        isTutorialActive = false;

        // 暗転用パネルを非表示
        if (darkPanel != null) darkPanel.SetActive(false);

        // プレイヤーの操作（移動と視点）を再び有効化する
        if (moveScript != null) moveScript.enabled = true;
        if (lookScript != null) lookScript.enabled = true;

        // マウスカーソルをロックして非表示に戻す
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ★変更：チュートリアルが終わったらプレハブからスライムを生成する
        if (slimePrefab != null && slimeSpawnPosition != null)
        {
            Instantiate(slimePrefab, slimeSpawnPosition.position, slimeSpawnPosition.rotation);
        }
        else if (slimePrefab != null)
        {
            // 万が一位置指定を忘れていた場合は原点(0,0,0)に出現させる
            Instantiate(slimePrefab);
        }
    }
}