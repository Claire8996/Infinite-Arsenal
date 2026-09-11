using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Tooltip("弾が与えるダメージ")]
    public int damage = 100; // ★追加：ダメージ量

    void OnCollisionEnter(Collision collision)
    {
        // ぶつかった相手が「Enemy」タグを持っていた場合
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 相手にくっついている SlimeAI スクリプトを取得する
            SlimeAI slime = collision.gameObject.GetComponent<SlimeAI>();

            // スクリプトが見つかったらダメージを与える
            if (slime != null)
            {
                slime.TakeDamage(damage);
            }

            // 敵に当たった場合は弾を消滅させる
            Destroy(gameObject);
        }
    }
}