using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmeraldLinkedText : MonoBehaviour
{
    [Header("【現在のエメラルド数を表示するテキスト】")]
    [Tooltip("TextMeshProを使う場合はこちらにセットしてください（未設定でも同じオブジェクトにあれば自動取得します）")]
    public TextMeshProUGUI tmpText;

    [Tooltip("標準のTextを使う場合はこちらにセットしてください（未設定でも同じオブジェクトにあれば自動取得します）")]
    public Text normalText;

    [Header("【表示フォーマット設定】")]
    [Tooltip("数字の前に付ける文字（例：「× 」）")]
    public string prefix = "";

    [Tooltip("数字の後に付ける文字（例：「 個」）")]
    public string suffix = "";

    [Header("【初期値設定（セーブデータ未作成時用）】")]
    [Tooltip("EmeraldDisplay側と合わせた初期エメラルド数")]
    public int defaultEmeralds = 1000;

    // 前回表示した数値を記憶（無駄なテキスト更新処理を抑えるため）
    private int lastDisplayedEmeralds = int.MinValue;

    void Awake()
    {
        // インスペクターで未割り当ての場合、同じオブジェクトから自動取得する
        if (tmpText == null)
        {
            tmpText = GetComponent<TextMeshProUGUI>();
        }
        if (normalText == null)
        {
            normalText = GetComponent<Text>();
        }
    }

    void Start()
    {
        UpdateLinkedText(true);
    }

    void Update()
    {
        UpdateLinkedText(false);
    }

    // 最新のエメラルド数を取得してテキストへ反映するメソッド
    public void UpdateLinkedText(bool forceUpdate = false)
    {
        // EmeraldDisplay側の共有システムから現在のエメラルド数を取得
        int currentEmeralds = EmeraldDisplay.GetCurrentEmeralds(defaultEmeralds);

        if (!forceUpdate && currentEmeralds == lastDisplayedEmeralds)
        {
            return;
        }

        lastDisplayedEmeralds = currentEmeralds;
        string displayStr = prefix + currentEmeralds.ToString() + suffix;

        if (tmpText != null)
        {
            tmpText.text = displayStr;
        }

        if (normalText != null)
        {
            normalText.text = displayStr;
        }
    }
}