using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ★追加：UIオブジェクト1つ1つに対して「非表示にするか」を設定できる新しいクラス
[System.Serializable]
public class UIElementSetting
{
    [Tooltip("対象のUIオブジェクト")]
    public GameObject uiObject;

    [Tooltip("チェックを入れるとゲーム開始時に自動で非表示にし、このステップの時だけ表示します")]
    public bool hideAtStart = true;
}

[System.Serializable]
public class TutorialStep
{
    // ★変更：ただのGameObject配列から、設定付きの配列に変更
    public UIElementSetting[] stepUIElements;
}

public class DisablePlayerControl : MonoBehaviour
{
    public static bool IsEventActive = false;

    [Header("UI設定（前半）")]
    public GameObject darkPanel;
    public TutorialStep[] tutorialSteps;

    [Header("UI設定（出現後）")]
    public TutorialStep[] postSpawnSteps;
    public float smokeWaitTime = 1.5f;

    [Header("敵の設定")]
    public GameObject slimePrefab;
    public Transform slimeSpawnPosition;

    [Header("エフェクト設定")]
    public GameObject spawnSmokePrefab;

    private int currentStepIndex = 0;
    private int postStepIndex = 0;

    private enum EventPhase { Tutorial, Spawning, PostSpawnUI, Finished }
    private EventPhase currentPhase = EventPhase.Tutorial;

    private PlayerMovement moveScript;
    private MouseLook lookScript;

    void Start()
    {
        IsEventActive = true;

        // ★追加：設定されたUIの中で「Hide At Start」がオンのものを、ゲーム開始時に全て非表示にする
        HideInitialUIElements(tutorialSteps);
        HideInitialUIElements(postSpawnSteps);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            moveScript = player.GetComponent<PlayerMovement>();
            if (moveScript != null) moveScript.enabled = false;

            lookScript = player.GetComponentInChildren<MouseLook>();
            if (lookScript != null) lookScript.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (darkPanel != null) darkPanel.SetActive(true);

        if (tutorialSteps != null && tutorialSteps.Length > 0)
        {
            currentPhase = EventPhase.Tutorial;
            HighlightCurrentStepUI(tutorialSteps, currentStepIndex);
        }
        else
        {
            StartCoroutine(SpawnSlimeSequence());
        }
    }

    // ★追加：ゲーム開始時にUIを隠すための共通メソッド
    void HideInitialUIElements(TutorialStep[] steps)
    {
        if (steps == null) return;
        foreach (TutorialStep step in steps)
        {
            foreach (UIElementSetting setting in step.stepUIElements)
            {
                if (setting.uiObject != null && setting.hideAtStart)
                {
                    setting.uiObject.SetActive(false);
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (currentPhase == EventPhase.Tutorial)
            {
                NextTutorialStep();
            }
            else if (currentPhase == EventPhase.PostSpawnUI)
            {
                NextPostSpawnStep();
            }
        }
    }

    void NextTutorialStep()
    {
        RemoveHighlightCurrentStepUI(tutorialSteps, currentStepIndex);
        currentStepIndex++;

        if (currentStepIndex < tutorialSteps.Length)
        {
            HighlightCurrentStepUI(tutorialSteps, currentStepIndex);
        }
        else
        {
            StartCoroutine(SpawnSlimeSequence());
        }
    }

    IEnumerator SpawnSlimeSequence()
    {
        currentPhase = EventPhase.Spawning;

        if (darkPanel != null) darkPanel.SetActive(false);

        if (slimePrefab != null && slimeSpawnPosition != null)
        {
            Instantiate(slimePrefab, slimeSpawnPosition.position, slimeSpawnPosition.rotation);
            if (spawnSmokePrefab != null) Instantiate(spawnSmokePrefab, slimeSpawnPosition.position, slimeSpawnPosition.rotation);
        }

        yield return new WaitForSeconds(smokeWaitTime);

        if (postSpawnSteps != null && postSpawnSteps.Length > 0)
        {
            currentPhase = EventPhase.PostSpawnUI;
            if (darkPanel != null) darkPanel.SetActive(true);
            HighlightCurrentStepUI(postSpawnSteps, postStepIndex);
        }
        else
        {
            EndAllEvents();
        }
    }

    void NextPostSpawnStep()
    {
        RemoveHighlightCurrentStepUI(postSpawnSteps, postStepIndex);
        postStepIndex++;

        if (postStepIndex < postSpawnSteps.Length)
        {
            HighlightCurrentStepUI(postSpawnSteps, postStepIndex);
        }
        else
        {
            EndAllEvents();
        }
    }

    void HighlightCurrentStepUI(TutorialStep[] steps, int index)
    {
        foreach (UIElementSetting setting in steps[index].stepUIElements)
        {
            GameObject ui = setting.uiObject;
            if (ui != null)
            {
                // ★追加：このステップが来た時に、非表示設定だったUIを表示する
                if (setting.hideAtStart)
                {
                    ui.SetActive(true);
                }

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

    void RemoveHighlightCurrentStepUI(TutorialStep[] steps, int index)
    {
        foreach (UIElementSetting setting in steps[index].stepUIElements)
        {
            GameObject ui = setting.uiObject;
            if (ui != null)
            {
                GraphicRaycaster raycaster = ui.GetComponent<GraphicRaycaster>();
                if (raycaster != null) Destroy(raycaster);

                Canvas canvas = ui.GetComponent<Canvas>();
                if (canvas != null) Destroy(canvas);

                // ★追加：ステップが終わったら、設定に合わせて再び非表示にする
                if (setting.hideAtStart)
                {
                    ui.SetActive(false);
                }
            }
        }
    }

    void EndAllEvents()
    {
        currentPhase = EventPhase.Finished;

        IsEventActive = false;

        if (darkPanel != null) darkPanel.SetActive(false);

        if (moveScript != null) moveScript.enabled = true;
        if (lookScript != null) lookScript.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}