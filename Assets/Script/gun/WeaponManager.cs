using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("武器の設定")]
    [Tooltip("武器を表示する位置（カメラの子オブジェクト）")]
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
        // プレハブか位置が未設定ならエラーを出して処理を中断
        if (weaponPrefab == null || weaponHoldPosition == null)
        {
            Debug.LogWarning("WeaponManager: 銃のプレハブかWeapon Hold Positionが設定されていません。");
            return;
        }

        // 既に何か持っている場合は古いものを消す（将来の武器切り替え用）
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
        }

        // 銃のプレハブを生成し、weaponHoldPositionの子オブジェクトとして配置する
        currentWeapon = Instantiate(weaponPrefab, weaponHoldPosition);

        // 銃のローカル座標と回転を0にリセットし、Hold Positionの場所にぴったり合わせる
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
    }
}