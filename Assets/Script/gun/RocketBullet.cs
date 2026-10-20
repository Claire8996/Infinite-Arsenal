using UnityEngine;

public class RocketBullet : MonoBehaviour
{
    [Header("爆発設定")]
    [Tooltip("爆発時に発生させるエフェクト（煙や炎）のプレハブ")]
    public GameObject explosionEffectPrefab;

    [Tooltip("爆発に巻き込む範囲（半径）")]
    public float explosionRadius = 5f;

    [Tooltip("爆発の吹き飛ばし力")]
    public float explosionForce = 700f;

    [Header("ダメージ設定")]
    [Tooltip("爆風に巻き込んだ敵に与えるダメージ量")]
    public int explosionDamage = 100;

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
        // 万が一発射直後にPlayer自身のColliderに接触判定が出ても、爆発せずそのまま貫通させる
        if (collision.gameObject.CompareTag("Player") ||
            collision.gameObject.GetComponentInParent<PlayerMovement>() != null ||
            collision.gameObject.GetComponentInParent<PlayerHealth>() != null)
        {
            return;
        }

        // 1. 爆発エフェクトを生成する
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, transform.rotation);
        }

        // 2. 爆発範囲内のオブジェクトをすべて取得する
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        int totalExplosionDamage = explosionDamage + ArmorStats.TotalAttackBonus;

        foreach (Collider nearbyObject in colliders)
        {
            // ぶつかった相手が「Enemy」タグを持っていた場合
            if (nearbyObject.CompareTag("Enemy"))
            {
                // =========================================================
                // 敵が物理的な衝撃で「上」にも吹き飛ばないようにする処理
                // =========================================================
                Rigidbody enemyRb = nearbyObject.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    enemyRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                    float currentVelocityY = enemyRb.velocity.y;
                    if (currentVelocityY > 0f)
                    {
                        currentVelocityY = 0f;
                    }

                    enemyRb.velocity = new Vector3(enemyRb.velocity.x, currentVelocityY, enemyRb.velocity.z);
                    enemyRb.angularVelocity = Vector3.zero;
                }
                // =========================================================

                // ★追加：与えた爆発ダメージをミッションの合計ダメージ数に加算する
                MissionManager.AddDamage(totalExplosionDamage);

                // 1. スライムかどうかチェック
                SlimeAI slime = nearbyObject.GetComponent<SlimeAI>();
                if (slime != null)
                {
                    slime.TakeDamage(totalExplosionDamage);
                }

                // 2. タートルシェルかどうかチェック
                TurtleShellAI turtle = nearbyObject.GetComponent<TurtleShellAI>();
                if (turtle != null)
                {
                    turtle.TakeDamage(totalExplosionDamage);
                }

                // 3. マッシュルームかどうかチェック
                MushroomAI mushroom = nearbyObject.GetComponent<MushroomAI>();
                if (mushroom != null)
                {
                    mushroom.TakeDamage(totalExplosionDamage);
                }

                // 4. サボテンかどうかチェック
                CactusAI cactus = nearbyObject.GetComponent<CactusAI>();
                if (cactus != null)
                {
                    cactus.TakeDamage(totalExplosionDamage);
                }
            }
        }

        // 3. 爆発したので、このロケット弾自体は消滅する
        Destroy(gameObject);
    }
}