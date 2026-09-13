using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class UIElementSetting
{
    [Tooltip("対象のUIオブジェクト")]
    public GameObject uiObject;
    public bool hideAtStart = true;
}

[System.Serializable]
public class TutorialStep
{
    public UIElementSetting[] stepUIElements;
}

[System.Serializable]
public class EnemyWave
{
    public TutorialStep[] preSpawnUI;
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPositions;
    public TutorialStep[] postSpawnUI;
}

public class DisablePlayerControl : MonoBehaviour
{
    public static bool IsEventActive = false;

    [Header("初期チュートリアル設定")]
    public GameObject darkPanel;
    public TutorialStep[] initialTutorialSteps;

    [Header("ウェーブ設定")]
    public EnemyWave[] enemyWaves;

    [Header("エフェクト設定")]
    public GameObject spawnSmokePrefab;
    public float smokeWaitTime = 1.5f;

    [Header("クリア後の脱出・宝箱設定")]
    public Transform chestLookTarget;
    public ParticleSystem clearParticle;
    public Transform exitPoint;
    public float exitTriggerRadius = 3.0f;
    public GameObject exitUIText;

    [Header("宝箱開封の演出設定")]
    public Transform cinematicCameraPoint;
    public Transform chestLid;
    public TutorialStep[] clearRewardSteps;
    public string nextSceneName;

    private int currentStepIndex = 0;
    private int currentWaveIndex = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private enum EventPhase
    {
        InitialTutorial, PreSpawnUI, Spawning, PostSpawnUI, Battling,
        ForcedLook, GoalReady, CinematicOpening, ClearRewardUI
    }
    private EventPhase currentPhase = EventPhase.InitialTutorial;

