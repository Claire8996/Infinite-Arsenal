using System.Collections;
using UnityEngine;

public class RocketBullet : MonoBehaviour
{
    [Header("爆発設定")]
    [Tooltip("爆発時に発生させるエフェクト（煙や炎）のプレハブ")]
    public GameObject explosionEffectPrefab;

    [Tooltip("爆発に巻き込む範囲（半径）")]
    public float explosionRadius = 5f;

    [Tooltip("爆発の吹き飛ばし力")]
    public float explosionForce = 700f;

    [Header("ダメージ設定")]
    [Tooltip("爆風に巻き込んだ敵に与えるダメージ量")]
    public int explosionDamage = 100;

    // ==========================================
    // 着弾時の爆発音（開始秒数・終了秒数指定）設定
    // ==========================================
    [Header("効果音（爆発音）設定")]
    [Tooltip("爆発音を再生するAudioSource（Prefab自身にAdd Componentしたものをセットしてください）")]
    public AudioSource explosionAudioSource;

    [Tooltip("着弾時に流す効果音（AudioClip）")]
    public AudioClip explosionSoundClip;

    [Tooltip("効果音を何秒から流すか（開始時間）")]
    public float soundStartTime = 0f;

    [Tooltip("効果音を何秒まで再生するか（終了時間 ※0以下にすると最後まで再生します）")]
    public float soundEndTime = 0f;

    [Tooltip("効果音の音量（0.0 〜 1.0）")]
    [Range(0f, 1f)]
    public float soundVolume = 1.0f;

    private static AudioClip trimmedExplosionClip;
    private static AudioClip cachedSourceClip;
    private static float cachedStartTime = -1f;
    private static float cachedEndTime = -1f;

    void Awake()
    {
        // 生成された瞬間に「Wall」と「Player」のColliderを貫通（衝突無視）するように設定
        IgnoreWallAndPlayerCollisions();
        SetupAudioSource();
        RebuildTrimmedAudioClipIfNeeded();
    }

    void Start()
    {
        // 念のためStartでも再確認
        IgnoreWallAndPlayerCollisions();
    }

