using UnityEngine;

public class SlimeAI : MonoBehaviour
{
    [Header("スライムの設定")]
    public float moveSpeed = 2.0f;       // 歩くスピード
    public float attackRange = 1.5f;     // 攻撃を開始する距離
    public float attackCooldown = 2.0f;  // 次の攻撃までの待機時間
    public int damage = 10;              // プレイヤーに与えるダメージ

    private Transform player;
    private Animator animator;
    private float lastAttackTime;
    private bool hasDealtDamage = false;

    // 現在再生中のアニメーション名を記憶する変数
    private string currentState = "";

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // アニメーションの切り替わり中、または攻撃アニメーション中（再生完了前）の場合は
        // 上書きを防ぐために以後の移動やアニメーション変更処理を一時停止する
        if (animator.IsInTransition(0) || (stateInfo.IsName("Attack01") && stateInfo.normalizedTime < 0.9f))
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            // 攻撃範囲内で、かつクールダウンが終わっていれば攻撃
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
            }
            else
            {
                // クールダウン中は待機
                ChangeAnimation("IdleNormal");
            }
        }
        else
        {
            // 攻撃範囲外なら追いかける
            Chase();
        }
    }

    void Chase()
    {
        Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(targetPos);
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);

        ChangeAnimation("WalkFWD");
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        hasDealtDamage = false;

        ChangeAnimation("Attack01");
    }

    // ★追加：アニメーションを安全に切り替えるための専用メソッド
    void ChangeAnimation(string newState)
    {
        // 既に指定されたアニメーションを再生中なら、何もしない（毎フレーム再生を防ぐ）
        if (currentState == newState) return;

        animator.CrossFade(newState, 0.1f);
        currentState = newState;
    }

    void OnTriggerStay(Collider other)
    {
        // currentState を使って攻撃判定を行う
        if (other.CompareTag("Player") && currentState == "Attack01" && !hasDealtDamage)
        {
            Debug.Log("プレイヤーに " + damage + " のダメージ！");

            hasDealtDamage = true;
        }
    }
}