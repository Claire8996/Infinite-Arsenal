using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StageCarouselItem
{
    [Tooltip("並べて表示するUIオブジェクト（Stage1, Stage2など）")]
    public RectTransform uiObject;

    [Tooltip("このオブジェクトがCenterPositionにある時にボタンを押すと移動するScene名")]
    public string targetSceneName;

    // ==========================================
    // ★追加：CenterPositionの時だけ表示するテキストオブジェクト
    // ==========================================
    [Header("【中央表示テキスト設定】")]
    [Tooltip("このオブジェクトがCenterPositionにある時だけ表示（SetActive(true)）するテキストオブジェクト")]
    public GameObject stageInfoTextObject;
}

public class StageSelectCarousel : MonoBehaviour
{
    [Header("【3つの基準Position設定】")]
    [Tooltip("左側の基準位置（LeftPosition）")]
    public Transform leftPosition;

    [Tooltip("中央の基準位置（CenterPosition）")]
    public Transform centerPosition;

    [Tooltip("右側の基準位置（RightPosition）")]
    public Transform rightPosition;

    [Header("【表示するUIオブジェクトと移動先Sceneの配列】")]
    [Tooltip("設定した順番でCenterPosition → RightPosition → さらに右側へと順々に並びます")]
    public StageCarouselItem[] stageItems;

    [Header("【シーン移動ボタン＆SceneTransitionManager連携】")]
    [Tooltip("現在CenterPositionにあるステージのSceneへ移動するボタン（1つ）")]
    public Button startStageButton;

    [Tooltip("シーン移動を担当する SceneTransitionManager")]
    public SceneTransitionManager sceneTransitionManager;

    [Header("【スライド移動・大きさ（Scale）・色の設定】")]
    [Tooltip("切り替わる時のスライド移動時間（0にすると一瞬で切り替わります）")]
    public float slideDuration = 0.3f;

    [Tooltip("CenterPositionに来た時の大きさ（Scale）")]
    public Vector3 centerScale = new Vector3(1.3f, 1.3f, 1.3f);

    [Tooltip("CenterPosition以外の時の大きさ（Scale）")]
    public Vector3 otherScale = new Vector3(0.8f, 0.8f, 0.8f);

    [Tooltip("CenterPositionにある時の色（通常色）")]
    public Color centerColor = Color.white;

    [Tooltip("CenterPosition以外の時の色（若干灰色）")]
    public Color otherColor = new Color(0.65f, 0.65f, 0.65f, 1f);

    [Header("【マウスホイール設定】")]
    [Tooltip("チェックを入れると、マウスホイールを回した時の移動方向が逆になります")]
    public bool invertScrollDirection = false;

    [Tooltip("ホイールを回した時、スライド移動が完了するまで次のホイール入力を待つか（連続で飛びすぎるのを防ぎます）")]
    public bool waitSlideFinishOnScroll = true;

    // 最後にCenterPositionだったオブジェクトの番号を保存するセーブキー
    private const string SaveKeyCenterIndex = "LastCenterStageIndex";

    // 現在CenterPositionにあるオブジェクトの配列インデックス番号
    private int currentCenterIndex = 0;
    private Coroutine slideCoroutine;

    // 各UIオブジェクトのAlpha値を制御するためのCanvasGroup配列
    private CanvasGroup[] itemCanvasGroups;

    // 各UIオブジェクト内の画像（Graphic）と現在の色を管理する配列
    private Graphic[][] itemGraphics;
    private Color[] currentItemColors;

