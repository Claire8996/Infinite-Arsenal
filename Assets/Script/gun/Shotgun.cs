using UnityEngine;

public class Shotgun : MonoBehaviour
{
    [Header("銃の設定")]
    [Tooltip("発射する弾丸のプレハブ")]
    public GameObject bulletPrefab;

    [Tooltip("弾丸と煙が出る位置（銃口）")]
    public Transform muzzlePoint;

    [Tooltip("弾の飛ぶ速度")]
    public float bulletSpeed = 50f; // ★他の銃より少し遅め（近距離特化のイメージ）

    [Tooltip("次の射撃までの時間（秒）")]
    public float fireRate = 1.0f; // ★ガシャンッとポンプアクションする時間を想定し遅めに設定

    // ==========================================
    // 💡 追加：ショットガン特有の設定
    // ==========================================
    [Tooltip("1回の射撃で発射される弾（ペレット）の数")]
    public int pelletCount = 8; // ★1回で8発の弾が同時に出る

    [Tooltip("散弾の広がり具合（数値を大きくするほど広範囲にバラける）")]
    public float spreadAmount = 1.5f; // ★広範囲に当たるようにブレを大きく設定

    [Header("エフェクト設定")]
    [Tooltip("発砲時に出す煙のパーティクル")]
    public ParticleSystem smokeParticle;

    [Header("UI設定")]
    [Tooltip("このショットガン専用のレティクル（照準）UI")]
    public GameObject reticleUI;

    private float nextFireTime = 0f;

    void Awake()
    {
        if (reticleUI == null)
        {
            // ショットガン専用のレティクルがあれば名前を合わせてください
            reticleUI = GameObject.Find("ShotgunReticle");
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

        // ショットガンの場合は、長押しではなく1回ごとのクリックで撃たせるゲームが多いので GetButtonDown もおすすめですが、
        // 今までの仕様に合わせるなら GetButton でOKです。
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        nextFireTime = Time.time + fireRate;

        if (bulletPrefab != null && muzzlePoint != null)
        {
            // 1. 画面のど真ん中（レティクルの位置）から奥に向かってRay（光線）を飛ばす
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            Vector3 baseTargetPoint; // ★今回はこれが「基準となる着弾点」になります

            if (Physics.Raycast(ray, out hit))
            {
                baseTargetPoint = hit.point;
            }
            else
            {
                baseTargetPoint = ray.GetPoint(100f);
            }

            // ==========================================
            // 💡 修正：指定した弾の数（pelletCount）だけループして一気に生成する！
            // ==========================================
            for (int i = 0; i < pelletCount; i++)
            {
                // 各弾ごとに、基準点からランダムにブレ（散らばり）を別々に計算する
                Vector3 spread = Random.insideUnitSphere * spreadAmount;
                Vector3 pelletTargetPoint = baseTargetPoint + spread;

                // 銃口からその散らばった着弾予定地に向かう方向
                Vector3 shootDirection = (pelletTargetPoint - muzzlePoint.position).normalized;

                // 弾丸を生成
                GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(shootDirection));

                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = bullet.AddComponent<Rigidbody>();
                }

                // それぞれの方向に弾を飛ばす
                rb.velocity = shootDirection * bulletSpeed;

                // ショットガンは遠くに飛ぶと威力が落ちる（消える）イメージを出すため、生存時間を短めに設定
                Destroy(bullet, 1.5f);
            }
        }

        if (smokeParticle != null)
        {
            smokeParticle.Stop();
            smokeParticle.Play();
        }
    }
}