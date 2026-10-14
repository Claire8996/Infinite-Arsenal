using UnityEngine;

public class ArmorItemLinker : MonoBehaviour
{
    [Header("【Content内に表示する防具Prefab】")]
    [Tooltip("ガチャでこの防具が当たった際、WeaponInventoryManagerのContent内に表示させたいPrefabをここにセットしてください")]
    public GameObject contentDisplayPrefab;
}