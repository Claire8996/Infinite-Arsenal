using UnityEngine;
using UnityEngine.UI; // UI（Image）を操作するために必要
using TMPro;          // TextMeshPro（テキスト）を操作するために必要
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("プレイヤーのステータス")]
    [Tooltip("初期体力（最大HP）")]
    public int maxHealth = 5000;

    // 現在のHP
    private int currentHealth;

    // 防具の体力ボーナスを加味した最終的な最大HP
    private int totalMaxHealth;

    [Header("UI設定")]
    [Tooltip("HPが減る赤いゲージのImage")]
    public Image hpBar;

    [Tooltip("現在のHPを表示するテキスト（5000 / 5000 形式）")]
    public TextMeshProUGUI hpText;

    // =========================================================
    // ステージ画面で現在の体力・防御力・移動速度を数字単体で表示したい場合の参照欄
    // =========================================================
    [Header("ステータス数値テキスト設定（3つ）")]
    [Tooltip("現在の体力を数字だけで表示するテキスト")]
    public TextMeshProUGUI healthValueText;

    [Tooltip("現在の防御力を数字だけで表示するテキスト")]
    public TextMeshProUGUI defenseValueText;

    [Tooltip("現在の移動速度を数字だけで表示するテキスト")]
    public TextMeshProUGUI speedValueText;

    // =========================================================
    // ★追加：ゲームオーバー時のメニューオブジェクトおよびボタン設定
    // =========================================================
    [Header("【ゲームオーバーメニュー設定】")]
    [Tooltip("ゲームオーバーの文字が表示された時に表示する複数のオブジェクト")]
    public GameObject[] gameOverDisplayObjects;

    [Tooltip("HomeSceneに戻るボタン")]
    public Button homeButton;

    [Tooltip("現在のステージをリスタートするボタン")]
    public Button restartButton;

    // ゲームオーバー重複実行防止フラグ
    private bool isDead = false;

    // UI吹き飛ばし計算用構造体
    private class FlyingUIData
    {
        public RectTransform rect;
        public Vector2 velocity;
        public Vector3 angularVelocity;
    }

    void Start()
    {
        // 基本の最大HPをArmorStatsへ同期する
        ArmorStats.BaseMaxHealth = maxHealth;

        // =========================================================
        // 初期体力に防具の「体力増加ボーナス（実数値）」を加算する
        // =========================================================
        totalMaxHealth = maxHealth + ArmorStats.TotalHealthBonus;

        // ゲーム開始時に、現在のHPをボーナス込みの最大HPと同じにする
        currentHealth = totalMaxHealth;

        // 最初のUI表示を更新する
        UpdateUI();

        // ★追加：ゲームオーバー用ボタンのリスナー登録とオブジェクトの初期非表示化
        if (homeButton != null)
        {
            homeButton.onClick.AddListener(OnClickHome);
        }
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnClickRestart);
        }

        if (gameOverDisplayObjects != null)
        {
            foreach (var obj in gameOverDisplayObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }

    // ★追加：ホーム画面へ戻るボタンが押された時の処理
    public void OnClickHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("HomeScene");
    }

    // ★追加：ステージをリスタートするボタンが押された時の処理
    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        // ★追加：TutorialSceneでは敵に攻撃されても体力が減らないようにする
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName.IndexOf("Tutorial", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return;
        }

        // =========================================================
        // 受けるダメージから防具の「防御力ボーナス（実数値）」を引く
        // （防御力が高くてマイナスになりHPが回復してしまうのを防ぐため、最低0にする）
        // =========================================================
        int finalDamage = Mathf.Max(0, damageAmount - ArmorStats.TotalDefenseBonus);

        // ダメージ分だけHPを減らす
        currentHealth -= finalDamage;

        // HPがマイナスにならないよう、0以下なら0にする
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        // ダメージを受けたのでUIの表示を更新する
        UpdateUI();

        // HPが0になったら死亡処理を呼ぶ
        if (currentHealth == 0)
        {
            Die();
        }
    }

    // HPバーとテキストの表示を更新する専用メソッド
    void UpdateUI()
    {
        // 1. バーを減らす処理（0.0〜1.0の割合で指定するため、割り算をする）
        if (hpBar != null && totalMaxHealth > 0)
        {
            hpBar.fillAmount = (float)currentHealth / totalMaxHealth;
        }

        // 2. テキストを更新する処理（例：「5500 / 5500」のように表示）
        if (hpText != null)
        {
            hpText.text = currentHealth.ToString() + " / " + totalMaxHealth.ToString();
        }

        // 3. 3つのステータステキスト（体力・防御力・移動速度）を数字で更新する
        if (healthValueText != null)
        {
            healthValueText.text = currentHealth.ToString();
        }

        if (defenseValueText != null)
        {
            defenseValueText.text = ArmorStats.CurrentTotalDefense.ToString();
        }

        if (speedValueText != null)
        {
            speedValueText.text = ArmorStats.CurrentTotalSpeed.ToString();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("プレイヤーは倒れた……（ゲームオーバー）");

        // プレイヤーの操作を停止
        PlayerMovement moveScript = GetComponent<PlayerMovement>();
        if (moveScript != null) moveScript.enabled = false;

        MouseLook lookScript = GetComponentInChildren<MouseLook>();
        if (lookScript != null) lookScript.enabled = false;

        StartCoroutine(PlayUltimateGameOverEffect());
    }

    // =========================================================
    // 超ド派手ゲームオーバー演出（UI全吹き飛ばし＋UI上での多重ParticleSystem＋グリッチ文字）
    // =========================================================
    private IEnumerator PlayUltimateGameOverEffect()
    {
        // ---------------------------------------------------------
        // 1. 既存の全UI要素を収集（Canvas直下の要素や個別パーツ）
        // ---------------------------------------------------------
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        List<FlyingUIData> flyingUIList = new List<FlyingUIData>();
        List<Vector2> initialAnchoredPositions = new List<Vector2>();

        foreach (Canvas c in allCanvases)
        {
            if (c == null) continue;

            // LayoutGroup等があると吹き飛ばし座標が固定されてしまうため無効化
            LayoutGroup[] layouts = c.GetComponentsInChildren<LayoutGroup>(true);
            foreach (var lg in layouts) if (lg != null) lg.enabled = false;

            // Canvas内のUI要素を取得（ルートCanvas自身は除く）
            RectTransform[] rects = c.GetComponentsInChildren<RectTransform>(false);
            foreach (RectTransform rt in rects)
            {
                if (rt == null || rt.gameObject == c.gameObject) continue;
                if (rt.parent == c.transform || rt.GetComponent<Graphic>() != null)
                {
                    if (rt.parent != c.transform && rt.parent.GetComponent<Graphic>() != null) continue;

                    Vector2 dirFromCenter = rt.anchoredPosition;
                    if (dirFromCenter.sqrMagnitude < 10f)
                    {
                        dirFromCenter = Random.insideUnitCircle.normalized;
                    }
                    else
                    {
                        dirFromCenter.Normalize();
                    }

                    // 外側へ向かう爆風初速 ＋ 上方向への跳ね上がり
                    Vector2 blastVel = dirFromCenter * Random.Range(900f, 1800f) + new Vector2(Random.Range(-400f, 400f), Random.Range(400f, 900f));

                    flyingUIList.Add(new FlyingUIData
                    {
                        rect = rt,
                        velocity = blastVel,
                        angularVelocity = new Vector3(
                            Random.Range(-360f, 360f),
                            Random.Range(-360f, 360f),
                            Random.Range(-720f, 720f)
                        )
                    });
                    initialAnchoredPositions.Add(rt.anchoredPosition);
                }
            }
        }

        // ---------------------------------------------------------
        // 2. UI最前面でParticleSystemと演出UIを描画する専用カメラ＆専用Canvas構築
        // ---------------------------------------------------------
        GameObject fxCamObj = new GameObject("GameOverFX_OverlayCamera");
        Camera fxCam = fxCamObj.AddComponent<Camera>();
        fxCam.clearFlags = CameraClearFlags.Depth;
        fxCam.cullingMask = 1 << 5; // UIレイヤーのみ描画
        fxCam.orthographic = true;
        fxCam.orthographicSize = 5f;
        fxCam.depth = 100f; // どのカメラよりも手前に描画
        fxCam.nearClipPlane = 0.1f;
        fxCam.farClipPlane = 100f;
        fxCamObj.transform.position = new Vector3(0f, 0f, -10f);

        GameObject fxCanvasObj = new GameObject("GameOverFX_Canvas");
        fxCanvasObj.layer = 5;
        Canvas fxCanvas = fxCanvasObj.AddComponent<Canvas>();
        fxCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        fxCanvas.worldCamera = fxCam;
        fxCanvas.planeDistance = 10f;
        fxCanvas.sortingOrder = 32000;

        CanvasScaler scaler = fxCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 暗転＆フラッシュ用背景パネル
        GameObject bgObj = CreateUIObj("BloodDarkBG", fxCanvasObj.transform);
        StretchFullScreen(bgObj.GetComponent<RectTransform>());
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(1f, 0f, 0.1f, 0.85f);

        // ---------------------------------------------------------
        // 3. Phase 1：致死ダメージの瞬間！赤い閃光と全UIの激しいグリッチ振動（0.28秒）
        // ---------------------------------------------------------
        float shakeTime = 0.28f;
        float timer = 0f;
        while (timer < shakeTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / shakeTime;

            bgImg.color = Color.Lerp(new Color(1f, 0.9f, 0.9f, 0.9f), new Color(0.3f, 0f, 0.05f, 0.4f), t);

            for (int i = 0; i < flyingUIList.Count; i++)
            {
                if (flyingUIList[i].rect != null)
                {
                    Vector2 jitter = Random.insideUnitCircle * 35f;
                    flyingUIList[i].rect.anchoredPosition = initialAnchoredPositions[i] + jitter;
                    flyingUIList[i].rect.localEulerAngles = new Vector3(0f, 0f, Random.Range(-12f, 12f));
                }
            }
            yield return null;
        }

        // ---------------------------------------------------------
        // 4. Phase 2：大爆発！UI上のParticleSystem一斉射出 ＆ 既存UI全吹き飛ばし
        // ---------------------------------------------------------
        Material glowMat = CreateAdditiveParticleMaterial(CreateRadialGlowTexture(128));
        Material shardMat = CreateAdditiveParticleMaterial(CreateDiamondShardTexture(64));
        Material ringMat = CreateAdditiveParticleMaterial(CreateRingTexture(256));

        ParticleSystem ringPS = CreateShockwaveParticleSystem(fxCanvasObj.transform, ringMat);
        ringPS.Play();

        ParticleSystem sparkPS = CreateSparkBurstParticleSystem(fxCanvasObj.transform, glowMat, 320);
        sparkPS.Play();

        ParticleSystem shardPS = CreateShatterShardParticleSystem(fxCanvasObj.transform, shardMat, 120);
        shardPS.Play();

        ParticleSystem emberPS = CreateRisingEmberParticleSystem(fxCanvasObj.transform, glowMat);
        emberPS.Play();

        // 上下のシネマティック黒帯
        RectTransform topBar = CreateUIObj("TopCinemaBar", fxCanvasObj.transform).GetComponent<RectTransform>();
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(200f, 0f);
        Image topBarImg = topBar.gameObject.AddComponent<Image>();
        topBarImg.color = new Color(0.02f, 0.0f, 0.01f, 0.95f);

        RectTransform bottomBar = CreateUIObj("BottomCinemaBar", fxCanvasObj.transform).GetComponent<RectTransform>();
        bottomBar.anchorMin = new Vector2(0f, 0f);
        bottomBar.anchorMax = new Vector2(1f, 0f);
        bottomBar.pivot = new Vector2(0.5f, 0f);
        bottomBar.sizeDelta = new Vector2(200f, 0f);
        Image bottomBarImg = bottomBar.gameObject.AddComponent<Image>();
        bottomBarImg.color = new Color(0.02f, 0.0f, 0.01f, 0.95f);

        // 中央を貫く真紅のスラッシュ閃光ライン
        RectTransform slashLine = CreateUIObj("SlashLine", fxCanvasObj.transform).GetComponent<RectTransform>();
        slashLine.sizeDelta = new Vector2(2600f, 18f);
        slashLine.localEulerAngles = new Vector3(0f, 0f, -4f);
        slashLine.localScale = new Vector3(0f, 1f, 1f);
        Image slashImg = slashLine.gameObject.AddComponent<Image>();
        slashImg.color = new Color(1f, 0.15f, 0.2f, 1f);

        // 背後の斜めダーク帯
        RectTransform centerBanner = CreateUIObj("CenterBanner", fxCanvasObj.transform).GetComponent<RectTransform>();
        centerBanner.sizeDelta = new Vector2(2400f, 220f);
        centerBanner.localEulerAngles = new Vector3(0f, 0f, -4f);
        centerBanner.localScale = new Vector3(1f, 0f, 1f);
        Image bannerImg = centerBanner.gameObject.AddComponent<Image>();
        bannerImg.color = new Color(0.05f, 0f, 0.01f, 0.88f);

        // 「GAME OVER」テキスト群
        RectTransform textRoot = CreateUIObj("TextRoot", fxCanvasObj.transform).GetComponent<RectTransform>();
        textRoot.localEulerAngles = new Vector3(0f, 0f, -4f);

        TextMeshProUGUI giantBgText = CreateStyledTMP("GiantBGText", textRoot, "GAME OVER", 250f, new Color(0.5f, 0f, 0.05f, 0f));
        giantBgText.characterSpacing = 12f;

        TextMeshProUGUI glitchCyanText = CreateStyledTMP("GlitchCyan", textRoot, "GAME OVER", 150f, new Color(0f, 0.95f, 1f, 0f));
        TextMeshProUGUI glitchRedText = CreateStyledTMP("GlitchRed", textRoot, "GAME OVER", 150f, new Color(1f, 0f, 0.2f, 0f));
        TextMeshProUGUI mainGoText = CreateStyledTMP("MainGameOver", textRoot, "GAME OVER", 150f, new Color(1f, 1f, 1f, 0f));
        mainGoText.enableVertexGradient = true;
        mainGoText.colorGradient = new VertexGradient(
            new Color(1f, 0.95f, 0.95f),
            new Color(1f, 0.95f, 0.95f),
            new Color(1f, 0.1f, 0.15f),
            new Color(0.8f, 0f, 0.05f)
        );

        // サブテキスト
        TextMeshProUGUI subText = CreateStyledTMP("SubText", textRoot, "—  Y O U   A R E   D E A D  —", 34f, new Color(1f, 0.3f, 0.3f, 0f));
        subText.rectTransform.anchoredPosition = new Vector2(0f, -125f);

        // ---------------------------------------------------------
        // 5. メインアニメーションループ（UI吹き飛び ＆ 文字叩きつけ ＆ 第2波爆発）
        // ---------------------------------------------------------
        float elapsed = 0f;
        float totalDuration = 3.5f;
        bool secondBurstTriggered = false;

        while (elapsed < totalDuration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            float progress = Mathf.Clamp01(elapsed / totalDuration);

            for (int i = 0; i < flyingUIList.Count; i++)
            {
                FlyingUIData item = flyingUIList[i];
                if (item.rect != null)
                {
                    item.velocity += new Vector2(0f, -2800f * dt);
                    item.rect.anchoredPosition += item.velocity * dt;
                    item.rect.localEulerAngles += item.angularVelocity * dt;
                    item.rect.localScale = Vector3.Lerp(item.rect.localScale, Vector3.zero, dt * 0.8f);
                }
            }

            bgImg.color = Color.Lerp(new Color(0.9f, 0.05f, 0.1f, 0.85f), new Color(0.04f, 0f, 0.01f, 0.92f), Mathf.Clamp01(elapsed * 2.2f));
            float barHeight = Mathf.Lerp(0f, 130f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed * 3.0f)));
            topBar.sizeDelta = new Vector2(200f, barHeight);
            bottomBar.sizeDelta = new Vector2(200f, barHeight);

            if (elapsed > 0.15f)
            {
                float slashT = Mathf.Clamp01((elapsed - 0.15f) / 0.25f);
                slashLine.localScale = new Vector3(Mathf.SmoothStep(0f, 1.2f, slashT), Mathf.Lerp(2.5f, 0.15f, slashT), 1f);
                slashImg.color = Color.Lerp(Color.white, new Color(1f, 0.05f, 0.15f, 0.9f), slashT);

                float bannerT = Mathf.Clamp01((elapsed - 0.2f) / 0.25f);
                centerBanner.localScale = new Vector3(1f, Mathf.SmoothStep(0f, 1f, bannerT), 1f);
            }

            if (elapsed > 0.3f)
            {
                float textT = Mathf.Clamp01((elapsed - 0.3f) / 0.22f);
                float slamCurve = 1f - Mathf.Pow(1f - textT, 4f);

                float currentScale = Mathf.Lerp(4.5f, 1.0f, slamCurve);

                if (textT >= 0.95f && !secondBurstTriggered)
                {
                    secondBurstTriggered = true;
                    ringPS.Emit(2);
                    sparkPS.Emit(180);
                    shardPS.Emit(70);
                }

                Vector2 impactShake = Vector2.zero;
                if (elapsed > 0.5f && elapsed < 0.85f)
                {
                    float decay = 1f - ((elapsed - 0.5f) / 0.35f);
                    impactShake = Random.insideUnitCircle * (28f * decay);
                }

                float heartBeat = 1f + Mathf.Sin(elapsed * 8f) * 0.035f;
                textRoot.anchoredPosition = impactShake;
                mainGoText.rectTransform.localScale = Vector3.one * (currentScale * heartBeat);
                mainGoText.color = new Color(1f, 1f, 1f, Mathf.Clamp01(textT * 2.5f));
                mainGoText.characterSpacing = Mathf.Lerp(60f, 8f, slamCurve);

                giantBgText.rectTransform.localScale = Vector3.one * (1.1f + (elapsed - 0.3f) * 0.15f);
                giantBgText.color = new Color(0.7f, 0f, 0.1f, Mathf.Lerp(0.35f, 0.08f, Mathf.Clamp01((elapsed - 0.3f) * 0.8f)));

                bool isGlitchFrame = (Random.value < 0.35f) || (elapsed < 0.75f);
                if (isGlitchFrame)
                {
                    float glitchPower = (elapsed < 0.75f) ? 18f : 7f;
                    glitchCyanText.rectTransform.anchoredPosition = new Vector2(Random.Range(-glitchPower, glitchPower), Random.Range(-4f, 4f));
                    glitchRedText.rectTransform.anchoredPosition = new Vector2(Random.Range(-glitchPower, glitchPower), Random.Range(-4f, 4f));
                    glitchCyanText.rectTransform.localScale = mainGoText.rectTransform.localScale;
                    glitchRedText.rectTransform.localScale = mainGoText.rectTransform.localScale;
                    glitchCyanText.characterSpacing = mainGoText.characterSpacing;
                    glitchRedText.characterSpacing = mainGoText.characterSpacing;
                    glitchCyanText.color = new Color(0f, 0.95f, 1f, 0.75f);
                    glitchRedText.color = new Color(1f, 0f, 0.2f, 0.75f);
                }
                else
                {
                    glitchCyanText.color = Color.clear;
                    glitchRedText.color = Color.clear;
                }
            }

            if (elapsed > 0.75f)
            {
                float subT = Mathf.Clamp01((elapsed - 0.75f) / 0.8f);
                subText.color = new Color(1f, 0.35f, 0.35f, subT);
                subText.characterSpacing = Mathf.Lerp(0f, 18f, Mathf.SmoothStep(0f, 1f, subT));
            }

            yield return null;
        }

        // ---------------------------------------------------------
        // 6. ★追加：ゲームオーバー文字表示＆演出完了後に複数のオブジェクトを表示し、カーソルを有効化
        // ---------------------------------------------------------
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (gameOverDisplayObjects != null)
        {
            foreach (var obj in gameOverDisplayObjects)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // 演出完了後も残り火パーティクルとグリッチが静かに脈打ち続けるループ
        while (true)
        {
            float t = Time.unscaledTime;
            float pulse = 1f + Mathf.Sin(t * 5f) * 0.025f;
            mainGoText.rectTransform.localScale = Vector3.one * pulse;

            if (Random.value < 0.12f)
            {
                glitchCyanText.rectTransform.anchoredPosition = new Vector2(Random.Range(-8f, 8f), Random.Range(-2f, 2f));
                glitchRedText.rectTransform.anchoredPosition = new Vector2(Random.Range(-8f, 8f), Random.Range(-2f, 2f));
                glitchCyanText.color = new Color(0f, 0.95f, 1f, 0.6f);
                glitchRedText.color = new Color(1f, 0f, 0.2f, 0.6f);
            }
            else
            {
                glitchCyanText.color = Color.clear;
                glitchRedText.color = Color.clear;
            }

            yield return null;
        }
    }

    // =========================================================
    // UI・テキスト生成補助メソッド群
    // =========================================================
    private GameObject CreateUIObj(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.layer = 5;
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        return obj;
    }

    private void StretchFullScreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private TextMeshProUGUI CreateStyledTMP(string name, Transform parent, string text, float fontSize, Color color)
    {
        GameObject obj = CreateUIObj(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1800f, 400f);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.color = color;
        return tmp;
    }

    // =========================================================
    // 4種類のParticleSystem動的構築メソッド群（UI最前面で再生）
    // =========================================================
    private ParticleSystem CreateShockwaveParticleSystem(Transform parent, Material mat)
    {
        GameObject obj = CreateUIObj("PS_ShockwaveRing", parent);
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 0.65f;
        main.startSpeed = 0f;
        main.startSize = 1800f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.2f, 0.2f, 0.9f), new Color(1f, 0.8f, 0.6f, 0.9f));

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 2) });

        var shape = ps.shape;
        shape.enabled = false;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.05f);
        sizeCurve.AddKey(0.3f, 0.7f);
        sizeCurve.AddKey(1f, 1.4f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colOverLifetime = ps.colorOverLifetime;
        colOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.1f, 0.2f), 0.4f), new GradientColorKey(new Color(0.5f, 0f, 0f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = mat;
        renderer.sortingOrder = 32010;
        return ps;
    }

    private ParticleSystem CreateSparkBurstParticleSystem(Transform parent, Material mat, int burstCount)
    {
        GameObject obj = CreateUIObj("PS_SparkBurst", parent);
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(400f, 2200f);
        main.startSize = new ParticleSystem.MinMaxCurve(12f, 38f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.5f, 1f), new Color(1f, 0.1f, 0.15f, 1f));
        main.gravityModifier = 40f;
        main.maxParticles = 1000;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 20f;

        var limitVel = ps.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.dampen = 0.08f;
        limitVel.limit = 150f;

        var colOverLifetime = ps.colorOverLifetime;
        colOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f), new GradientColorKey(new Color(1f, 0.2f, 0.1f), 0.5f), new GradientColorKey(new Color(0.4f, 0f, 0.05f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.04f;
        renderer.lengthScale = 3.5f;
        renderer.material = mat;
        renderer.sortingOrder = 32020;
        return ps;
    }

    private ParticleSystem CreateShatterShardParticleSystem(Transform parent, Material mat, int burstCount)
    {
        GameObject obj = CreateUIObj("PS_ShatterShards", parent);
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(300f, 1600f);
        main.startSize = new ParticleSystem.MinMaxCurve(25f, 75f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.05f, 0.15f, 0.95f), new Color(1f, 0.5f, 0.5f, 0.95f));
        main.gravityModifier = 120f;
        main.maxParticles = 500;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 40f;

        var rotOverLifetime = ps.rotationOverLifetime;
        rotOverLifetime.enabled = true;
        rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-10f, 10f);

        var colOverLifetime = ps.colorOverLifetime;
        colOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.9f, 0.05f, 0.1f), 0.3f), new GradientColorKey(new Color(0.2f, 0f, 0f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = mat;
        renderer.sortingOrder = 32015;
        return ps;
    }

    private ParticleSystem CreateRisingEmberParticleSystem(Transform parent, Material mat)
    {
        GameObject obj = CreateUIObj("PS_RisingEmbers", parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, -600f);

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(180f, 420f);
        main.startSize = new ParticleSystem.MinMaxCurve(10f, 36f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.1f, 0.1f, 0.8f), new Color(1f, 0.55f, 0.1f, 0.8f));
        main.maxParticles = 400;

        var emission = ps.emission;
        emission.rateOverTime = 75f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2000f, 10f, 1f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 85f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.5f;

        var colOverLifetime = ps.colorOverLifetime;
        colOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.1f, 0.1f), 0.5f), new GradientColorKey(new Color(0.3f, 0f, 0f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.2f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = mat;
        renderer.sortingOrder = 32005;
        return ps;
    }

    private Material CreateAdditiveParticleMaterial(Texture2D tex)
    {
        Shader shader = Shader.Find("Mobile/Particles/Additive");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.mainTexture = tex;
        return mat;
    }

    private Texture2D CreateRadialGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    private Texture2D CreateDiamondShardTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - half) / (half * 0.45f);
                float dy = Mathf.Abs(y - half) / half;
                float dist = dx + dy;
                float a = Mathf.Clamp01(1f - dist);
                a = Mathf.Pow(a, 1.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    private Texture2D CreateRingTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float ring = 1f - Mathf.Abs(d - 0.82f) / 0.12f;
                float a = Mathf.Clamp01(ring);
                a = a * a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }
}