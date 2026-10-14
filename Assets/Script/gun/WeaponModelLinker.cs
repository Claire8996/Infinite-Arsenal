using UnityEngine;

// UI（スロット）用のPrefabにアタッチし、実際に手に出す3Dモデルを指定するスクリプト
public class WeaponModelLinker : MonoBehaviour
{
    [Header("【実際の武器3Dモデル】")]
    [Tooltip("この枠が選ばれた時に、WeaponManagerの weaponPrefab に渡される実体のPrefab")]
    public GameObject actualWeaponPrefab;
}