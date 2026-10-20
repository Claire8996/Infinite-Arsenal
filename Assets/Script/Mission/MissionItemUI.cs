using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionItemUI : MonoBehaviour
{
    [Header("【テキスト参照設定】")]
    [Tooltip("ミッション内容を表示するテキスト（TextMeshPro）")]
    public TextMeshProUGUI missionDescriptionTMP;
    [Tooltip("（標準Textを使う場合）ミッション内容を表示するテキスト")]
    public Text missionDescriptionText;

    [Tooltip("報酬のエメラルド数を表示するテキスト（TextMeshPro）")]
    public TextMeshProUGUI rewardEmeraldTMP;
    [Tooltip("（標準Textを使う場合）報酬のエメラルド数を表示するテキスト")]
    public Text rewardEmeraldText;

    // ==========================================
    // 現在の進行度と目標数値を表示するテキスト設定（例：5 / 15）
    // ==========================================
    [Header("【進行度表示テキスト設定】")]
    [Tooltip("現在の進行度と目標数値を表示するテキスト（TextMeshPro）（例：5 / 15）")]
    public TextMeshProUGUI progressTMP;
    [Tooltip("（標準Textを使う場合）現在の進行度と目標数値を表示するテキスト")]
    public Text progressText;

    [Header("【ボタン＆色切り替え設定】")]
    [Tooltip("ミッションクリア時に押せる報酬受取ボタン")]
    public Button claimButton;

    [Tooltip("未クリア時は灰色、クリア時は通常色にするボタンのUI（Image）")]
    public Image claimButtonUIImage;

    [Tooltip("ボタンを押した時に削除（Destroy）するボタンのUIオブジェクト（未設定ならclaimButtonのGameObjectを削除します）")]
    public GameObject buttonUIObjectToDestroy;

    [Tooltip("ミッションクリア時（通常）のボタンの色")]
    public Color clearedButtonColor = Color.white;

    [Tooltip("ミッション未クリア時のボタンの色（灰色）")]
    public Color unclearedButtonColor = Color.gray;

    [Header("【ボタンが押された後に切り替わるUI】")]
    [Tooltip("ボタンが押されたら表示（または生成）されるクリア済みUIオブジェクト")]
    public GameObject completedUIObject;

    // ==========================================
    // ★追加：ボタンを押した時にエメラルドのテキストに向けて飛んでいくPrefab内のUI
    // ==========================================
    [Header("【テキストに向けて飛んでいくUI設定】")]
    [Tooltip("ミッション完了ボタンを押した時に、エメラルドテキストに向けて飛んでいくPrefab内のUIオブジェクト")]
    public GameObject flyingUIObject;

    // すでに報酬を受け取ってUIが切り替わったかどうか
    public bool IsClaimed { get; private set; } = false;
    // 現在ミッション条件を達成しているかどうか
    public bool IsCleared { get; private set; } = false;

    private Action onClaimCallback;

    public void Setup(string description, int rewardEmeralds, int currentProgress, int targetAmount, bool isCleared, bool isClaimed, GameObject fallbackCompletedUIPrefab, Action onClaim)
    {
        IsCleared = isCleared;
        IsClaimed = isClaimed;
        onClaimCallback = onClaim;

        // 1. ミッション内容テキストの反映
        if (missionDescriptionTMP != null) missionDescriptionTMP.text = description;
        if (missionDescriptionText != null) missionDescriptionText.text = description;

        // 2. エメラルド数テキストの反映
        string emeraldStr = rewardEmeralds.ToString();
        if (rewardEmeraldTMP != null) rewardEmeraldTMP.text = emeraldStr;
        if (rewardEmeraldText != null) rewardEmeraldText.text = emeraldStr;

        // 3. 進行度テキストの反映（例：「5 / 15」）
        string progressStr = currentProgress.ToString() + " / " + targetAmount.ToString();
        if (progressTMP != null) progressTMP.text = progressStr;
        if (progressText != null) progressText.text = progressStr;

        // もしPrefab側にcompletedUIObjectが設定されておらず、MissionManager側にPrefabが設定されていた場合は生成しておく
        if (completedUIObject == null && fallbackCompletedUIPrefab != null && claimButton != null)
        {
            completedUIObject = Instantiate(fallbackCompletedUIPrefab, claimButton.transform.parent);
            completedUIObject.transform.localPosition = claimButton.transform.localPosition;
            completedUIObject.SetActive(false);
        }

        // ボタンの標準カラー遷移で色が上書きされないようにする
        if (claimButton != null)
        {
            claimButton.transition = Selectable.Transition.None;
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClickClaimButton);
        }

        RefreshState(currentProgress, targetAmount, isCleared, isClaimed);
    }

    // 進行状況に合わせてボタンの押下可否・色（灰色／通常色）・UI切り替えを更新する
    public void RefreshState(int currentProgress, int targetAmount, bool isCleared, bool isClaimed)
    {
        IsCleared = isCleared;
        IsClaimed = isClaimed;

        // 進行度テキストを最新に更新
        string progressStr = currentProgress.ToString() + " / " + targetAmount.ToString();
        if (progressTMP != null) progressTMP.text = progressStr;
        if (progressText != null) progressText.text = progressStr;

        // すでにボタンを押して報酬を受け取り済みの場合は、ボタンUIを削除して切り替え後のUIを表示する
        if (IsClaimed)
        {
            if (completedUIObject != null)
            {
                completedUIObject.SetActive(true);
            }

            GameObject targetToDestroy = (buttonUIObjectToDestroy != null)
                ? buttonUIObjectToDestroy
                : (claimButton != null ? claimButton.gameObject : null);

            if (targetToDestroy != null)
            {
                Destroy(targetToDestroy);
            }
            return;
        }

        // まだ受け取っていない場合は、切り替え後のUIを非表示にしておく
        if (completedUIObject != null)
        {
            completedUIObject.SetActive(false);
        }

        // ミッションをクリアしていたらボタンを押せるようにし、未クリアなら押せないようにする
        if (claimButton != null)
        {
            claimButton.interactable = IsCleared;
        }

        // ミッションをクリアしていない時は灰色、クリアしたら通常の色にする
        Color targetColor = IsCleared ? clearedButtonColor : unclearedButtonColor;
        if (claimButtonUIImage != null)
        {
            claimButtonUIImage.color = targetColor;
        }
        else if (claimButton != null && claimButton.image != null)
        {
            claimButton.image.color = targetColor;
        }
    }

    private void OnClickClaimButton()
    {
        if (!IsCleared || IsClaimed) return;

        onClaimCallback?.Invoke();
    }
}