    private void SetupAudioSource()
    {
        if (explosionAudioSource == null)
        {
            explosionAudioSource = GetComponent<AudioSource>();
            if (explosionAudioSource == null)
            {
                explosionAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        explosionAudioSource.playOnAwake = false;
        explosionAudioSource.loop = false;
    }

    private void IgnoreWallAndPlayerCollisions()
    {
        Collider myCollider = GetComponent<Collider>();
        if (myCollider == null) return;

        // =========================================================
        // 1. 「Wall」タグのついたオブジェクトを貫通する処理
        // =========================================================
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        foreach (GameObject wall in walls)
        {
            if (wall == null) continue;

            Collider[] wallColliders = wall.GetComponentsInChildren<Collider>(true);
            foreach (Collider wallCol in wallColliders)
            {
                Physics.IgnoreCollision(myCollider, wallCol, true);
            }
        }

        // =========================================================
        // 2. 「Player」のColliderを貫通する処理
        // =========================================================
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player == null) continue;

            Collider[] playerColliders = player.GetComponentsInChildren<Collider>(true);
            foreach (Collider playerCol in playerColliders)
            {
                Physics.IgnoreCollision(myCollider, playerCol, true);
            }
        }

        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            Collider[] pmColliders = playerMovement.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in pmColliders)
            {
                Physics.IgnoreCollision(myCollider, col, true);
            }
        }

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            Collider[] phColliders = playerHealth.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in phColliders)
            {
                Physics.IgnoreCollision(myCollider, col, true);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 万が一発射直後にPlayer自身のColliderに接触判定が出ても、爆発せずそのまま貫通させる
        if (collision.gameObject.CompareTag("Player") ||
            collision.gameObject.GetComponentInParent<PlayerMovement>() != null ||
            collision.gameObject.GetComponentInParent<PlayerHealth>() != null)
        {
            return;
        }

        // 着弾時に爆発音を再生する
        PlayExplosionSound();

        // 1. 爆発エフェクトを生成する
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, transform.rotation);
        }

        // 2. 爆発範囲内のオブジェクトをすべて取得する
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        int totalExplosionDamage = explosionDamage + ArmorStats.TotalAttackBonus;

        foreach (Collider nearbyObject in colliders)
        {
            // ぶつかった相手が「Enemy」タグを持っていた場合、または各種敵AIコンポーネントを持っている場合
            bool isEnemy = nearbyObject.CompareTag("Enemy") ||
                           nearbyObject.GetComponentInParent<SlimeAI>() != null ||
                           nearbyObject.GetComponentInParent<StarFishAI>() != null ||
                           nearbyObject.GetComponentInParent<TurtleShellAI>() != null ||
                           nearbyObject.GetComponentInParent<MushroomAI>() != null ||
                           nearbyObject.GetComponentInParent<CactusAI>() != null ||
                           nearbyObject.GetComponentInParent<BlueSlimeAI>() != null ||
                           nearbyObject.GetComponentInParent<SheepAI>() != null;

            if (isEnemy)
            {
                Rigidbody enemyRb = nearbyObject.GetComponentInParent<Rigidbody>();
                if (enemyRb != null && !enemyRb.isKinematic)
                {
                    enemyRb.velocity = Vector3.zero;
                    enemyRb.angularVelocity = Vector3.zero;
                }

                // 与えた爆発ダメージをミッションの合計ダメージ数に加算する
                MissionManager.AddDamage(totalExplosionDamage);

                // 1. スライムかどうかチェック
                SlimeAI slime = nearbyObject.GetComponentInParent<SlimeAI>();
                if (slime != null)
                {
                    slime.TakeDamage(totalExplosionDamage);
                }

                // 2. スターフィッシュかどうかチェック
                StarFishAI starfish = nearbyObject.GetComponentInParent<StarFishAI>();
                if (starfish != null)
                {
                    starfish.TakeDamage(totalExplosionDamage);
                }

                // 3. タートルシェルかどうかチェック
                TurtleShellAI turtle = nearbyObject.GetComponentInParent<TurtleShellAI>();
                if (turtle != null)
                {
                    turtle.TakeDamage(totalExplosionDamage);
                }

                // 4. マッシュルームかどうかチェック
                MushroomAI mushroom = nearbyObject.GetComponentInParent<MushroomAI>();
                if (mushroom != null)
                {
                    mushroom.TakeDamage(totalExplosionDamage);
                }

                // 5. サボテンかどうかチェック
                CactusAI cactus = nearbyObject.GetComponentInParent<CactusAI>();
                if (cactus != null)
                {
                    cactus.TakeDamage(totalExplosionDamage);
                }

                // 6. ブルースライムかどうかチェック
                BlueSlimeAI blueSlime = nearbyObject.GetComponentInParent<BlueSlimeAI>();
                if (blueSlime != null)
                {
                    blueSlime.TakeDamage(totalExplosionDamage);
                }

                // 7. シープ（SheepAI）かどうかチェック
                SheepAI sheep = nearbyObject.GetComponentInParent<SheepAI>();
                if (sheep != null)
                {
                    sheep.TakeDamage(totalExplosionDamage);
                }
            }
        }

        // 3. 爆発したので、このロケット弾自体は消滅する
        Destroy(gameObject);
    }

    private void PlayExplosionSound()
    {
        if (explosionSoundClip == null) return;

        RebuildTrimmedAudioClipIfNeeded();

        AudioClip clipToPlay = trimmedExplosionClip != null ? trimmedExplosionClip : explosionSoundClip;

        GameObject tempAudioObj = new GameObject("TempExplosionAudio");
        tempAudioObj.transform.position = transform.position;
        AudioSource aSource = tempAudioObj.AddComponent<AudioSource>();
        aSource.clip = clipToPlay;
        aSource.volume = soundVolume;
        aSource.spatialBlend = 0f;
        aSource.Play();

        Destroy(tempAudioObj, clipToPlay.length + 0.1f);
    }

    private void RebuildTrimmedAudioClipIfNeeded()
    {
        if (explosionSoundClip == null) return;

        if (cachedSourceClip == explosionSoundClip &&
            Mathf.Approximately(cachedStartTime, soundStartTime) &&
            Mathf.Approximately(cachedEndTime, soundEndTime) &&
            trimmedExplosionClip != null)
        {
            return;
        }

        cachedSourceClip = explosionSoundClip;
        cachedStartTime = soundStartTime;
        cachedEndTime = soundEndTime;

        if (trimmedExplosionClip != null && trimmedExplosionClip != explosionSoundClip)
        {
            trimmedExplosionClip = null;
        }

        if (soundStartTime <= 0f && soundEndTime <= 0f)
        {
            trimmedExplosionClip = explosionSoundClip;
            return;
        }

        if (explosionSoundClip.loadState == AudioDataLoadState.Unloaded)
        {
            explosionSoundClip.LoadAudioData();
        }

        int frequency = explosionSoundClip.frequency;
        int channels = explosionSoundClip.channels;
        int totalSamples = explosionSoundClip.samples;

        int startSample = Mathf.Clamp(Mathf.RoundToInt(soundStartTime * frequency), 0, Mathf.Max(0, totalSamples - 1));
        int endSample = totalSamples;

        if (soundEndTime > 0f && soundEndTime * frequency > startSample)
        {
            endSample = Mathf.Clamp(Mathf.RoundToInt(soundEndTime * frequency), startSample + 1, totalSamples);
        }

        int sampleLength = endSample - startSample;
        if (sampleLength <= 0) return;

        float[] data = new float[sampleLength * channels];
        if (!explosionSoundClip.GetData(data, startSample))
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

        trimmedExplosionClip = AudioClip.Create(explosionSoundClip.name + "_TrimmedExplosion", sampleLength, channels, frequency, false);
        trimmedExplosionClip.SetData(data, 0);
    }
}