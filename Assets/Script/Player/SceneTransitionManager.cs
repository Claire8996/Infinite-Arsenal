using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("フェード演出の設定")]
    [Tooltip("画面全体を覆う真っ黒な画像（Image）")]
    public Image fadeImage;
    [Tooltip("暗転・明転にかかる時間（秒）")]
    public float fadeDuration = 0.8f;
    [Tooltip("シーン移動前、スタミナ消費を見せるための待機時間")]
    public float waitBeforeFade = 0.5f;

    [Header("スタミナ連携")]
    [Tooltip("EnergyManagerスクリプトが付いているオブジェクト")]
    public EnergyManager energyManager;
    [Tooltip("このボタンで移動する際に消費するスタミナ量")]
    public int requiredEnergy = 10;

    // 連打防止用フラグ
    private bool isTransitioning = false;

    void Start()
    {
        // ★シーン開始時に、真っ暗な状態から徐々に明るくする（フェードイン）
        if (fadeImage != null)
        {
            StartCoroutine(FadeInSequence());
        }
    }

    public void LoadTargetScene(string sceneName)
    {
        // 既に移動中なら何もしない（ボタンの連打バグ防止）
        if (isTransitioning) return;

        // スタミナが足りるかチェック
        if (energyManager != null)
        {
            if (!energyManager.HasEnoughEnergy(requiredEnergy))
            {
                Debug.Log("スタミナが足りません！");
                return; // ★スタミナ不足なら移動させない
            }
        }

        // シーン移動の演出スタート
        StartCoroutine(TransitionSequence(sceneName));
    }

    private IEnumerator TransitionSequence(string sceneName)
    {
        isTransitioning = true;

        // 1. エネルギーを消費する
        if (energyManager != null)
        {
            energyManager.UseEnergy(requiredEnergy);

            // ゲージが減ったのを見せるために少し待つ
            yield return new WaitForSeconds(waitBeforeFade);
        }

        // 2. 画面を徐々に暗くする（フェードアウト）
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float timer = 0f;
            Color color = fadeImage.color;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                // 透明度(Alpha)を 0(透明) から 1(真っ黒) へ
                color.a = Mathf.Clamp01(timer / fadeDuration);
                fadeImage.color = color;
                yield return null; // 1フレーム待つ
            }

            color.a = 1f; // 確実に真っ黒にする
            fadeImage.color = color;
        }

        // 3. 次のシーンを読み込む
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeInSequence()
    {
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        color.a = 1f; // 最初は真っ黒にしておく
        fadeImage.color = color;

        float timer = 0f;

        // 画面を徐々に明るくする（フェードイン）
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // 透明度(Alpha)を 1(真っ黒) から 0(透明) へ
            color.a = Mathf.Clamp01(1f - (timer / fadeDuration));
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f; // 確実に透明にする
        fadeImage.color = color;

        // 画面が明るくなったら、ボタンを押せるように非表示にする
        fadeImage.gameObject.SetActive(false);
    }
}