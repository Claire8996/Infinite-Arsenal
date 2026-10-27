using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class WaveData
{
    [Tooltip("このウェーブで出現させる敵のプレハブ")]
    public GameObject[] enemyPrefabs;

    [Tooltip("敵を出現させる位置（上の敵プレハブと同じ順番・数で設定）")]
    public Transform[] spawnPositions;
}

// ==========================================
// 報酬やチュートリアルステップ用のUIおよび報酬設定クラス
// ==========================================
[System.Serializable]
public class WaveUIElementSetting
{
    [Tooltip("対象のUIオブジェクト")]
    public GameObject uiObject;
    public bool hideAtStart = true;
    [Tooltip("チェックを入れると、このUIのテキストを1文字ずつ表示します")]
    public bool useTypewriter = false;
}

[System.Serializable]
public class WaveRewardStep
{
    public WaveUIElementSetting[] stepUIElements;

    [Tooltip("このステップのオブジェクトが表示された時に入手するエメラルド数")]
    public int rewardEmeralds = 0;

    [Tooltip("このステップのオブジェクトが表示された時に入手するコイン数")]
    public int rewardCoins = 0;
}

public class WaveManager : MonoBehaviour
{
    [Header("進行状況UI設定")]
    [Tooltip("剣のアイコンの方のテキスト（例: 1 / 3）")]
    public TextMeshProUGUI waveProgressText;

    [Tooltip("ボスの顔アイコンの方のテキスト（例: 2 / 5）")]
    public TextMeshProUGUI enemyCountText;

    [Header("シーン開始時のUI演出")]
    [Tooltip("表示したいUIのCanvasGroup（透明度操作用）")]
    public CanvasGroup introUIGroup;
    [Tooltip("表示したいUIのTransform（拡大縮小用）")]
    public Transform introUITransform;

    [Header("ウェーブ設定")]
    public WaveData[] enemyWaves;
    [Header("エフェクト設定")]
    public GameObject spawnSmokePrefab;
    public float smokeWaitTime = 1.5f;

    [Header("ボス戦（最終ウェーブ）のワープ設定")]
    [Tooltip("ボス戦突入時にプレイヤーをワープさせる地点")]
    public Transform bossPlayerSpawnPoint;

    [Tooltip("ボス戦突入時にアクティブ（表示）にするオブジェクト（ステージや壁など）")]
    public GameObject bossStageObject;

    [Header("クリア後の脱出・宝箱設定")]
    public Transform chestLookTarget;
    public ParticleSystem clearParticle;
    public Transform exitPoint;
    public float exitTriggerRadius = 3.0f;
    public GameObject exitUIText;

    [Header("宝箱開封の演出設定")]
    public Transform cinematicCameraPoint;
    public Transform chestLid;

    [Header("宝箱開封後に順番に表示する報酬ステップ（エメラルドやコイン設定付き）")]
    public WaveRewardStep[] clearRewardSteps;

    public string nextSceneName;

    private int currentWaveIndex = 0;
    private int currentRewardIndex = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();
    // どの敵Prefabから生成された敵かを保持するリスト（ミッションの撃破判定用）
    private List<GameObject> activeEnemyOriginalPrefabs = new List<GameObject>();
    private int initialEnemyCountForCurrentWave = 0;

    private enum WavePhase
    {
        Intro, Spawning, Battling,
        ForcedLook, GoalReady, CinematicOpening, ClearRewardUI, Finished
    }
    private WavePhase currentPhase = WavePhase.Intro;

    private Transform playerTransform;
    private PlayerMovement moveScript;
    private MouseLook lookScript;

    // タイプライター演出用
    private Dictionary<TextMeshProUGUI, string> originalTexts = new Dictionary<TextMeshProUGUI, string>();
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    public float typeDelay = 0.05f;

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

        // クリア報酬UIの初期非表示化
        if (clearRewardSteps != null)
        {
            foreach (WaveRewardStep step in clearRewardSteps)
            {
                if (step == null || step.stepUIElements == null) continue;
                foreach (WaveUIElementSetting setting in step.stepUIElements)
                {
                    if (setting != null && setting.uiObject != null)
                    {
                        setting.uiObject.SetActive(false);
                    }
                }
            }
        }

        if (bossStageObject != null)
        {
            bossStageObject.SetActive(false);
        }

        if (introUIGroup != null) introUIGroup.alpha = 0f;
        if (introUITransform != null) introUITransform.localScale = Vector3.one * 0.5f;

