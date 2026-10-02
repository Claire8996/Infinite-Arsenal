using System.Collections.Generic;
using UnityEngine;

// MonoBehaviourを継承しない、純粋なデータ置き場クラス
public static class WeaponDataKeeper
{
    // 装備中の武器（既存）
    public static GameObject[] equippedWeaponPrefabs = new GameObject[3];

    // ★追加：ガチャで引いた新しい武器を一時保存しておくリスト
    public static List<GameObject> gachaResultWeapons = new List<GameObject>();
}