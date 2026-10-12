using UnityEngine;
using UnityEngine.UI;
using TMPro; // もしTextMeshProを使っている場合はこれが必要です

public class EmeraldDisplay : MonoBehaviour
{
    [Header("【エメラルドの数を表示するテキスト】")]
    [Tooltip("標準のTextコンポーネント用")]
    public Text normalText;

    [Tooltip("TextMeshProコンポーネント用（使っている方に登録してください）")]
    public TextMeshProUGUI tmpText;

    [Header("【表示設定】")]
    [Tooltip("数字の前後に文字を入れたい場合（例：「× 」と入れると「× 1000」になる）")]
    public string prefix = "";
    public string suffix = "";

    // 💡 万が一セーブデータがまだ無い時のために、GachaSystemで設定した初期値をここにも入れておく
    [Header("セーブデータが無い場合の初期値")]
    public int defaultEmeralds = 1000;

    void Start()
    {
        UpdateEmeraldDisplay();
    }

    // 💡 （任意）もしこのシーンを開いたままエメラルドが増減することがある場合は、
    // Updateで常に監視するか、このメソッドを外部から呼び出してください。
    void Update()
    {
        // 常に最新の状態を画面に反映させる（処理は非常に軽いのでUpdateでOKです）
        UpdateEmeraldDisplay();
    }

    public void UpdateEmeraldDisplay()
    {
        // GachaSystemで保存している「PlayerEmeralds」のセーブデータを読み込む
        int currentEmeralds = PlayerPrefs.GetInt("PlayerEmeralds", defaultEmeralds);

        // 表示用の文字列を作成
        string displayText = prefix + currentEmeralds.ToString() + suffix;

        // 登録されているテキストUIに文字をセットする
        if (normalText != null)
        {
            normalText.text = displayText;
        }

        if (tmpText != null)
        {
            tmpText.text = displayText;
        }
    }
}