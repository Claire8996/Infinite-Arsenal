using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Tooltip("弾が与えるダメージ")]
    public int damage = 100;

    void Awake()
    {
        // 生成された瞬間に敵以外の不要なコライダーやWall/PassThroughとの衝突を無視させる
        IgnoreNonEnemyCollisions();
    }

    void Start()
    {
        // 念のためStartでも再確認
        IgnoreNonEnemyCollisions();
    }

    private void IgnoreNonEnemyCollisions()
    {
        Collider myCollider = GetComponent<Collider>();
        if (myCollider == null) return;

        // シーン内のすべてのコライダーを取得し、敵（Enemyタグや各種AI）以外との衝突をすべて無視する
        Collider[] allColliders = FindObjectsOfType<Collider>();
        foreach (Collider col in allColliders)
        {
            if (col == null || col == myCollider) continue;

            // 1. WallタグまたはPassThroughタグを持つオブジェクトは強制的に無視
            if (col.CompareTag("Wall") || col.CompareTag("PassThrough") ||
                (col.transform.parent != null && (col.transform.parent.CompareTag("Wall") || col.transform.parent.CompareTag("PassThrough"))) ||
                col.transform.root.CompareTag("Wall") || col.transform.root.CompareTag("PassThrough"))
            {
                Physics.IgnoreCollision(myCollider, col, true);
                continue;
            }

            // 2. 相手が敵かどうかを判定
            bool isEnemy = col.CompareTag("Enemy") ||
                           col.GetComponentInParent<SlimeAI>() != null ||
                           col.GetComponentInParent<StarFishAI>() != null ||
                           col.GetComponentInParent<TurtleShellAI>() != null ||
                           col.GetComponentInParent<MushroomAI>() != null ||
                           col.GetComponentInParent<CactusAI>() != null ||
                           col.GetComponentInParent<BlueSlimeAI>() != null ||
                           col.GetComponentInParent<SheepAI>() != null;

            // 敵以外であれば衝突を完全に無視（貫通）させる
            if (!isEnemy)
            {
                Physics.IgnoreCollision(myCollider, col, true);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 万が一、WallやPassThrough、あるいは敵以外に接触した場合は何もしない（すり抜ける）
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("PassThrough") ||
            collision.gameObject.transform.root.CompareTag("Wall") || collision.gameObject.transform.root.CompareTag("PassThrough"))
        {
            return;
        }

        bool isEnemy = collision.gameObject.CompareTag("Enemy") ||
                       collision.gameObject.GetComponentInParent<SlimeAI>() != null ||
                       collision.gameObject.GetComponentInParent<StarFishAI>() != null ||
                       collision.gameObject.GetComponentInParent<TurtleShellAI>() != null ||
                       collision.gameObject.GetComponentInParent<MushroomAI>() != null ||
                       collision.gameObject.GetComponentInParent<CactusAI>() != null ||
                       collision.gameObject.GetComponentInParent<BlueSlimeAI>() != null ||
                       collision.gameObject.GetComponentInParent<SheepAI>() != null;

        if (!isEnemy)
        {
            return;
        }

        // ぶつかった相手が「Enemy」だった場合の処理
        Rigidbody enemyRb = collision.gameObject.GetComponentInParent<Rigidbody>();
        if (enemyRb != null && !enemyRb.isKinematic)
        {
            enemyRb.velocity = Vector3.zero;
            enemyRb.angularVelocity = Vector3.zero;
        }

        int totalDamage = damage + ArmorStats.TotalAttackBonus;

        // 与えたダメージをミッションの合計ダメージ数に加算する
        MissionManager.AddDamage(totalDamage);

        // 1. スライムかどうかチェック
        SlimeAI slime = collision.gameObject.GetComponentInParent<SlimeAI>();
        if (slime != null)
        {
            slime.TakeDamage(totalDamage);
        }

        // 2. スターフィッシュかどうかチェック
        StarFishAI starfish = collision.gameObject.GetComponentInParent<StarFishAI>();
        if (starfish != null)
        {
            starfish.TakeDamage(totalDamage);
        }

        // 3. タートルシェルかどうかチェック
        TurtleShellAI turtle = collision.gameObject.GetComponentInParent<TurtleShellAI>();
        if (turtle != null)
        {
            turtle.TakeDamage(totalDamage);
        }

        // 4. マッシュルームかどうかチェック
        MushroomAI mushroom = collision.gameObject.GetComponentInParent<MushroomAI>();
        if (mushroom != null)
        {
            mushroom.TakeDamage(totalDamage);
        }

        // 5. サボテンかどうかチェック
        CactusAI cactus = collision.gameObject.GetComponentInParent<CactusAI>();
        if (cactus != null)
        {
            cactus.TakeDamage(totalDamage);
        }

        // 6. ブルースライムかどうかチェック
        BlueSlimeAI blueSlime = collision.gameObject.GetComponentInParent<BlueSlimeAI>();
        if (blueSlime != null)
        {
            blueSlime.TakeDamage(totalDamage);
        }

        // 7. シープ（SheepAI）かどうかチェック
        SheepAI sheep = collision.gameObject.GetComponentInParent<SheepAI>();
        if (sheep != null)
        {
            sheep.TakeDamage(totalDamage);
        }

        // 敵に当たった場合は弾を消滅させる
        Destroy(gameObject);
    }
}