    void Start()
    {
        if (stageItems == null || stageItems.Length == 0) return;

        // 各オブジェクトのAlpha値と色（灰色変化）を制御するためのコンポーネントを自動準備
        itemCanvasGroups = new CanvasGroup[stageItems.Length];
        itemGraphics = new Graphic[stageItems.Length][];
        currentItemColors = new Color[stageItems.Length];

        for (int i = 0; i < stageItems.Length; i++)
        {
            if (stageItems[i] != null && stageItems[i].uiObject != null)
            {
                CanvasGroup cg = stageItems[i].uiObject.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = stageItems[i].uiObject.gameObject.AddComponent<CanvasGroup>();
                }
                itemCanvasGroups[i] = cg;

                // 子オブジェクトも含めて色を変えられるUI要素（Image等）を取得しておく
                itemGraphics[i] = stageItems[i].uiObject.GetComponentsInChildren<Graphic>(true);
                currentItemColors[i] = centerColor;
            }
        }

        // SceneTransitionManager が未設定ならシーン内から自動で探す
        if (sceneTransitionManager == null)
        {
            sceneTransitionManager = FindObjectOfType<SceneTransitionManager>();
        }

        // シーン移動ボタンが設定されていれば、クリック時の処理を自動登録する
        if (startStageButton != null)
        {
            startStageButton.onClick.RemoveListener(OnClickLoadCurrentCenterScene);
            startStageButton.onClick.AddListener(OnClickLoadCurrentCenterScene);
        }

        // 記憶の復元：最後にCenterPositionだったオブジェクトの番号を読み込む
        currentCenterIndex = PlayerPrefs.GetInt(SaveKeyCenterIndex, 0);
        currentCenterIndex = Mathf.Clamp(currentCenterIndex, 0, stageItems.Length - 1);

