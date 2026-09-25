using UnityEngine;

// static（静的）にすることで、シーンが変わってもデータが消えなくなります！
public static class WeaponDataKeeper
{
    // スロット1〜3で選ばれた武器プレハブを記憶する配列
    public static GameObject[] equippedWeaponPrefabs = new GameObject[3];
}