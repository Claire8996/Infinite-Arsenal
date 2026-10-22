using UnityEngine;

public class UIFloatingEffect : MonoBehaviour
{
    [Header("【ふわふわ浮遊設定】")]
    [Tooltip("上下に移動する幅（ピクセル単位）")]
    public float floatAmplitude = 15f;

    [Tooltip("上下移動の速さ（周期の調整）")]
    public float floatSpeed = 2f;

    [Tooltip("ランダムなタイミングで浮遊を開始させる（複数のUIが同じ動きになるのを防ぎます）")]
    public bool randomOffset = true;

    private RectTransform rectTransform;
    private Vector2 startAnchoredPos;
    private float timeOffset;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            startAnchoredPos = rectTransform.anchoredPosition;
        }

        // オブジェクトごとに動きのタイミングをずらす
        if (randomOffset)
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }
        else
        {
            timeOffset = 0f;
        }
    }

    void Update()
    {
        if (rectTransform == null) return;

        // サイン波を使って滑らかな上下のオフセットを計算
        float yOffset = Mathf.Sin((Time.time * floatSpeed) + timeOffset) * floatAmplitude;

        // 初期位置を基準にY座標のみを変化させる
        Vector2 newPos = startAnchoredPos;
        newPos.y += yOffset;
        rectTransform.anchoredPosition = newPos;
    }
}