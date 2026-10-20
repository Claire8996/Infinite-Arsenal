using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class StageCarouselItem
{
    [Tooltip("並べて表示するUIオブジェクト（Stage1, Stage2など）")]
    public RectTransform uiObject;

    [Tooltip("このオブジェクトがCenterPositionにある時にボタンを押すと移動するScene名")]
    public string targetSceneName;

    [Header("【中央表示テキスト設定】")]
    [Tooltip("このオブジェクトがCenterPositionにある時だけ表示（SetActive(true)）するテキストオブジェクト")]
    public GameObject stageInfoTextObject;

    [Header("【ステージクリア状態】")]
    [Tooltip("チェックを入れると、このステージはクリア済み扱いになります（ゲーム内でクリアした際も自動で記憶されます）")]
    public bool isCleared = false;
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

    [Header("【未解放（灰色）ステージ＆ロックUI Prefab設定】")]
    [Tooltip("灰色になるStageCarouselItemのUIに表示する「UIのPrefab」をここにセットしてください")]
    public GameObject lockedStageUIPrefab;

    [Tooltip("クリアしていないステージの『次のステージ』以降に適用する灰色の色味")]
    public Color lockedStageColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    // ==========================================
    // ★追加：プレイボタン自体のUI（Image）をインスペクターから直接参照する設定欄
    // ==========================================
    [Header("【プレイボタンのUI設定】")]
    [Tooltip("灰色や通常色に切り替えたいプレイボタンのUI（Imageコンポーネント）をここにセットしてください")]
    public Image playButtonUIImage;

    [Tooltip("プレイ可能なステージがCenterPositionにある時のプレイボタンUIの色（通常色）")]
    public Color normalPlayButtonColor = Color.white;

    [Tooltip("灰色のステージがCenterPositionに来た時のプレイボタンUIの色（灰色）")]
    public Color lockedPlayButtonColor = Color.gray;

    [Tooltip("追加表示したUIのPrefab（lockedStageUIPrefab）自体も一緒に灰色にする場合はチェックを入れてください")]
    public bool tintLockedUIPrefabToo = false;

    [Header("【灰色にしないステージ名テキストの設定】")]
    [Tooltip("ここに登録したテキストコンポーネントは、ステージがロックされても灰色になりません")]
    public List<Graphic> excludeGraphicsFromTint = new List<Graphic>();

    [Header("【マウスホイール設定】")]
    [Tooltip("チェックを入れると、マウスホイールを回した時の移動方向が逆になります")]
    public bool invertScrollDirection = false;

    [Tooltip("ホイールを回した時、スライド移動が完了するまで次のホイール入力を待つか（連続で飛びすぎるのを防ぎます）")]
    public bool waitSlideFinishOnScroll = true;

    private const string SaveKeyCenterIndex = "LastCenterStageIndex";
    private const string SaveKeyStageClearedPrefix = "StageCleared_";

    private int currentCenterIndex = 0;
    private Coroutine slideCoroutine;

    private CanvasGroup[] itemCanvasGroups;
    private Graphic[][] itemGraphics;
    private Color[] currentItemColors;

    private GameObject[] spawnedLockedUIs;

    void Start()
    {
        if (stageItems == null || stageItems.Length == 0) return;

        LoadStageClearStates();

        itemCanvasGroups = new CanvasGroup[stageItems.Length];
        itemGraphics = new Graphic[stageItems.Length][];
        currentItemColors = new Color[stageItems.Length];
        spawnedLockedUIs = new GameObject[stageItems.Length];

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

                if (!tintLockedUIPrefabToo)
                {
                    itemGraphics[i] = stageItems[i].uiObject.GetComponentsInChildren<Graphic>(true);
                }

                if (lockedStageUIPrefab != null)
                {
                    GameObject lockedUI = Instantiate(lockedStageUIPrefab, stageItems[i].uiObject, false);
                    lockedUI.transform.SetAsLastSibling();
                    spawnedLockedUIs[i] = lockedUI;
                }

                if (tintLockedUIPrefabToo)
                {
                    itemGraphics[i] = stageItems[i].uiObject.GetComponentsInChildren<Graphic>(true);
                }

                currentItemColors[i] = centerColor;
            }
        }

        if (sceneTransitionManager == null)
        {
            sceneTransitionManager = FindObjectOfType<SceneTransitionManager>();
        }

        if (startStageButton != null)
        {
            startStageButton.transition = Selectable.Transition.None;
            startStageButton.onClick.RemoveListener(OnClickLoadCurrentCenterScene);
            startStageButton.onClick.AddListener(OnClickLoadCurrentCenterScene);
        }

        currentCenterIndex = PlayerPrefs.GetInt(SaveKeyCenterIndex, 0);
        currentCenterIndex = Mathf.Clamp(currentCenterIndex, 0, stageItems.Length - 1);

        UpdateObjectsPositionImmediate();
    }

    void Update()
    {
        if (stageItems == null || stageItems.Length == 0) return;

        if (waitSlideFinishOnScroll && slideCoroutine != null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (!invertScrollDirection)
            {
                if (scroll < 0f) MoveToNextObject();
                else if (scroll > 0f) MoveToPreviousObject();
            }
            else
            {
                if (scroll > 0f) MoveToNextObject();
                else if (scroll < 0f) MoveToPreviousObject();
            }
        }
    }

    public bool IsStageLocked(int itemIndex)
    {
        if (stageItems == null || stageItems.Length == 0) return false;

        int firstUnclearedIndex = stageItems.Length;
        for (int i = 0; i < stageItems.Length; i++)
        {
            if (stageItems[i] != null && !stageItems[i].isCleared)
            {
                firstUnclearedIndex = i;
                break;
            }
        }

        return itemIndex > firstUnclearedIndex;
    }

    private void LoadStageClearStates()
    {
        if (stageItems == null) return;

        for (int i = 0; i < stageItems.Length; i++)
        {
            if (stageItems[i] == null) continue;

            bool savedCleared = PlayerPrefs.GetInt(SaveKeyStageClearedPrefix + i, 0) == 1;
            if (!string.IsNullOrEmpty(stageItems[i].targetSceneName))
            {
                savedCleared = savedCleared || (PlayerPrefs.GetInt(SaveKeyStageClearedPrefix + stageItems[i].targetSceneName, 0) == 1);
            }

            if (stageItems[i].isCleared)
            {
                PlayerPrefs.SetInt(SaveKeyStageClearedPrefix + i, 1);
                if (!string.IsNullOrEmpty(stageItems[i].targetSceneName))
                {
                    PlayerPrefs.SetInt(SaveKeyStageClearedPrefix + stageItems[i].targetSceneName, 1);
                }
            }
            else if (savedCleared)
            {
                stageItems[i].isCleared = true;
            }
        }
        PlayerPrefs.Save();
    }

    public static void MarkStageClearedBySceneName(string clearedSceneName)
    {
        if (string.IsNullOrEmpty(clearedSceneName)) return;
        PlayerPrefs.SetInt(SaveKeyStageClearedPrefix + clearedSceneName, 1);
        PlayerPrefs.Save();
    }

    // =========================================================
    // ★修正：インスペクターで指定したプレイボタンのUI（Image）の色を切り替える処理
    // =========================================================
    private void UpdatePlayButtonState()
    {
        bool isCenterStageLocked = IsStageLocked(currentCenterIndex);

        if (startStageButton != null)
        {
            startStageButton.interactable = !isCenterStageLocked;
        }

        // インスペクターで設定したプレイボタンのUI画像の色を通常色または灰色に変更する
        if (playButtonUIImage != null)
        {
            Color targetBtnColor = isCenterStageLocked ? lockedPlayButtonColor : normalPlayButtonColor;
            playButtonUIImage.color = new Color(targetBtnColor.r, targetBtnColor.g, targetBtnColor.b, playButtonUIImage.color.a);
        }
    }

    private void UpdateLockedUIPrefabsVisibility()
    {
        if (spawnedLockedUIs == null) return;

        for (int i = 0; i < spawnedLockedUIs.Length; i++)
        {
            if (spawnedLockedUIs[i] != null)
            {
                bool isLocked = IsStageLocked(i);
                spawnedLockedUIs[i].SetActive(isLocked);
            }
        }
    }

    public void OnClickLoadCurrentCenterScene()
    {
        if (stageItems == null || stageItems.Length == 0) return;
        if (currentCenterIndex < 0 || currentCenterIndex >= stageItems.Length) return;

        if (IsStageLocked(currentCenterIndex))
        {
            Debug.Log("前のステージをまだクリアしていないため、このステージはまだプレイできません！");
            return;
        }

        string sceneName = stageItems[currentCenterIndex].targetSceneName;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"配列の {currentCenterIndex} 番目のオブジェクトに移動先のScene名が設定されていません！");
            return;
        }

        if (sceneTransitionManager != null)
        {
            bool canPlay = true;
            if (sceneTransitionManager.energyManager != null)
            {
                canPlay = sceneTransitionManager.energyManager.HasEnoughEnergy(sceneTransitionManager.requiredEnergy);
            }

            if (canPlay)
            {
                MissionManager.AddStagePlay(1);
            }

            sceneTransitionManager.LoadTargetScene(sceneName);
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager が見つかりません！Inspectorに設定してください。");
        }
    }

    public void SetCenterObjectByIndex(int targetIndex)
    {
        if (stageItems == null || stageItems.Length == 0) return;
        if (targetIndex < 0 || targetIndex >= stageItems.Length) return;

        currentCenterIndex = targetIndex;

        PlayerPrefs.SetInt(SaveKeyCenterIndex, currentCenterIndex);
        PlayerPrefs.Save();

        MoveObjectsToTargetPositions();
    }

    public void MoveToNextObject()
    {
        if (stageItems == null || stageItems.Length == 0) return;
        if (currentCenterIndex + 1 < stageItems.Length)
        {
            SetCenterObjectByIndex(currentCenterIndex + 1);
        }
    }

    public void MoveToPreviousObject()
    {
        if (stageItems == null || stageItems.Length == 0) return;
        if (currentCenterIndex - 1 >= 0)
        {
            SetCenterObjectByIndex(currentCenterIndex - 1);
        }
    }

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

    private Vector3 CalculateTargetScale(int itemIndex)
    {
        int offset = itemIndex - currentCenterIndex;
        return (offset == 0) ? centerScale : otherScale;
    }

    private Color CalculateTargetColor(int itemIndex)
    {
        if (IsStageLocked(itemIndex))
        {
            return lockedStageColor;
        }

        int offset = itemIndex - currentCenterIndex;
        return (offset == 0) ? centerColor : otherColor;
    }

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
                    if (excludeGraphicsFromTint != null && excludeGraphicsFromTint.Contains(g))
                    {
                        continue;
                    }

                    if (g is Text || g is TextMeshProUGUI)
                    {
                        continue;
                    }

                    g.color = new Color(targetColor.r, targetColor.g, targetColor.b, g.color.a);
                }
            }
        }
    }

    private float CalculateTargetAlpha(int itemIndex)
    {
        int offset = itemIndex - currentCenterIndex;
        return (Mathf.Abs(offset) <= 1) ? 1f : 0f;
    }

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

                if (stageItems[i].stageInfoTextObject != null)
                {
                    stageItems[i].stageInfoTextObject.SetActive(i == currentCenterIndex);
                }
            }
        }

        UpdateLockedUIPrefabsVisibility();
        UpdatePlayButtonState();
    }

    private void MoveObjectsToTargetPositions()
    {
        UpdateLockedUIPrefabsVisibility();
        UpdatePlayButtonState();

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

    private IEnumerator SlideRoutine()
    {
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
                    stageItems[i].uiObject.position = Vector3.Lerp(startPositions[i], targetPositions[i], easeOutT);
                    stageItems[i].uiObject.localScale = Vector3.Lerp(startScales[i], targetScales[i], easeOutT);

                    Color lerpedColor = Color.Lerp(startColors[i], targetColors[i], easeOutT);
                    ApplyColorToItem(i, lerpedColor);

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