        UpdateProgressUI(0, 0, 0);

        SetEventMode(false);
        StartCoroutine(IntroUIAnimCoroutine());
    }

    void Update()
    {
        if (currentPhase == WavePhase.Battling)
        {
            // 倒されて消滅（null）した敵を検知し、ミッション進行度（合計撃破＆特定敵撃破）をカウントする
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] == null)
                {
                    GameObject sourcePrefab = (i < activeEnemyOriginalPrefabs.Count) ? activeEnemyOriginalPrefabs[i] : null;
                    MissionManager.AddEnemyKill(sourcePrefab);

                    activeEnemies.RemoveAt(i);
                    if (i < activeEnemyOriginalPrefabs.Count)
                    {
                        activeEnemyOriginalPrefabs.RemoveAt(i);
                    }
                }
            }

            UpdateProgressUI(currentWaveIndex + 1, activeEnemies.Count, initialEnemyCountForCurrentWave);

            if (activeEnemies.Count == 0)
            {
                currentWaveIndex++;
                if (currentWaveIndex < enemyWaves.Length)
                {
                    StartCoroutine(SpawnWaveSequence(enemyWaves[currentWaveIndex]));
                }
                else
                {
                    StartCoroutine(AllEnemiesClearedSequence());
                }
            }
        }

        if (currentPhase == WavePhase.GoalReady)
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

        // ClearRewardUI段階でReturnキー等を押した際のステップ送り＆報酬付与処理
        if (currentPhase == WavePhase.ClearRewardUI && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            if (isTyping)
            {
                SkipTyping();
                return;
            }

            AdvanceClearRewardStep();
        }
    }

    void UpdateProgressUI(int displayWaveNumber, int currentEnemies, int totalEnemies)
    {
        if (waveProgressText != null)
        {
            int maxWaves = enemyWaves.Length;
            waveProgressText.text = displayWaveNumber.ToString() + " / " + maxWaves.ToString();
        }

        if (enemyCountText != null)
        {
            enemyCountText.text = currentEnemies.ToString() + " / " + totalEnemies.ToString();
        }
    }

    IEnumerator IntroUIAnimCoroutine()
    {
        currentPhase = WavePhase.Intro;

        if (introUIGroup == null || introUITransform == null)
        {
            if (enemyWaves.Length > 0) StartCoroutine(SpawnWaveSequence(enemyWaves[0]));
            yield break;
        }

        float t = 0;
        float animDuration = 0.8f;

        while (t < 1f)
        {
            t += Time.deltaTime / animDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            introUIGroup.alpha = smoothT;
            introUITransform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one, smoothT);
            yield return null;
        }

        yield return new WaitForSeconds(3.0f);

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / animDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            introUIGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);
            introUITransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, smoothT);
            yield return null;
        }

        introUIGroup.gameObject.SetActive(false);

        if (enemyWaves.Length > 0)
        {
            StartCoroutine(SpawnWaveSequence(enemyWaves[0]));
        }
    }

    IEnumerator SpawnWaveSequence(WaveData wave)
    {
        currentPhase = WavePhase.Spawning;
        activeEnemies.Clear();
        activeEnemyOriginalPrefabs.Clear();
        initialEnemyCountForCurrentWave = 0;

        bool isBossWave = (currentWaveIndex == enemyWaves.Length - 1);
        if (isBossWave)
        {
            if (bossStageObject != null) bossStageObject.SetActive(true);
            if (bossPlayerSpawnPoint != null && playerTransform != null)
            {
                WarpPlayer(bossPlayerSpawnPoint.position, bossPlayerSpawnPoint.rotation);
            }
        }

        for (int i = 0; i < wave.enemyPrefabs.Length; i++)
        {
            if (wave.enemyPrefabs[i] != null && i < wave.spawnPositions.Length && wave.spawnPositions[i] != null)
            {
                Transform spawnPoint = wave.spawnPositions[i];
                GameObject enemy = Instantiate(wave.enemyPrefabs[i], spawnPoint.position, spawnPoint.rotation);
                activeEnemies.Add(enemy);
                activeEnemyOriginalPrefabs.Add(wave.enemyPrefabs[i]);

                if (spawnSmokePrefab != null) Instantiate(spawnSmokePrefab, spawnPoint.position, spawnPoint.rotation);
            }
        }

        initialEnemyCountForCurrentWave = activeEnemies.Count;
        UpdateProgressUI(currentWaveIndex + 1, activeEnemies.Count, initialEnemyCountForCurrentWave);

        yield return new WaitForSeconds(smokeWaitTime);
        currentPhase = WavePhase.Battling;
    }

    void WarpPlayer(Vector3 targetPosition, Quaternion targetRotation)
    {
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerTransform.position = targetPosition;
        playerTransform.rotation = targetRotation;

        if (cc != null) cc.enabled = true;
    }

    IEnumerator AllEnemiesClearedSequence()
    {
        currentPhase = WavePhase.ForcedLook;
        SetEventMode(true);

        StageSelectCarousel.MarkStageClearedBySceneName(SceneManager.GetActiveScene().name);

        UpdateProgressUI(enemyWaves.Length, 0, initialEnemyCountForCurrentWave);

        Camera mainCam = Camera.main;
        Quaternion startCamRot = Quaternion.identity;
        Quaternion origLocalRot = Quaternion.identity;
        Quaternion targetLookRot = Quaternion.identity;
        bool canRotateCam = (chestLookTarget != null && mainCam != null && playerTransform != null);

        if (canRotateCam)
        {
            startCamRot = mainCam.transform.rotation;
            origLocalRot = mainCam.transform.localRotation;
            Vector3 dirToTarget = (chestLookTarget.position - mainCam.transform.position).normalized;
            targetLookRot = Quaternion.LookRotation(dirToTarget);

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

        if (clearParticle != null && exitPoint != null)
        {
            clearParticle.transform.position = exitPoint.position;
            clearParticle.Play();
        }

        // ★修正：DisablePlayerControlスクリプトと同様に、パーティクル（光等）がしっかり発光し切るまで1.5秒待機してから視点を戻す
        yield return new WaitForSeconds(1.5f);

        if (canRotateCam)
        {
            float t = 0;
            float duration = 1.0f;

            while (t < 1f)
            {
                float smoothT = Mathf.SmoothStep(0, 1, t);
                mainCam.transform.rotation = Quaternion.Slerp(targetLookRot, startCamRot, smoothT);
                t += Time.deltaTime / duration;
                yield return null;
            }
            mainCam.transform.rotation = startCamRot;
            mainCam.transform.localRotation = origLocalRot;
        }

        currentPhase = WavePhase.GoalReady;
        SetEventMode(false);
    }

    IEnumerator CinematicChestOpen()
    {
        currentPhase = WavePhase.CinematicOpening;
        SetEventMode(true);

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

        if (chestLid != null)
        {
            Animator[] anims = chestLid.GetComponentsInParent<Animator>();
            foreach (Animator anim in anims) anim.enabled = false;

            Quaternion startRot = chestLid.localRotation;
            Quaternion endRot = Quaternion.Euler(-105.96f, chestLid.localEulerAngles.y, chestLid.localEulerAngles.z);

            float t = 0;
            float duration = 0.5f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                chestLid.localRotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            chestLid.localRotation = endRot;
        }

        yield return new WaitForSeconds(1.0f);

        // クリア報酬ステップが存在する場合はUI表示＆報酬付与を開始
        if (clearRewardSteps != null && clearRewardSteps.Length > 0)
        {
            currentPhase = WavePhase.ClearRewardUI;
            currentRewardIndex = 0;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            HighlightClearRewardStep(currentRewardIndex);
        }
        else
        {
            LoadNextScene();
        }
    }

    // クリア報酬ステップのUI表示・タイプライター・報酬付与処理
    void HighlightClearRewardStep(int index)
    {
        if (clearRewardSteps == null || index < 0 || index >= clearRewardSteps.Length) return;

        WaveRewardStep step = clearRewardSteps[index];
        List<TextMeshProUGUI> textsToType = new List<TextMeshProUGUI>();

        if (step.stepUIElements != null)
        {
            foreach (WaveUIElementSetting setting in step.stepUIElements)
            {
                if (setting == null) continue;
                GameObject ui = setting.uiObject;
                if (ui != null)
                {
                    ui.SetActive(true);
                    Canvas canvas = ui.GetComponent<Canvas>();
                    if (canvas == null)
                    {
                        canvas = ui.AddComponent<Canvas>();
                        ui.AddComponent<GraphicRaycaster>();
                    }
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 100;

                    if (setting.useTypewriter)
                    {
                        TextMeshProUGUI[] tmpros = ui.GetComponentsInChildren<TextMeshProUGUI>();
                        foreach (var tmp in tmpros)
                        {
                            if (!originalTexts.ContainsKey(tmp))
                            {
                                originalTexts[tmp] = tmp.text;
                            }
                            textsToType.Add(tmp);
                        }
                    }
                }
            }
        }

        GrantClearStepRewards(step);

        if (textsToType.Count > 0)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypewriterCoroutine(textsToType));
        }
    }

    void AdvanceClearRewardStep()
    {
        if (clearRewardSteps == null || currentRewardIndex < 0 || currentRewardIndex >= clearRewardSteps.Length) return;

        // 現在のステップのUIを非表示にする
        WaveRewardStep currentStep = clearRewardSteps[currentRewardIndex];
        if (currentStep.stepUIElements != null)
        {
            foreach (WaveUIElementSetting setting in currentStep.stepUIElements)
            {
                if (setting != null && setting.uiObject != null)
                {
                    GraphicRaycaster raycaster = setting.uiObject.GetComponent<GraphicRaycaster>();
                    if (raycaster != null) Destroy(raycaster);
                    Canvas canvas = setting.uiObject.GetComponent<Canvas>();
                    if (canvas != null) Destroy(canvas);
                    setting.uiObject.SetActive(false);
                }
            }
        }

        currentRewardIndex++;

        if (currentRewardIndex < clearRewardSteps.Length)
        {
            HighlightClearRewardStep(currentRewardIndex);
        }
        else
        {
            currentPhase = WavePhase.Finished;
            LoadNextScene();
        }
    }

    void GrantClearStepRewards(WaveRewardStep step)
    {
        if (step == null) return;
        bool isSaved = false;

        if (step.rewardEmeralds > 0)
        {
            EmeraldDisplay emeraldDisplay = FindObjectOfType<EmeraldDisplay>();
            int defaultEmeralds = (emeraldDisplay != null) ? emeraldDisplay.defaultEmeralds : 0;
            int currentEmeralds = PlayerPrefs.GetInt("PlayerEmeralds", defaultEmeralds);
            int newEmeralds = currentEmeralds + step.rewardEmeralds;
            PlayerPrefs.SetInt("PlayerEmeralds", newEmeralds);
            isSaved = true;

            if (emeraldDisplay != null)
            {
                emeraldDisplay.UpdateEmeraldDisplay();
            }
        }

        if (step.rewardCoins > 0)
        {
            CoinDisplay coinDisplay = FindObjectOfType<CoinDisplay>();
            int defaultCoins = (coinDisplay != null) ? coinDisplay.defaultCoins : 0;
            int currentCoins = PlayerPrefs.GetInt("PlayerCoins", defaultCoins);
            int newCoins = currentCoins + step.rewardCoins;
            PlayerPrefs.SetInt("PlayerCoins", newCoins);
            isSaved = true;

            if (coinDisplay != null)
            {
                coinDisplay.UpdateCoinDisplay();
            }
        }

        if (isSaved)
        {
            PlayerPrefs.Save();
        }
    }

    IEnumerator TypewriterCoroutine(List<TextMeshProUGUI> textComps)
    {
        isTyping = true;

        foreach (var t in textComps) t.text = "";

        int maxLength = 0;
        foreach (var t in textComps)
        {
            if (originalTexts.ContainsKey(t) && originalTexts[t].Length > maxLength)
            {
                maxLength = originalTexts[t].Length;
            }
        }

        for (int i = 0; i < maxLength; i++)
        {
            foreach (var t in textComps)
            {
                if (originalTexts.ContainsKey(t) && i < originalTexts[t].Length)
                {
                    t.text += originalTexts[t][i];
                }
            }
            yield return new WaitForSeconds(typeDelay);
        }

        isTyping = false;
    }

    void SkipTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        foreach (var kvp in originalTexts)
        {
            if (kvp.Key != null && kvp.Key.gameObject.activeInHierarchy)
            {
                kvp.Key.text = kvp.Value;
            }
        }
        isTyping = false;
    }

    void SetEventMode(bool isEvent)
    {
        DisablePlayerControl.IsEventActive = isEvent;

        if (moveScript != null) moveScript.enabled = !isEvent;
        if (lookScript != null) lookScript.enabled = !isEvent;

        Cursor.lockState = isEvent ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isEvent;
    }

    void LoadNextScene()
    {
        StageSelectCarousel.MarkStageClearedBySceneName(SceneManager.GetActiveScene().name);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}