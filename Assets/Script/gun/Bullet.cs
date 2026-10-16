using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Tooltip("弾が与えるダメージ")]
    public int damage = 100;

    void Awake()
    {
        // 生成された瞬間に「Wall」と「Player」のColliderを貫通（衝突無視）するように設定
        IgnoreWallAndPlayerCollisions();
    }

    void Start()
    {
        // 念のためStartでも再確認
        IgnoreWallAndPlayerCollisions();
    }

    private void IgnoreWallAndPlayerCollisions()
    {
        Collider myCollider = GetComponent<Collider>();
        if (myCollider == null) return;

        // =========================================================
        // 1. 「Wall」タグのついたオブジェクトを貫通する処理
        // =========================================================
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        foreach (GameObject wall in walls)
        {
            if (wall == null) continue;

            Collider[] wallColliders = wall.GetComponentsInChildren<Collider>(true);
            foreach (Collider wallCol in wallColliders)
            {
                Physics.IgnoreCollision(myCollider, wallCol, true);
            }
        }

        // =========================================================
        // ★追加：2. 「Player」のColliderを貫通する処理
        // =========================================================
        // (A) 「Player」タグがついたオブジェクトのColliderをすべて無視
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player == null) continue;

            Collider[] playerColliders = player.GetComponentsInChildren<Collider>(true);
            foreach (Collider playerCol in playerColliders)
            {
                Physics.IgnoreCollision(myCollider, playerCol, true);
            }
        }

        // (B) タグ設定を忘れていても確実に貫通するよう、PlayerMovement / PlayerHealth 側のColliderも無視
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            Collider[] pmColliders = playerMovement.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in pmColliders)
            {
                Physics.IgnoreCollision(myCollider, col, true);
            }
        }

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            Collider[] phColliders = playerHealth.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in phColliders)
            {
                Physics.IgnoreCollision(myCollider, col, true);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // ★追加：万が一Playerに接触した場合は何もせずスルーする
        if (collision.gameObject.CompareTag("Player") ||
            collision.gameObject.GetComponentInParent<PlayerMovement>() != null ||
            collision.gameObject.GetComponentInParent<PlayerHealth>() != null)
        {
            return;
        }

        // ぶつかった相手が「Enemy」タグを持っていた場合
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // =========================================================
            // 敵が物理的な衝撃で「上」にも吹き飛ばないようにする処理
            // =========================================================
            Rigidbody enemyRb = collision.gameObject.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
                // 弾が当たった瞬間のY軸（上下）の速度を取得
                float currentVelocityY = enemyRb.velocity.y;

                // もし「上方向」に吹き飛ぶ力（プラスの値）が発生していたら強制的に0にする
                // （マイナスの場合は重力で落下中なので、そのまま生かす）
                if (currentVelocityY > 0f)
                {
                    currentVelocityY = 0f;
                }

                // 横の吹き飛びを0にしつつ、上に飛ばないように調整したY軸の速度をセット
                enemyRb.velocity = new Vector3(0f, currentVelocityY, 0f);
                enemyRb.angularVelocity = Vector3.zero;
            }
            // =========================================================

            // =========================================================
            // 弾のダメージに防具の「攻撃力ボーナス（実数値）」を加算する
            // =========================================================
            int totalDamage = damage + ArmorStats.TotalAttackBonus;

            // 1. スライムかどうかチェック
            SlimeAI slime = collision.gameObject.GetComponent<SlimeAI>();
            if (slime != null)
            {
                slime.TakeDamage(totalDamage);
            }

            // 2. タートルシェルかどうかチェック
            TurtleShellAI turtle = collision.gameObject.GetComponent<TurtleShellAI>();
            if (turtle != null)
            {
                turtle.TakeDamage(totalDamage);
            }

            // 3. マッシュルームかどうかチェック
            MushroomAI mushroom = collision.gameObject.GetComponent<MushroomAI>();
            if (mushroom != null)
            {
                mushroom.TakeDamage(totalDamage);
            }

            // 4. サボテンかどうかチェック
            CactusAI cactus = collision.gameObject.GetComponent<CactusAI>();
            if (cactus != null)
            {
                cactus.TakeDamage(totalDamage);
            }

            // 敵に当たった場合は弾を消滅させる
            Destroy(gameObject);
        }
    }
}