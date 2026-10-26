using UnityEngine;

public class UIBounce : MonoBehaviour
{
    [Header("【バウンド設定】")]
    [Tooltip("移動する幅（ピクセル単位）")]
    public float floatAmplitude = 20f;

    [Tooltip("運動の速さ（周期）")]
    public float floatSpeed = 3.5f;

    [Header("【かっこいい演出（しなり・ストレッチ）設定】")]
    [Tooltip("端でUIが伸び縮みする強さ（スケールの変化量）")]
    public float stretchAmount = 0.15f;

    [Tooltip("回転の揺れを加えるかどうか")]
    public bool enableTilt = true;

    [Tooltip("揺れる時の傾き角度（度）")]
    public float tiltAngle = 2.0f;

    private RectTransform rectTransform;
    private Vector2 initialAnchoredPos;
    private Vector3 initialScale;
    private float initialRotationZ;
    private float randomOffset;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            initialAnchoredPos = rectTransform.anchoredPosition;
            initialScale = rectTransform.localScale;
            initialRotationZ = rectTransform.localEulerAngles.z; // エフェクトで設定した向きの角度を保持
        }

        // 複数のUIを配置したときに動きが同期しないよう、ランダムなオフセットを与える
        randomOffset = Random.Range(0f, 10f);
    }

    void Update()
    {
        if (rectTransform == null) return;

        float time = Time.time * floatSpeed + randomOffset;

        // サイン波を少し歪ませて「タメ（重心移動）」のある滑らかな動きを作る
        float smoothSin = Mathf.Sin(time + Mathf.Sin(time) * 0.5f);

        // ★修正：矢印が向いている角度（initialRotationZ）の方向に沿って前後にスライドするように計算
        float rad = initialRotationZ * Mathf.Deg2Rad;
        Vector2 moveDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 newPos = initialAnchoredPos + moveDirection * (smoothSin * floatAmplitude);
        rectTransform.anchoredPosition = newPos;

        // --- スクイッシュ＆ストレッチ（端で「ペコン」と潰れて伸びるかっこいいしなり） ---
        float cosVal = Mathf.Abs(Mathf.Cos(time));

        float scaleX = initialScale.x * (1f - (cosVal * stretchAmount * 0.5f));
        float scaleY = initialScale.y * (1f + (cosVal * stretchAmount));
        rectTransform.localScale = new Vector3(scaleX, scaleY, initialScale.z);

        // --- わずかな傾き（初期角度 ＋ 揺れ） ---
        if (enableTilt)
        {
            float tiltZ = initialRotationZ + (Mathf.Sin(time * 0.5f) * tiltAngle);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
        }
    }
}