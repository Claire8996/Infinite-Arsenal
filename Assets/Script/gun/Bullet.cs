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
        // 2. 「Player」のColliderを貫通する処理
        // =========================================================
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
        // 万が一Playerに接触した場合は何もせずスルーする
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
            // ★修正：敵が弾の衝撃で吹き飛ばないようにする処理
            // =========================================================
            Rigidbody enemyRb = collision.gameObject.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
                enemyRb.velocity = Vector3.zero;
                enemyRb.angularVelocity = Vector3.zero;
            }
            // =========================================================

            int totalDamage = damage + ArmorStats.TotalAttackBonus;

            // 与えたダメージをミッションの合計ダメージ数に加算する
            MissionManager.AddDamage(totalDamage);

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

            // 5. ブルースライムかどうかチェック
            BlueSlimeAI blueSlime = collision.gameObject.GetComponent<BlueSlimeAI>();
            if (blueSlime != null)
            {
                blueSlime.TakeDamage(totalDamage);
            }

            // 敵に当たった場合は弾を消滅させる
            Destroy(gameObject);
        }
    }
}