using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("武器の設定")]
    [Tooltip("武器を表示する位置（座標と向きの基準用）")]
    public Transform weaponHoldPosition;

    [Tooltip("装備する銃のプレハブ")]
    public GameObject weaponPrefab;

    private GameObject currentWeapon;

    void Start()
    {
        EquipWeapon();
    }

    // 武器を装備・表示する処理
    public void EquipWeapon()
    {
        if (weaponPrefab == null || weaponHoldPosition == null)
        {
            Debug.LogWarning("WeaponManager: 銃のプレハブかWeapon Hold Positionが設定されていません。");
            return;
        }

        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
        }

        // 1. まず weaponHoldPosition の位置と回転に合わせて銃を生成する
        currentWeapon = Instantiate(weaponPrefab, weaponHoldPosition.position, weaponHoldPosition.rotation);

        // 2. 生成した銃の親を「このスクリプトがアタッチされているオブジェクト（Player）」に設定する
        currentWeapon.transform.SetParent(transform);
    }
}