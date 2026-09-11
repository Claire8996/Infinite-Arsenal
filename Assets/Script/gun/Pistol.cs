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
    public GameObject reticleUI;

    private float nextFireTime = 0f;

    void Awake()
    {
        if (reticleUI == null)
        {
            reticleUI = GameObject.Find("PistolReticle");
        }
    }

    void OnEnable()
    {
        if (reticleUI != null) reticleUI.SetActive(true);
    }

    void OnDisable()
    {
        if (reticleUI != null) reticleUI.SetActive(false);
    }

    void Update()
    {
        if (DisablePlayerControl.IsEventActive) return;
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        nextFireTime = Time.time + fireRate;

        if (bulletPrefab != null && muzzlePoint != null)
        {
            // --- ★ここから射撃方向の計算処理 ---

            // 1. 画面のど真ん中（レティクルの位置）から奥に向かってRay（光線）を飛ばす
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            Vector3 targetPoint;

            // 2. もしRayが何かにぶつかったら、そこを「着弾予定地」にする
            if (Physics.Raycast(ray, out hit))
            {
                targetPoint = hit.point;
            }
            else
            {
                // 何もぶつからなかった場合（空などを撃った場合）は、カメラのずっと奥をターゲットにする
                targetPoint = ray.GetPoint(100f);
            }

            // 3. 銃口から着弾予定地に向かう「方向」を計算する
            Vector3 shootDirection = (targetPoint - muzzlePoint.position).normalized;

            // --- ★ここまで ---

            // 4. 計算した方向に向けて弾丸の角度を合わせて生成する
            GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(shootDirection));

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = bullet.AddComponent<Rigidbody>();
            }

            // 5. 計算した方向に向かって弾を飛ばす
            rb.velocity = shootDirection * bulletSpeed;

            Destroy(bullet, 3f);
        }

        if (smokeParticle != null)
        {
            smokeParticle.Play();
        }
    }
}