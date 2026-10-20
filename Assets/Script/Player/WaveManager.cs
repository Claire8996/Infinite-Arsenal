using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    [Tooltip("宝箱が開いた後に順番に表示するUI（複数指定可）")]
    public GameObject[] clearRewardUIs;

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
        if (clearRewardUIs != null)
        {
            foreach (GameObject ui in clearRewardUIs)
            {
                if (ui != null) ui.SetActive(false);
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

        if (currentPhase == WavePhase.ClearRewardUI)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (clearRewardUIs[currentRewardIndex] != null)
                {
                    clearRewardUIs[currentRewardIndex].SetActive(false);
                }

                currentRewardIndex++;

                if (currentRewardIndex < clearRewardUIs.Length)
                {
                    if (clearRewardUIs[currentRewardIndex] != null)
                    {
                        clearRewardUIs[currentRewardIndex].SetActive(true);
                    }
                }
                else
                {
                    currentPhase = WavePhase.Finished;
                    LoadNextScene();
                }
            }
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

        // ★追加：このステージの敵をすべて倒したので、現在のScene名をクリア済みとして記録・解放する
        StageSelectCarousel.MarkStageClearedBySceneName(SceneManager.GetActiveScene().name);

        UpdateProgressUI(enemyWaves.Length, 0, initialEnemyCountForCurrentWave);

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

        if (clearParticle != null && exitPoint != null)
        {
            clearParticle.transform.position = exitPoint.position;
            clearParticle.Play();
        }

        yield return new WaitForSeconds(0.5f);
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

        if (clearRewardUIs != null && clearRewardUIs.Length > 0)
        {
            currentPhase = WavePhase.ClearRewardUI;
            currentRewardIndex = 0;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (clearRewardUIs[currentRewardIndex] != null)
            {
                clearRewardUIs[currentRewardIndex].SetActive(true);
            }
        }
        else
        {
            LoadNextScene();
        }
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
        // ★追加：シーン移動直前にも確実に現在のScene名をクリア済みとして記録する
        StageSelectCarousel.MarkStageClearedBySceneName(SceneManager.GetActiveScene().name);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}