using System.Collections.Generic;
using UnityEngine;

public class ArmorStats : MonoBehaviour
{
    [Header("【防具のステータス強化設定（実数値）】")]
    [Tooltip("攻撃力の増加量（弾のダメージにそのまま加算されます）")]
    public int attackBonus = 0;

    [Tooltip("防御力の増加量（受けるダメージからこの数値分だけ軽減されます）")]
    public int defenseBonus = 0;

    [Tooltip("プレイヤーの最大体力（HP）の増加量")]
    public int healthBonus = 0;

    [Tooltip("プレイヤーの移動速度（歩き・走り）の増加量")]
    public float speedBonus = 0f;

    // =========================================================
    // 装備中の防具ステータスをシーン間で保持・合計するシステム
    // =========================================================
    private static Dictionary<int, ArmorStats> equippedArmors = new Dictionary<int, ArmorStats>();

    // 全防具の合計ボーナス値
    public static int TotalAttackBonus { get; private set; } = 0;
    public static int TotalDefenseBonus { get; private set; } = 0;
    public static int TotalHealthBonus { get; private set; } = 0;
    public static float TotalSpeedBonus { get; private set; } = 0f;

    // =========================================================
    // ★追加：プレイヤーの基本ステータス（PlayerHealth / PlayerMovement の初期値と同期）
    // =========================================================
    public static int BaseMaxHealth = 5000;
    public static int BaseDefense = 0;
    public static float BaseWalkSpeed = 5f;

    // ボーナス込みの現在のステータスを取得するプロパティ
    public static int CurrentTotalHealth => BaseMaxHealth + TotalHealthBonus;
    public static int CurrentTotalDefense => BaseDefense + TotalDefenseBonus;
    public static float CurrentTotalSpeed => BaseWalkSpeed + TotalSpeedBonus;

    // インベントリで防具が選択された時に呼ばれ、装備状態を更新するメソッド
    public static void EquipArmor(int categoryIndex, ArmorStats newArmorStats)
    {
        if (newArmorStats != null)
        {
            equippedArmors[categoryIndex] = newArmorStats;
        }
        else if (equippedArmors.ContainsKey(categoryIndex))
        {
            equippedArmors.Remove(categoryIndex);
        }

        RecalculateTotals();
    }

    // 装備中のすべての防具の効果を合計する処理
    private static void RecalculateTotals()
    {
        TotalAttackBonus = 0;
        TotalDefenseBonus = 0;
        TotalHealthBonus = 0;
        TotalSpeedBonus = 0f;

        foreach (var pair in equippedArmors)
        {
            ArmorStats stats = pair.Value;
            if (stats != null)
            {
                TotalAttackBonus += stats.attackBonus;
                TotalDefenseBonus += stats.defenseBonus;
                TotalHealthBonus += stats.healthBonus;
                TotalSpeedBonus += stats.speedBonus;
            }
        }

        Debug.Log($"【防具効果合計】攻撃+{TotalAttackBonus} / 防御+{TotalDefenseBonus} / HP+{TotalHealthBonus} / 速度+{TotalSpeedBonus}");
    }
}