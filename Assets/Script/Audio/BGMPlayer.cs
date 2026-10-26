using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    [Header("【オーディオ参照設定】")]
    [Tooltip("BGMを再生するAudioSource（未設定の場合は同じオブジェクトから自動取得します）")]
    public AudioSource audioSource;

    [Tooltip("再生するBGMの音声ファイル（AudioClip）※Load Typeを「Decompress On Load」にしてください")]
    public AudioClip bgmClip;

    [Header("【再生時間・ループ範囲設定（秒）】")]
    [Tooltip("音楽が何秒から始まるか（開始時間）")]
    public float startTime = 0f;

    [Tooltip("音楽が何秒で終わるか（終了時間 ※0以下に設定すると曲の最後まで再生して開始時間に戻ります）")]
    public float endTime = 0f;

    [Header("【途切れ防止・シームレスループ設定】")]
    [Tooltip("チェックを入れると、曲の最初と最後にある「無音や消えかけの小さい余韻」を自動で切り詰め、途切れを無くします")]
    public bool autoTrimSilence = true;

    [Tooltip("曲の終わりの「消えかけの余韻」をどこまでカットするかの感度（数値を上げるほど、音が小さくなり始める早い段階で次のループへ繋がります）")]
    [Range(0.01f, 0.3f)]
    public float tailCutSensitivity = 0.08f;

    [Tooltip("曲の終わりと次のループの始まりを重ねる（オーバーラップさせる）時間（秒）")]
    [Range(0f, 1.0f)]
    public float loopOverlapTime = 0.15f;

    [Header("【オプション】")]
    [Tooltip("シーン開始時に自動で再生を開始するかどうか")]
    public bool playOnStart = true;

    private AudioClip generatedLoopClip;

    private float appliedStartTime = -1f;
    private float appliedEndTime = -1f;
    private float appliedOverlap = -1f;
    private bool appliedAutoTrim = false;
    private float appliedSensitivity = -1f;
    private AudioClip appliedSourceClip = null;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;

        if (bgmClip == null && audioSource.clip != null)
        {
            bgmClip = audioSource.clip;
        }

        if (bgmClip != null && bgmClip.loadState == AudioDataLoadState.Unloaded)
        {
            bgmClip.LoadAudioData();
        }
    }

    void Start()
    {
        if (playOnStart && bgmClip != null)
        {
            PlayBGM();
        }
    }

    void Update()
    {
        if (audioSource == null || bgmClip == null) return;

        // Play中にInspectorで秒数や感度を調整した場合、即座にループ音源を作り直して反映する
        if (audioSource.isPlaying && HasSettingsChanged())
        {
            PlayBGM();
        }
    }

    private bool HasSettingsChanged()
    {
        return !Mathf.Approximately(appliedStartTime, startTime) ||
               !Mathf.Approximately(appliedEndTime, endTime) ||
               !Mathf.Approximately(appliedOverlap, loopOverlapTime) ||
               appliedAutoTrim != autoTrimSilence ||
               !Mathf.Approximately(appliedSensitivity, tailCutSensitivity) ||
               appliedSourceClip != bgmClip;
    }

    // BGMを指定範囲で切り出し、隙間ゼロでループ再生するメソッド
    public void PlayBGM()
    {
        if (audioSource == null) return;

        if (bgmClip == null && audioSource.clip != null && audioSource.clip != generatedLoopClip)
        {
            bgmClip = audioSource.clip;
        }

        if (bgmClip == null) return;

        appliedStartTime = startTime;
        appliedEndTime = endTime;
        appliedOverlap = loopOverlapTime;
        appliedAutoTrim = autoTrimSilence;
        appliedSensitivity = tailCutSensitivity;
        appliedSourceClip = bgmClip;

        AudioClip loopClip = CreateSeamlessLoopClip(bgmClip, startTime, endTime, loopOverlapTime, autoTrimSilence, tailCutSensitivity);

        if (loopClip != null)
        {
            if (generatedLoopClip != null)
            {
                Destroy(generatedLoopClip);
            }
            generatedLoopClip = loopClip;

            audioSource.Stop();
            audioSource.clip = generatedLoopClip;
            audioSource.loop = true;
            audioSource.timeSamples = 0;
            audioSource.Play();
        }
        else
        {
            audioSource.Stop();
            audioSource.clip = bgmClip;
            audioSource.loop = true;
            audioSource.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, bgmClip.length - 0.01f));
            audioSource.Play();
        }
    }

    private AudioClip CreateSeamlessLoopClip(AudioClip sourceClip, float startSec, float endSec, float overlapSec, bool trimSilence, float sensitivity)
    {
        if (sourceClip == null) return null;

        int frequency = sourceClip.frequency;
        int channels = sourceClip.channels;
        int totalSamples = sourceClip.samples;

        int startSample = Mathf.Clamp(Mathf.RoundToInt(startSec * frequency), 0, Mathf.Max(0, totalSamples - 1));
        int endSample = totalSamples;

        if (endSec > 0f && endSec * frequency > startSample)
        {
            endSample = Mathf.Clamp(Mathf.RoundToInt(endSec * frequency), startSample + 1, totalSamples);
        }

        int sampleLength = endSample - startSample;
        if (sampleLength <= 0) return null;

        float[] rawData = new float[sampleLength * channels];
        if (!sourceClip.GetData(rawData, startSample))
        {
            Debug.LogWarning("BGMの波形データを取得できませんでした。AudioClipのLoad Typeが「Decompress On Load」になっているか確認してください。");
            return null;
        }

        int trimStartFrame = 0;
        int trimEndFrame = sampleLength - 1;

        if (trimSilence)
        {
            // 1. 曲全体の最大音量（ピーク）を調べる
            float maxPeak = 0f;
            for (int i = 0; i < rawData.Length; i++)
            {
                float abs = Mathf.Abs(rawData[i]);
                if (abs > maxPeak) maxPeak = abs;
            }

            if (maxPeak > 0.0001f)
            {
                // 単発ノイズに騙されないよう、約10ミリ秒分のウィンドウ平均で音量を判定する
                int windowFrames = Mathf.Max(1, frequency / 100);

                // 先頭の立ち上がり判定（敏感めに検出して出だしが欠けないようにする）
                float headThreshold = maxPeak * Mathf.Min(0.02f, sensitivity * 0.5f);
                for (int frame = 0; frame < sampleLength - windowFrames; frame += windowFrames / 2)
                {
                    if (GetWindowMaxAmplitude(rawData, frame, windowFrames, channels) >= headThreshold)
                    {
                        // 立ち上がりの少し手前（約5ミリ秒前）から開始してアタック音を保護
                        trimStartFrame = Mathf.Max(0, frame - (windowFrames / 2));
                        break;
                    }
                }

                // 末尾の減衰カット判定（音が小さくなって途切れて聞こえる余韻部分を切り詰める）
                float tailThreshold = maxPeak * sensitivity;
                for (int frame = sampleLength - windowFrames - 1; frame > trimStartFrame; frame -= windowFrames / 2)
                {
                    if (GetWindowMaxAmplitude(rawData, frame, windowFrames, channels) >= tailThreshold)
                    {
                        trimEndFrame = Mathf.Min(sampleLength - 1, frame + windowFrames);
                        break;
                    }
                }
            }
        }

        int activeFrames = (trimEndFrame - trimStartFrame) + 1;
        if (activeFrames <= 0) activeFrames = sampleLength;

        // 末尾の余韻と先頭の出だしを重ねるフレーム数
        int overlapFrames = Mathf.Clamp(Mathf.RoundToInt(overlapSec * frequency), 0, activeFrames / 3);
        int finalFrames = activeFrames - overlapFrames;
        if (finalFrames <= 0)
        {
            overlapFrames = 0;
            finalFrames = activeFrames;
        }

        float[] finalData = new float[finalFrames * channels];

        // メイン区間の波形をコピー
        for (int frame = 0; frame < finalFrames; frame++)
        {
            int srcBase = (trimStartFrame + frame) * channels;
            int dstBase = frame * channels;
            for (int ch = 0; ch < channels; ch++)
            {
                finalData[dstBase + ch] = rawData[srcBase + ch];
            }
        }

        // ★重要：先頭の出だしの音量は100%維持したまま、末尾の余韻をその上に重ねて繋ぎ目の隙間を完全に消す
        if (overlapFrames > 0)
        {
            // 先頭の最初のフレームで「プツッ」と鳴らないよう極小（約2ミリ秒）のスムージングだけかける
            int microFadeFrames = Mathf.Min(overlapFrames, Mathf.Max(1, frequency / 500));

            for (int f = 0; f < overlapFrames; f++)
            {
                float tailFadeOut = 1f - ((float)f / overlapFrames); // 1.0 -> 0.0
                float headAttack = (f < microFadeFrames) ? ((float)f / microFadeFrames) : 1f;

                int headDstBase = f * channels;
                int tailSrcBase = (trimStartFrame + finalFrames + f) * channels;

                for (int ch = 0; ch < channels; ch++)
                {
                    float headSample = finalData[headDstBase + ch] * headAttack;
                    float tailSample = rawData[tailSrcBase + ch] * tailFadeOut;

                    // 2つの音を重ね合わせ、音割れしない範囲に収める
                    finalData[headDstBase + ch] = Mathf.Clamp(headSample + tailSample, -1f, 1f);
                }
            }

            // 終端フレームから先頭フレームへ戻る境界の振幅差をゼロに揃える（クリックノイズ防止）
            int endSmoothFrames = Mathf.Min(finalFrames / 10, Mathf.Max(1, frequency / 500));
            for (int f = 0; f < endSmoothFrames; f++)
            {
                float ratio = (float)f / endSmoothFrames;
                int dstFrame = (finalFrames - endSmoothFrames) + f;
                int dstBase = dstFrame * channels;
                int nextLoopHeadSrcBase = (trimStartFrame + finalFrames) * channels;

                for (int ch = 0; ch < channels; ch++)
                {
                    finalData[dstBase + ch] = Mathf.Lerp(finalData[dstBase + ch], rawData[nextLoopHeadSrcBase + ch], ratio * 0.5f);
                }
            }
        }

        AudioClip newClip = AudioClip.Create(sourceClip.name + "_SeamlessLoop", finalFrames, channels, frequency, false);
        newClip.SetData(finalData, 0);
        return newClip;
    }

    private float GetWindowMaxAmplitude(float[] data, int startFrame, int windowFrames, int channels)
    {
        float maxAmp = 0f;
        int endFrame = startFrame + windowFrames;
        for (int f = startFrame; f < endFrame; f++)
        {
            int baseIdx = f * channels;
            if (baseIdx + channels > data.Length) break;
            for (int ch = 0; ch < channels; ch++)
            {
                float amp = Mathf.Abs(data[baseIdx + ch]);
                if (amp > maxAmp) maxAmp = amp;
            }
        }
        return maxAmp;
    }

    public void StopBGM()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void OnDestroy()
    {
        if (generatedLoopClip != null)
        {
            Destroy(generatedLoopClip);
        }
    }
}