using UnityEngine;

public class RPG7 : MonoBehaviour
{
    [Header("銃の設定")]
    [Tooltip("発射する弾丸（ロケット弾）のプレハブ")]
    public GameObject bulletPrefab;

    [Tooltip("弾丸と煙が出る位置（銃口）")]
    public Transform muzzlePoint;

    [Tooltip("弾の飛ぶ速度")]
    public float bulletSpeed = 35f; // ★ロケット弾は初速が少し遅め（シューッと飛んでいく感じ）

    [Tooltip("次の弾が撃てるようになるまでの時間（秒）")]
    public float fireRate = 2.5f; // ★ロケランなのでリロードに時間がかかる（連射不可）設定

    [Tooltip("弾のブレ（数値を大きくするほど着弾点がバラける）")]
    public float spreadAmount = 0.02f; // ★ブレはほぼ無し

    [Header("エフェクト設定")]
    [Tooltip("発砲時に出す煙のパーティクル（バックブラスト等の煙）")]
    public ParticleSystem smokeParticle;

    [Header("UI設定")]
    [Tooltip("このRPG7専用のレティクル（照準）UI")]
    public GameObject reticleUI;

    private float nextFireTime = 0f;

    void Awake()
    {
        if (reticleUI == null)
        {
            reticleUI = GameObject.Find("RPG7Reticle");
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

        // ロケランは1発ずつ撃つので、GetButtonDown（押した瞬間）にするのがおすすめです
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
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out hit))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(100f);
            }

            targetPoint += Random.insideUnitSphere * spreadAmount;

            Vector3 shootDirection = (targetPoint - muzzlePoint.position).normalized;

            GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(shootDirection));

            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = bullet.AddComponent<Rigidbody>();
            }
            // ★追加：ロケット弾はまっすぐ飛ぶので、重力で落ちないようにする
            rb.useGravity = false;

            rb.velocity = shootDirection * bulletSpeed;

            // 何かにぶつかるまで長めに残す
            Destroy(bullet, 5f);
        }

        if (smokeParticle != null)
        {
            smokeParticle.Stop();
            smokeParticle.Play();
        }
    }
}