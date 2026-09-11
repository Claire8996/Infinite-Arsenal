using UnityEngine;
using UnityEngine.UI; // UI（Image）を操作するために必要
using TMPro;          // TextMeshPro（テキスト）を操作するために必要

public class PlayerHealth : MonoBehaviour
{
    [Header("プレイヤーのステータス")]
    [Tooltip("初期体力（最大HP）")]
    public int maxHealth = 5000;

    // 現在のHP
    private int currentHealth;

    [Header("UI設定")]
    [Tooltip("HPが減る赤いゲージのImage")]
    public Image hpBar;

    [Tooltip("現在のHPを表示するテキスト")]
    public TextMeshProUGUI hpText;

    void Start()
    {
        // ゲーム開始時に、現在のHPを最大HPと同じ（5000）にする
        currentHealth = maxHealth;

        // 最初のUI表示を更新する
        UpdateUI();
    }

    public void TakeDamage(int damageAmount)
    {
        // ダメージ分だけHPを減らす
        currentHealth -= damageAmount;

        // HPがマイナスにならないよう、0以下なら0にする
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        // ダメージを受けたのでUIの表示を更新する
        UpdateUI();

        // HPが0になったら死亡処理を呼ぶ
        if (currentHealth == 0)
        {
            Die();
        }
    }

    // ★追加：HPバーとテキストの表示を更新する専用メソッド
    void UpdateUI()
    {
        // 1. バーを減らす処理（0.0〜1.0の割合で指定するため、割り算をする）
        if (hpBar != null)
        {
            hpBar.fillAmount = (float)currentHealth / maxHealth;
        }

        // 2. テキストを更新する処理（例：「5000 / 5000」のように表示）
        if (hpText != null)
        {
            hpText.text = currentHealth.ToString() + " / " + maxHealth.ToString();
        }
    }

    void Die()
    {
        Debug.Log("プレイヤーは倒れた……（ゲームオーバー）");
    }
}