    private Transform playerTransform;
    private PlayerMovement moveScript;
    private MouseLook lookScript;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            moveScript = player.GetComponent<PlayerMovement>();
            lookScript = player.GetComponentInChildren<MouseLook>();
        }

        if (exitUIText != null) exitUIText.SetActive(false);

        HideInitialUIElements(initialTutorialSteps);
        HideInitialUIElements(clearRewardSteps);
        if (enemyWaves != null)
        {
            foreach (var wave in enemyWaves)
            {
                HideInitialUIElements(wave.preSpawnUI);
                HideInitialUIElements(wave.postSpawnUI);
            }
        }

        if (initialTutorialSteps != null && initialTutorialSteps.Length > 0)
        {
            currentPhase = EventPhase.InitialTutorial;
            SetEventMode(true);
            currentStepIndex = 0;
            HighlightCurrentStepUI(initialTutorialSteps, currentStepIndex);
        }
        else
        {
            StartWave(0);
        }
    }

    void HideInitialUIElements(TutorialStep[] steps)
    {
        if (steps == null) return;
        foreach (TutorialStep step in steps)
        {
            foreach (UIElementSetting setting in step.stepUIElements)
            {
                if (setting.uiObject != null && setting.hideAtStart) setting.uiObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (IsEventActive && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            if (currentPhase == EventPhase.InitialTutorial)
                AdvanceStep(initialTutorialSteps, () => StartWave(0));
            else if (currentPhase == EventPhase.PreSpawnUI)
                AdvanceStep(enemyWaves[currentWaveIndex].preSpawnUI, () => StartCoroutine(SpawnWaveSequence(enemyWaves[currentWaveIndex])));
            else if (currentPhase == EventPhase.PostSpawnUI)
                AdvanceStep(enemyWaves[currentWaveIndex].postSpawnUI, () => StartBattle());
            else if (currentPhase == EventPhase.ClearRewardUI)
                AdvanceStep(clearRewardSteps, LoadNextScene);
        }

        if (currentPhase == EventPhase.Battling)
        {
            activeEnemies.RemoveAll(enemy => enemy == null);

            if (activeEnemies.Count == 0)
            {
                currentWaveIndex++;
                if (currentWaveIndex < enemyWaves.Length)
                {
                    StartWave(currentWaveIndex);
                }
                else
                {
                    StartCoroutine(AllEnemiesClearedSequence());
                }
            }
        }

        if (currentPhase == EventPhase.GoalReady)
        {
            if (playerTransform != null && exitPoint != null)
            {
                float distanceToExit = Vector3.Distance(playerTransform.position, exitPoint.position);
                if (distanceToExit <= exitTriggerRadius)
                {
                    if (exitUIText != null) exitUIText.SetActive(true);
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (exitUIText != null) exitUIText.SetActive(false);
                        StartCoroutine(CinematicChestOpen());
                    }
                }
                else
                {
                    if (exitUIText != null) exitUIText.SetActive(false);
                }
            }
        }
    }

    IEnumerator AllEnemiesClearedSequence()
    {
        currentPhase = EventPhase.ForcedLook;
        SetEventMode(true);
        if (darkPanel != null) darkPanel.SetActive(false);

        Camera mainCam = Camera.main;
        if (chestLookTarget != null && mainCam != null && playerTransform != null)
        {
            Quaternion startCamRot = mainCam.transform.rotation;
            Vector3 dirToTarget = (chestLookTarget.position - mainCam.transform.position).normalized;
            Quaternion targetLookRot = Quaternion.LookRotation(dirToTarget);

            float t = 0;
            float duration = 1.0f;

            while (t < 1f)
            {
                float smoothT = Mathf.SmoothStep(0, 1, t);
                mainCam.transform.rotation = Quaternion.Slerp(startCamRot, targetLookRot, smoothT);
                t += Time.deltaTime / duration;
                yield return null;
            }
            mainCam.transform.rotation = targetLookRot;
        }

        // ★修正：確実に振り向いた「後」に、光を脱出ポイントに移動させてから再生する
        if (clearParticle != null && exitPoint != null)
        {
            clearParticle.transform.position = exitPoint.position;
            clearParticle.Play();
        }

        yield return new WaitForSeconds(0.5f);
        currentPhase = EventPhase.GoalReady;
        SetEventMode(false);
    }

    IEnumerator CinematicChestOpen()
    {
        currentPhase = EventPhase.CinematicOpening;
        IsEventActive = true;
        if (moveScript != null) moveScript.enabled = false;
        if (lookScript != null) lookScript.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Camera mainCam = Camera.main;

        if (cinematicCameraPoint != null && mainCam != null)
        {
            Vector3 startPos = mainCam.transform.position;
            Quaternion startRot = mainCam.transform.rotation;

            float t = 0;
            float duration = 1.5f;

            while (t < 1f)
            {
                float smoothT = Mathf.SmoothStep(0, 1, t);
                mainCam.transform.position = Vector3.Lerp(startPos, cinematicCameraPoint.position, smoothT);
                mainCam.transform.rotation = Quaternion.Slerp(startRot, cinematicCameraPoint.rotation, smoothT);
                t += Time.deltaTime / duration;
                yield return null;
            }
            mainCam.transform.position = cinematicCameraPoint.position;
            mainCam.transform.rotation = cinematicCameraPoint.rotation;
        }

        // ★大改修：宝箱が確実に開くように計算方法を変更し、アニメーターの干渉を防ぐ
        if (chestLid != null)
        {
            // 宝箱にAnimatorがついていたら強制的にオフにする
            Animator anim = chestLid.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
            Animator parentAnim = chestLid.parent?.GetComponent<Animator>();
            if (parentAnim != null) parentAnim.enabled = false;

            Vector3 startEuler = chestLid.localEulerAngles;

            float t = 0;
            float duration = 0.5f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                // X軸だけを現在の角度から -105.96度 に向かって安全に回転させる
                float currentX = Mathf.LerpAngle(startEuler.x, -105.96f, t);
                chestLid.localEulerAngles = new Vector3(currentX, startEuler.y, startEuler.z);
                yield return null;
            }
            chestLid.localEulerAngles = new Vector3(-105.96f, startEuler.y, startEuler.z);
        }

        yield return new WaitForSeconds(1.0f);

        if (clearRewardSteps != null && clearRewardSteps.Length > 0)
        {
            currentPhase = EventPhase.ClearRewardUI;
            currentStepIndex = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (darkPanel != null) darkPanel.SetActive(true);
            HighlightCurrentStepUI(clearRewardSteps, currentStepIndex);
        }
        else
        {
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
    }

    void AdvanceStep(TutorialStep[] steps, System.Action onComplete)
    {
        RemoveHighlightCurrentStepUI(steps, currentStepIndex);
        currentStepIndex++;

        if (currentStepIndex < steps.Length) HighlightCurrentStepUI(steps, currentStepIndex);
        else onComplete?.Invoke();
    }

    void StartWave(int index)
    {
        SetEventMode(true);
        var wave = enemyWaves[index];
        currentStepIndex = 0;

        if (wave.preSpawnUI != null && wave.preSpawnUI.Length > 0)
        {
            currentPhase = EventPhase.PreSpawnUI;
            HighlightCurrentStepUI(wave.preSpawnUI, currentStepIndex);
        }
        else
        {
            StartCoroutine(SpawnWaveSequence(wave));
        }
    }

    IEnumerator SpawnWaveSequence(EnemyWave wave)
    {
        currentPhase = EventPhase.Spawning;
        if (darkPanel != null) darkPanel.SetActive(false);
        activeEnemies.Clear();

        for (int i = 0; i < wave.enemyPrefabs.Length; i++)
        {
            if (wave.enemyPrefabs[i] != null && i < wave.spawnPositions.Length && wave.spawnPositions[i] != null)
            {
                Transform spawnPoint = wave.spawnPositions[i];
                GameObject enemy = Instantiate(wave.enemyPrefabs[i], spawnPoint.position, spawnPoint.rotation);
                activeEnemies.Add(enemy);
                if (spawnSmokePrefab != null) Instantiate(spawnSmokePrefab, spawnPoint.position, spawnPoint.rotation);
            }
        }

        yield return new WaitForSeconds(smokeWaitTime);

        if (wave.postSpawnUI != null && wave.postSpawnUI.Length > 0)
        {
            currentPhase = EventPhase.PostSpawnUI;
            if (darkPanel != null) darkPanel.SetActive(true);
            currentStepIndex = 0;
            HighlightCurrentStepUI(wave.postSpawnUI, currentStepIndex);
        }
        else
        {
            StartBattle();
        }
    }

    void StartBattle()
    {
        currentPhase = EventPhase.Battling;
        SetEventMode(false);
    }

    void SetEventMode(bool isEvent)
    {
        IsEventActive = isEvent;
        if (darkPanel != null) darkPanel.SetActive(isEvent);
        if (moveScript != null) moveScript.enabled = !isEvent;
        if (lookScript != null) lookScript.enabled = !isEvent;
        Cursor.lockState = isEvent ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isEvent;
    }

    void HighlightCurrentStepUI(TutorialStep[] steps, int index)
    {
        foreach (UIElementSetting setting in steps[index].stepUIElements)
        {
            GameObject ui = setting.uiObject;
            if (ui != null)
            {
                if (setting.hideAtStart) ui.SetActive(true);
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
                if (setting.hideAtStart) ui.SetActive(false);
            }
        }
    }
}