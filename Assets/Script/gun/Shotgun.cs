using System.Collections;
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

    // ==========================================
    // ★追加：発砲時の効果音（開始秒数・終了秒数指定）設定
    // ==========================================
    [Header("効果音（銃声）設定")]
    [Tooltip("銃声を再生するAudioSource（Prefab自身にAdd Componentしたものをセットしてください）")]
    public AudioSource gunAudioSource;

    [Tooltip("発砲時に流す効果音（AudioClip）")]
    public AudioClip shootSoundClip;

    [Tooltip("効果音を何秒から流すか（開始時間）")]
    public float soundStartTime = 0f;

    [Tooltip("効果音を何秒まで再生するか（終了時間 ※0以下にすると最後まで再生します）")]
    public float soundEndTime = 0f;

    [Tooltip("効果音の音量（0.0 〜 1.0）")]
    [Range(0f, 1f)]
    public float soundVolume = 1.0f;

    private float nextFireTime = 0f;

    private AudioClip trimmedShootClip;
    private AudioClip cachedSourceClip;
    private float cachedStartTime = -1f;
    private float cachedEndTime = -1f;
    private Coroutine stopSoundCoroutine;

    void Awake()
    {
        if (reticleUI == null)
        {
            // ショットガン専用のレティクルがあれば名前を合わせてください
            reticleUI = GameObject.Find("ShotgunReticle");
        }

        SetupAudioSource();
        RebuildTrimmedAudioClipIfNeeded();
    }

    private void SetupAudioSource()
    {
        if (gunAudioSource == null)
        {
            gunAudioSource = GetComponent<AudioSource>();
            if (gunAudioSource == null)
            {
                gunAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        gunAudioSource.playOnAwake = false;
        gunAudioSource.loop = false;
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

        // ★追加：発砲時の効果音を再生
        PlayShootSound();

        if (bulletPrefab != null && muzzlePoint != null)
        {
            // 1. 画面のど真ん中（レティクルの位置）から奥に向かってRay（光線）を飛ばす
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            Vector3 baseTargetPoint; // ★이번にはこれが「基準となる着弾点」になります

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

    private void PlayShootSound()
    {
        if (gunAudioSource == null) SetupAudioSource();

        if (shootSoundClip == null && gunAudioSource.clip != null && gunAudioSource.clip != trimmedShootClip)
        {
            shootSoundClip = gunAudioSource.clip;
        }

        if (shootSoundClip == null) return;

        RebuildTrimmedAudioClipIfNeeded();

        if (trimmedShootClip != null)
        {
            gunAudioSource.PlayOneShot(trimmedShootClip, soundVolume);
        }
        else
        {
            gunAudioSource.clip = shootSoundClip;
            gunAudioSource.volume = soundVolume;

            float clipLen = shootSoundClip.length;
            float safeStart = Mathf.Clamp(soundStartTime, 0f, Mathf.Max(0f, clipLen - 0.01f));
            gunAudioSource.time = safeStart;
            gunAudioSource.Play();

            if (soundEndTime > safeStart)
            {
                float playDuration = Mathf.Min(soundEndTime, clipLen) - safeStart;
                if (stopSoundCoroutine != null) StopCoroutine(stopSoundCoroutine);
                stopSoundCoroutine = StartCoroutine(StopAudioAfterDelay(playDuration));
            }
        }
    }

    private IEnumerator StopAudioAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gunAudioSource != null && gunAudioSource.isPlaying)
        {
            gunAudioSource.Stop();
        }
    }

    private void RebuildTrimmedAudioClipIfNeeded()
    {
        if (shootSoundClip == null) return;

        if (cachedSourceClip == shootSoundClip &&
            Mathf.Approximately(cachedStartTime, soundStartTime) &&
            Mathf.Approximately(cachedEndTime, soundEndTime) &&
            trimmedShootClip != null)
        {
            return;
        }

        cachedSourceClip = shootSoundClip;
        cachedStartTime = soundStartTime;
        cachedEndTime = soundEndTime;

        if (trimmedShootClip != null && trimmedShootClip != shootSoundClip)
        {
            Destroy(trimmedShootClip);
            trimmedShootClip = null;
        }

        if (soundStartTime <= 0f && soundEndTime <= 0f)
        {
            trimmedShootClip = shootSoundClip;
            return;
        }

        if (shootSoundClip.loadState == AudioDataLoadState.Unloaded)
        {
            shootSoundClip.LoadAudioData();
        }

        int frequency = shootSoundClip.frequency;
        int channels = shootSoundClip.channels;
        int totalSamples = shootSoundClip.samples;

        int startSample = Mathf.Clamp(Mathf.RoundToInt(soundStartTime * frequency), 0, Mathf.Max(0, totalSamples - 1));
        int endSample = totalSamples;

        if (soundEndTime > 0f && soundEndTime * frequency > startSample)
        {
            endSample = Mathf.Clamp(Mathf.RoundToInt(soundEndTime * frequency), startSample + 1, totalSamples);
        }

        int sampleLength = endSample - startSample;
        if (sampleLength <= 0) return;

        float[] data = new float[sampleLength * channels];
        if (!shootSoundClip.GetData(data, startSample))
        {
            return;
        }

        int fadeFrames = Mathf.Min(sampleLength / 10, Mathf.Max(1, frequency * 3 / 1000));
        for (int f = 0; f < fadeFrames; f++)
        {
            float t = (float)f / fadeFrames;
            int headBase = f * channels;
            int tailBase = (sampleLength - 1 - f) * channels;
            for (int ch = 0; ch < channels; ch++)
            {
                data[headBase + ch] *= t;
                data[tailBase + ch] *= t;
            }
        }

        trimmedShootClip = AudioClip.Create(shootSoundClip.name + "_TrimmedShot", sampleLength, channels, frequency, false);
        trimmedShootClip.SetData(data, 0);
    }

    void OnDestroy()
    {
        if (trimmedShootClip != null && trimmedShootClip != shootSoundClip)
        {
            Destroy(trimmedShootClip);
        }
    }
}