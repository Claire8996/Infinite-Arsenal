using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("視点移動の設定")]
    public float mouseSensitivity = 100f; // マウス感度
    public Transform playerBody;          // プレイヤー本体のTransform

    private float xRotation = 0f;

    void Start()
    {
        // ゲーム開始時にマウスカーソルを画面中央にロックし、非表示にする
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. マウスの移動量を取得
        // Time.deltaTimeを掛けることで、パソコンの性能に関わらず一定の速度で動くようにします
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2. 上下の視点移動（カメラ自身のX軸回転）計算
        xRotation -= mouseY;
        // 真上や真下を向いた時に、首がぐるんと一回転しないように角度を-90度〜90度に制限
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // カメラの上下の回転を適用
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 3. 左右の視点移動（プレイヤー本体のY軸回転）
        // カメラだけでなく、プレイヤー本体ごと左右に回転させます
        playerBody.Rotate(Vector3.up * mouseX);
    }
}