using UnityEngine;

public class LMG : MonoBehaviour
{
    [Header("銃の設定")]
    [Tooltip("発射する弾丸のプレハブ")]
    public GameObject bulletPrefab;

    [Tooltip("弾丸と煙が出る位置（銃口）")]
    public Transform muzzlePoint;

    [Tooltip("弾の飛ぶ速度")]
    public float bulletSpeed = 60f; // ★重量級の銃なので、弾速はAKと同じくらいに設定

    [Tooltip("次の弾が撃てるようになるまでの時間（秒）\n※長押しでこの間隔で連射されます")]
    public float fireRate = 0.075f; // ★M4(0.095f)より連射速度を速くし、弾幕を張れるように設定

    [Tooltip("弾のブレ（数値を大きくするほど着弾点がバラける）")]
    public float spreadAmount = 0.6f; // ★連射が速い分、AK(0.5f)以上にブレを大きくして「ばらまく」感じを表現

    [Header("エフェクト設定")]
    [Tooltip("発砲時に出す煙のパーティクル")]
    public ParticleSystem smokeParticle;

    [Header("UI設定")]
    [Tooltip("このLMG専用のレティクル（照準）UI")]
    public GameObject reticleUI;

    private float nextFireTime = 0f;

    void Awake()
    {
        if (reticleUI == null)
        {
            // LMG専用のレティクルがあれば名前を合わせてください
            reticleUI = GameObject.Find("LMGReticle");
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

        // 左クリック長押しでフルオート連射（LMG特有の弾幕）
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
            Vector3 targetPoint;

            // 2. もしRayが何かにぶつかったら、そこを「着弾予定地」にする
            if (Physics.Raycast(ray, out hit))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(100f);
            }

            // 着弾予定地を少しだけランダムにずらして「銃のブレ」を表現する
            targetPoint += Random.insideUnitSphere * spreadAmount;

            // 3. 銃口から着弾予定地に向かう「方向」を計算する
            Vector3 shootDirection = (targetPoint - muzzlePoint.position).normalized;

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
            // 連射した時に煙が途切れないように、一度リセットしてから再生する
            smokeParticle.Stop();
            smokeParticle.Play();
        }
    }
}