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

    void Start()
    {
        // =========================================================
        // ★追加：「Wall」タグのついたオブジェクトを貫通する処理
        // =========================================================
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null)
        {
            // シーン内の「Wall」タグがついたオブジェクトを全て取得
            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            foreach (GameObject wall in walls)
            {
                if (wall == null) continue;

                // 親だけでなく、子供に付いているColliderも全て取得
                Collider[] wallColliders = wall.GetComponentsInChildren<Collider>();
                foreach (Collider wallCol in wallColliders)
                {
                    // 弾のColliderと壁のColliderの「物理的な衝突」を無視する！
                    Physics.IgnoreCollision(myCollider, wallCol, true);
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 1. 爆発エフェクトを生成する
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, transform.rotation);
        }

        // 2. 爆発範囲内のオブジェクトをすべて取得する
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

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
                    // まず爆風の力を加える
                    enemyRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);

                    // 弾が当たった瞬間のY軸（上下）の速度を取得
                    float currentVelocityY = enemyRb.velocity.y;

                    // もし「上方向」に吹き飛ぶ力（プラスの値）が発生していたら強制的に0にする
                    if (currentVelocityY > 0f)
                    {
                        currentVelocityY = 0f;
                    }

                    // ロケランらしく「横」への爆風の吹き飛びは残しつつ、上に飛ばないようにY軸を調整
                    enemyRb.velocity = new Vector3(enemyRb.velocity.x, currentVelocityY, enemyRb.velocity.z);
                    enemyRb.angularVelocity = Vector3.zero;
                }
                // =========================================================

                // 1. スライムかどうかチェック
                SlimeAI slime = nearbyObject.GetComponent<SlimeAI>();
                if (slime != null)
                {
                    slime.TakeDamage(explosionDamage);
                }

                // 2. タートルシェルかどうかチェック
                TurtleShellAI turtle = nearbyObject.GetComponent<TurtleShellAI>();
                if (turtle != null)
                {
                    turtle.TakeDamage(explosionDamage);
                }

                // 3. マッシュルームかどうかチェック
                MushroomAI mushroom = nearbyObject.GetComponent<MushroomAI>();
                if (mushroom != null)
                {
                    mushroom.TakeDamage(explosionDamage);
                }

                // 4. サボテンかどうかチェック
                CactusAI cactus = nearbyObject.GetComponent<CactusAI>();
                if (cactus != null)
                {
                    cactus.TakeDamage(explosionDamage);
                }
            }
        }

        // 3. 爆発したので、このロケット弾自体は消滅する
        Destroy(gameObject);
    }
}