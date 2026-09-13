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

            // 敵に当たった場合は弾を消滅させる
            Destroy(gameObject);
        }
    }
}