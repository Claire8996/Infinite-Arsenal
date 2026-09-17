using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    // ==========================================
    // ★追加：ボス戦（最終ウェーブ）専用設定
    // ==========================================
    [Header("ボス戦（最終ウェーブ）のワープ設定")]
    [Tooltip("ボス戦突入時にプレイヤーをワープさせる地点")]
    public Transform bossPlayerSpawnPoint;

    [Tooltip("ボス戦突入時にアクティブ（表示）にするオブジェクト（ステージや壁など）")]
    public GameObject bossStageObject;
    // ==========================================

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
        // プレイヤーの情報を取得
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            moveScript = player.GetComponent<PlayerMovement>();
            lookScript = player.GetComponentInChildren<MouseLook>();
        }

        // 初期設定：UIやエフェクトを隠す
        if (exitUIText != null) exitUIText.SetActive(false);
        if (clearRewardUIs != null)
        {
            foreach (GameObject ui in clearRewardUIs)
            {
                if (ui != null) ui.SetActive(false);
            }
        }

        // ★追加：ボスステージ用オブジェクトを最初は非表示にしておく
        if (bossStageObject != null)
        {
            bossStageObject.SetActive(false);
        }

        if (introUIGroup != null) introUIGroup.alpha = 0f;
        if (introUITransform != null) introUITransform.localScale = Vector3.one * 0.5f;

        // ゲーム開始時にUI演出をスタート（この間プレイヤーは動ける）
        SetEventMode(false);
        StartCoroutine(IntroUIAnimCoroutine());
    }

    void Update()
    {
        // バトル中の全滅チェック
        if (currentPhase == WavePhase.Battling)
        {
            activeEnemies.RemoveAll(enemy => enemy == null);

            if (activeEnemies.Count == 0)
            {
                currentWaveIndex++;
                if (currentWaveIndex < enemyWaves.Length)
                {
                    StartCoroutine(SpawnWaveSequence(enemyWaves[currentWaveIndex]));
                }
                else
                {
                    // すべてのウェーブをクリアしたら、宝箱への注目演出へ
                    StartCoroutine(AllEnemiesClearedSequence());
                }
            }
        }

        // Eキーで宝箱を開ける判定
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

        // クリア報酬UIをEnterキーで進める処理
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

        // =========================================================
        // ★追加：最後のウェーブ（ボス戦）の場合のワープ＆オブジェクト表示
        // =========================================================
        bool isBossWave = (currentWaveIndex == enemyWaves.Length - 1);
        if (isBossWave)
        {
            // 1. ボスステージのオブジェクトを表示
            if (bossStageObject != null)
            {
                bossStageObject.SetActive(true);
            }

            // 2. プレイヤーを指定の場所へワープ
            if (bossPlayerSpawnPoint != null && playerTransform != null)
            {
                WarpPlayer(bossPlayerSpawnPoint.position, bossPlayerSpawnPoint.rotation);
            }
        }
        // =========================================================

        // 敵と煙のスポーン
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
        currentPhase = WavePhase.Battling;
    }

    // ★追加：CharacterControllerの干渉を受けずに確実にワープさせる専用メソッド
    void WarpPlayer(Vector3 targetPosition, Quaternion targetRotation)
    {
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // 一時的にオフ

        playerTransform.position = targetPosition;
        playerTransform.rotation = targetRotation;

        if (cc != null) cc.enabled = true; // オンに戻す
    }

    IEnumerator AllEnemiesClearedSequence()
    {
        currentPhase = WavePhase.ForcedLook;
        SetEventMode(true);

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
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}