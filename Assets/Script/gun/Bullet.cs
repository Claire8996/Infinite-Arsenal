using UnityEngine;

public class Bullet : MonoBehaviour
{
    // 物理的な衝突をした時の処理（ColliderのIsTriggerがオフの場合）
    void OnCollisionEnter(Collision collision)
    {
        // ぶつかった相手のタグが"Ground"だった場合
        if (collision.gameObject.CompareTag("Ground"))
        {
            // 弾自身を消滅させて貫通を防ぐ
            Destroy(gameObject);
        }
    }

    // センサーとして接触した時の処理（ColliderのIsTriggerがオンの場合）
    void OnTriggerEnter(Collider other)
    {
        // 触れた相手のタグが"Ground"だった場合
        if (other.CompareTag("Ground"))
        {
            // 弾自身を消滅させて貫通を防ぐ
            Destroy(gameObject);
        }
    }
}