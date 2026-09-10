using UnityEngine;

public class Pistol : MonoBehaviour
{
    [Header("銃の設定")]
    [Tooltip("発射する弾丸のプレハブ")]
    public GameObject bulletPrefab;

    [Tooltip("弾丸と煙が出る位置（銃口）")]
    public Transform muzzlePoint;

    [Tooltip("弾の飛ぶ速度")]
    public float bulletSpeed = 50f;

    [Tooltip("次の弾が撃てるようになるまでの時間（秒）")]
    public float fireRate = 0.2f;

    [Header("エフェクト設定")]
    [Tooltip("発砲時に出す煙のパーティクル")]
    public ParticleSystem smokeParticle;

    [Header("UI設定")]
    [Tooltip("このピストル専用のレティクル（照準）UI")]
    public GameObject reticleUI; // ★追加：レティクルの枠

    private float nextFireTime = 0f;

    // ★追加：この銃がアクティブ（装備）になった時に呼ばれる
    void OnEnable()
    {
        if (reticleUI != null)
        {
            reticleUI.SetActive(true); // レティクルを表示
        }
    }

    // ★追加：この銃が非アクティブ（別の武器に切り替え等）になった時に呼ばれる
    void OnDisable()
    {
        if (reticleUI != null)
        {
            reticleUI.SetActive(false); // レティクルを非表示
        }
    }

    void Update()
    {
        // GetButtonDownを使うことで、押しっぱなし（フルオート）を無効化し単発撃ちにする
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        nextFireTime = Time.time + fireRate;

        // 1. 弾丸の生成と発射
        if (bulletPrefab != null && muzzlePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = bullet.AddComponent<Rigidbody>();
            }

            rb.velocity = muzzlePoint.forward * bulletSpeed;

            // 3秒後に弾を消す
            Destroy(bullet, 3f);
        }

        // 2. 煙パーティクルの再生
        if (smokeParticle != null)
        {
            smokeParticle.Play();
        }
    }
}