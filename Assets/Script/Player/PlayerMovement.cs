using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("移動設定")]
    public float walkSpeed = 5f;    // 歩く速度
    public float runSpeed = 10f;    // 走る速度
    public float jumpHeight = 1.2f; // ジャンプの高さ（重力を強くした分、少し低くして自然に調整）
    public float gravity = -25f;    // 重力（-9.81から-25に変更してキビキビ落とす）

    private CharacterController controller;
    private Vector3 velocity;       // 落下などの速度計算用
    private bool isGrounded;        // 接地判定

    void Start()
    {
        // アタッチされているCharacterControllerを取得
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1. 接地判定
        // CharacterControllerの組み込み判定を使用
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            // 地面にしっかり接地させるため、わずかに下向きの力をかけ続ける
            velocity.y = -2f;
        }

        // 2. 移動入力の取得 (W,A,S,D または 矢印キー)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // プレイヤーの向きを基準に移動方向を決定
        Vector3 move = transform.right * x + transform.forward * z;

        // 3. 走る・歩くの速度切り替え (左Shiftキー)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // X, Z軸の移動を実行
        controller.Move(move * currentSpeed * Time.deltaTime);

        // 4. ジャンプ処理 (Spaceキー)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // 物理学の公式 (v = √2gh) を使って必要な上向きの速度を計算
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 5. 重力の適用
        velocity.y += gravity * Time.deltaTime;

        // Y軸の移動（落下・ジャンプ）を実行
        controller.Move(velocity * Time.deltaTime);
    }
}