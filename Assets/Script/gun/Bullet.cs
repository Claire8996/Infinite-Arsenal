using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Tooltip("弾が与えるダメージ")]
    public int damage = 100;

    void OnCollisionEnter(Collision collision)
    {
        // ぶつかった相手が「Enemy」タグを持っていた場合
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // =========================================================
            // ★改修：敵が物理的な衝撃で「上」にも吹き飛ばないようにする処理
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

            // 1. スライムかどうかチェック
            SlimeAI slime = collision.gameObject.GetComponent<SlimeAI>();
            if (slime != null)
            {
                slime.TakeDamage(damage);
            }

            // 2. タートルシェルかどうかチェック
            TurtleShellAI turtle = collision.gameObject.GetComponent<TurtleShellAI>();
            if (turtle != null)
            {
                turtle.TakeDamage(damage);
            }

            // 3. マッシュルームかどうかチェック
            MushroomAI mushroom = collision.gameObject.GetComponent<MushroomAI>();
            if (mushroom != null)
            {
                mushroom.TakeDamage(damage);
            }

            // 4. サボテンかどうかチェック
            CactusAI cactus = collision.gameObject.GetComponent<CactusAI>();
            if (cactus != null)
            {
                cactus.TakeDamage(damage);
            }

            // 敵に当たった場合は弾を消滅させる
            Destroy(gameObject);
        }
    }
}