        // ゲーム開始時はアニメーションなしで即座に配置・大きさ・色・Alpha・テキスト表示を適用する
        UpdateObjectsPositionImmediate();
    }

    // 毎フレーム、マウスホイールの回転を検知してオブジェクトを移動させる処理
    void Update()
    {
        if (stageItems == null || stageItems.Length == 0) return;

        if (waitSlideFinishOnScroll && slideCoroutine != null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (!invertScrollDirection)
            {
                if (scroll < 0f)
                {
                    MoveToNextObject();
                }
                else if (scroll > 0f)
                {
                    MoveToPreviousObject();
                }
            }
            else
            {
                if (scroll > 0f)
                {
                    MoveToNextObject();
                }
                else if (scroll < 0f)
                {
                    MoveToPreviousObject();
                }
            }
        }
    }

    // =========================================================
    // ボタン用1：現在CenterPositionにあるオブジェクトのSceneへ移動する処理
    // =========================================================
    public void OnClickLoadCurrentCenterScene()
    {
        if (stageItems == null || stageItems.Length == 0) return;
        if (currentCenterIndex < 0 || currentCenterIndex >= stageItems.Length) return;

        string sceneName = stageItems[currentCenterIndex].targetSceneName;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"配列の {currentCenterIndex} 番目のオブジェクトに移動先のScene名が設定されていません！");
            return;
        }

        if (sceneTransitionManager != null)
        {
            sceneTransitionManager.LoadTargetScene(sceneName);
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager が見つかりません！Inspectorに設定してください。");
        }
    }

    // =========================================================
    // ボタン用2：指定した番号（0, 1, 2...）のオブジェクトをCenterPositionにする処理
    // =========================================================
    public void SetCenterObjectByIndex(int targetIndex)
    {
        if (stageItems == null || stageItems.Length == 0) return;
        if (targetIndex < 0 || targetIndex >= stageItems.Length) return;

        currentCenterIndex = targetIndex;

        // 最後にCenterPositionだった番号としてすぐにセーブする
        PlayerPrefs.SetInt(SaveKeyCenterIndex, currentCenterIndex);
        PlayerPrefs.Save();

        // 各オブジェクトを新しい位置・大きさ・色・Alphaへ変化させる
        MoveObjectsToTargetPositions();
    }

    // =========================================================
    // ボタン用3：1つ右（次）のオブジェクトをCenterPositionに持ってくる処理
    // =========================================================
    public void MoveToNextObject()
    {
        if (stageItems == null || stageItems.Length == 0) return;
        if (currentCenterIndex + 1 < stageItems.Length)
        {
            SetCenterObjectByIndex(currentCenterIndex + 1);
        }
    }

    // =========================================================
    // ボタン用4：1つ左（前）のオブジェクトをCenterPositionに持ってくる処理
    // =========================================================
    public void MoveToPreviousObject()
    {
        if (stageItems == null || stageItems.Length == 0) return;
        if (currentCenterIndex - 1 >= 0)
        {
            SetCenterObjectByIndex(currentCenterIndex - 1);
        }
    }

    // 配列の順番に従って、各オブジェクトの目標座標を計算する
    private Vector3 CalculateTargetPosition(int itemIndex)
    {
        if (centerPosition == null) return Vector3.zero;

        int offset = itemIndex - currentCenterIndex;

        if (offset == 0)
        {
            return centerPosition.position;
        }
        else if (offset > 0)
        {
            if (rightPosition != null)
            {
                Vector3 rightStep = rightPosition.position - centerPosition.position;
                return centerPosition.position + (rightStep * offset);
            }
            return centerPosition.position;
        }
        else
        {
            if (leftPosition != null)
            {
                Vector3 leftStep = leftPosition.position - centerPosition.position;
                return centerPosition.position + (leftStep * Mathf.Abs(offset));
            }
            return centerPosition.position;
        }
    }

    // 各オブジェクトの目標Scaleを計算する（Centerは1.3、それ以外は0.8）
    private Vector3 CalculateTargetScale(int itemIndex)
    {
        int offset = itemIndex - currentCenterIndex;
        return (offset == 0) ? centerScale : otherScale;
    }

    // 各オブジェクトの目標色を計算する（Centerは通常色、それ以外は若干灰色）
    private Color CalculateTargetColor(int itemIndex)
    {
        int offset = itemIndex - currentCenterIndex;
        return (offset == 0) ? centerColor : otherColor;
    }

    // 指定したオブジェクト内のUI要素に色を適用する補助メソッド
    private void ApplyColorToItem(int itemIndex, Color targetColor)
    {
        if (currentItemColors != null && itemIndex < currentItemColors.Length)
        {
            currentItemColors[itemIndex] = targetColor;
        }

        if (itemGraphics != null && itemIndex < itemGraphics.Length && itemGraphics[itemIndex] != null)
        {
            foreach (Graphic g in itemGraphics[itemIndex])
            {
                if (g != null)
                {
                    g.color = new Color(targetColor.r, targetColor.g, targetColor.b, g.color.a);
                }
            }
        }
    }

    // 各オブジェクトの目標Alpha値を計算する（Left・Center・Rightにある時は 1、それより外側は 0）
    private float CalculateTargetAlpha(int itemIndex)
    {
        int offset = itemIndex - currentCenterIndex;
        return (Mathf.Abs(offset) <= 1) ? 1f : 0f;
    }

    // 即座にすべてのオブジェクトの位置・大きさ・色・Alpha・テキスト表示を定位置へ反映する
    private void UpdateObjectsPositionImmediate()
    {
        for (int i = 0; i < stageItems.Length; i++)
        {
            if (stageItems[i] != null && stageItems[i].uiObject != null)
            {
                stageItems[i].uiObject.position = CalculateTargetPosition(i);
                stageItems[i].uiObject.localScale = CalculateTargetScale(i);

                ApplyColorToItem(i, CalculateTargetColor(i));

                if (itemCanvasGroups != null && i < itemCanvasGroups.Length && itemCanvasGroups[i] != null)
                {
                    float targetAlpha = CalculateTargetAlpha(i);
                    itemCanvasGroups[i].alpha = targetAlpha;

                    bool isVisible = targetAlpha > 0.01f;
                    itemCanvasGroups[i].interactable = isVisible;
                    itemCanvasGroups[i].blocksRaycasts = isVisible;
                }

                // ==========================================
                // ★追加：CenterPositionにある時だけテキストを表示する
                // ==========================================
                if (stageItems[i].stageInfoTextObject != null)
                {
                    stageItems[i].stageInfoTextObject.SetActive(i == currentCenterIndex);
                }
            }
        }
    }

    // スライドアニメーションでオブジェクトを移動させる
    private void MoveObjectsToTargetPositions()
    {
        if (slideDuration <= 0f)
        {
            UpdateObjectsPositionImmediate();
            return;
        }

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        slideCoroutine = StartCoroutine(SlideRoutine());
    }

    // 移動と同時に「Scale（大きさ）」「色（通常⇔若干灰色）」「Alpha（透明度）」もなめらかに変化させる
    private IEnumerator SlideRoutine()
    {
        // スライドが始まる瞬間に、すべてのテキストを非表示にするか、
        // あるいはスライド完了時に切り替わるようにする（ここでは一瞬でパッと切り替わるように即座に反映します）
        for (int i = 0; i < stageItems.Length; i++)
        {
            if (stageItems[i] != null && stageItems[i].stageInfoTextObject != null)
            {
                stageItems[i].stageInfoTextObject.SetActive(i == currentCenterIndex);
            }
        }

        Vector3[] startPositions = new Vector3[stageItems.Length];
        Vector3[] targetPositions = new Vector3[stageItems.Length];

        Vector3[] startScales = new Vector3[stageItems.Length];
        Vector3[] targetScales = new Vector3[stageItems.Length];

        Color[] startColors = new Color[stageItems.Length];
        Color[] targetColors = new Color[stageItems.Length];

        float[] startAlphas = new float[stageItems.Length];
        float[] targetAlphas = new float[stageItems.Length];

        for (int i = 0; i < stageItems.Length; i++)
        {
            if (stageItems[i] != null && stageItems[i].uiObject != null)
            {
                startPositions[i] = stageItems[i].uiObject.position;
                targetPositions[i] = CalculateTargetPosition(i);

                startScales[i] = stageItems[i].uiObject.localScale;
                targetScales[i] = CalculateTargetScale(i);

                startColors[i] = (currentItemColors != null && i < currentItemColors.Length) ? currentItemColors[i] : centerColor;
                targetColors[i] = CalculateTargetColor(i);

                if (itemCanvasGroups != null && i < itemCanvasGroups.Length && itemCanvasGroups[i] != null)
                {
                    startAlphas[i] = itemCanvasGroups[i].alpha;
                    targetAlphas[i] = CalculateTargetAlpha(i);
                }
            }
        }

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float easeOutT = Mathf.Sin(t * Mathf.PI * 0.5f);

            for (int i = 0; i < stageItems.Length; i++)
            {
                if (stageItems[i] != null && stageItems[i].uiObject != null)
                {
                    // 1. 位置の移動
                    stageItems[i].uiObject.position = Vector3.Lerp(startPositions[i], targetPositions[i], easeOutT);

                    // 2. 大きさ（Scale）の変化（1.3 ⇔ 0.8）
                    stageItems[i].uiObject.localScale = Vector3.Lerp(startScales[i], targetScales[i], easeOutT);

                    // 3. 色の変化（通常色 ⇔ 若干灰色）
                    Color lerpedColor = Color.Lerp(startColors[i], targetColors[i], easeOutT);
                    ApplyColorToItem(i, lerpedColor);

                    // 4. 透明度（Alpha）の変化
                    if (itemCanvasGroups != null && i < itemCanvasGroups.Length && itemCanvasGroups[i] != null)
                    {
                        itemCanvasGroups[i].alpha = Mathf.Lerp(startAlphas[i], targetAlphas[i], easeOutT);
                    }
                }
            }

            yield return null;
        }

        UpdateObjectsPositionImmediate();
        slideCoroutine = null;
    }
}