using UnityEngine;

public class UIFloatingAnim : MonoBehaviour
{
    [Header("【アニメーション設定】")]
    [Tooltip("動く方向と距離（Xに10を入れると左右に10ピクセル動く）")]
    public Vector3 moveAmount = new Vector3(15f, 0f, 0f); // 初期設定は左右に15ピクセル

    [Tooltip("動く速さ")]
    public float moveSpeed = 5f;

    private RectTransform rectTransform;
    private Vector3 initialPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // 最初の位置を記憶しておく
        if (rectTransform != null)
        {
            initialPosition = rectTransform.anchoredPosition;
        }
    }

    void Update()
    {
        if (rectTransform == null) return;

        // Mathf.Sin() を使って -1.0 〜 1.0 の間を滑らかに行ったり来たりさせる
        float sinValue = Mathf.Sin(Time.time * moveSpeed);

        // 元の位置から指定した距離だけズラす
        Vector3 newPos = initialPosition + (moveAmount * sinValue);

        rectTransform.anchoredPosition = newPos;
    }
}