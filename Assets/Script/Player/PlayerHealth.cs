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

    // 防具の体力ボーナスを加味した最終的な最大HP
    private int totalMaxHealth;

    [Header("UI設定")]
    [Tooltip("HPが減る赤いゲージのImage")]
    public Image hpBar;

    [Tooltip("現在のHPを表示するテキスト（5000 / 5000 形式）")]
    public TextMeshProUGUI hpText;

    // =========================================================
    // ★追加：ステージ画面で現在の体力・防御力・移動速度を数字単体で表示したい場合の参照欄
    // =========================================================
    [Header("ステータス数値テキスト設定（3つ）")]
    [Tooltip("現在の体力を数字だけで表示するテキスト")]
    public TextMeshProUGUI healthValueText;

    [Tooltip("現在の防御力を数字だけで表示するテキスト")]
    public TextMeshProUGUI defenseValueText;

    [Tooltip("現在の移動速度を数字だけで表示するテキスト")]
    public TextMeshProUGUI speedValueText;

    void Start()
    {
        // 基本の最大HPをArmorStatsへ同期する
        ArmorStats.BaseMaxHealth = maxHealth;

        // =========================================================
        // 初期体力に防具の「体力増加ボーナス（実数値）」を加算する
        // =========================================================
        totalMaxHealth = maxHealth + ArmorStats.TotalHealthBonus;

        // ゲーム開始時に、現在のHPをボーナス込みの最大HPと同じにする
        currentHealth = totalMaxHealth;

        // 最初のUI表示を更新する
        UpdateUI();
    }

    public void TakeDamage(int damageAmount)
    {
        // =========================================================
        // 受けるダメージから防具の「防御力ボーナス（実数値）」を引く
        // （防御力が高くてマイナスになりHPが回復してしまうのを防ぐため、最低0にする）
        // =========================================================
        int finalDamage = Mathf.Max(0, damageAmount - ArmorStats.TotalDefenseBonus);

        // ダメージ分だけHPを減らす
        currentHealth -= finalDamage;

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

    // HPバーとテキストの表示を更新する専用メソッド
    void UpdateUI()
    {
        // 1. バーを減らす処理（0.0〜1.0の割合で指定するため、割り算をする）
        if (hpBar != null)
        {
            hpBar.fillAmount = (float)currentHealth / totalMaxHealth;
        }

        // 2. テキストを更新する処理（例：「5500 / 5500」のように表示）
        if (hpText != null)
        {
            hpText.text = currentHealth.ToString() + " / " + totalMaxHealth.ToString();
        }

        // 3. ★追加：3つのステータステキスト（体力・防御力・移動速度）を数字で更新する
        if (healthValueText != null)
        {
            healthValueText.text = currentHealth.ToString();
        }

        if (defenseValueText != null)
        {
            defenseValueText.text = ArmorStats.CurrentTotalDefense.ToString();
        }

        if (speedValueText != null)
        {
            speedValueText.text = ArmorStats.CurrentTotalSpeed.ToString();
        }
    }

    void Die()
    {
        Debug.Log("プレイヤーは倒れた……（ゲームオーバー）");
    }
}