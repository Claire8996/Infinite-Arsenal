using UnityEngine;

// 他のスクリプト（StageWeaponDisplayなど）のStart処理より後に実行して確実に上書きする設定
[DefaultExecutionOrder(100)]
public class TutorialWeaponEquipper : MonoBehaviour
{
    [Header("【チュートリアル用の銃設定】")]
    [Tooltip("チュートリアルで無条件に撃てるようにする銃のPrefabをここにセットしてください")]
    public GameObject tutorialWeaponPrefab;

    [Header("【プレイヤー連携設定（空欄でも自動取得します）】")]
    [Tooltip("プレイヤーに付いている WeaponManager（未設定の場合はシーン内から自動で探します）")]
    public WeaponManager playerWeaponManager;

    void Start()
    {
        EquipTutorialWeapon();
    }

    public void EquipTutorialWeapon()
    {
        if (tutorialWeaponPrefab == null)
        {
            Debug.LogWarning("TutorialWeaponEquipper: チュートリアル用の銃PrefabがInspectorに設定されていません！");
            return;
        }

        // 1. WeaponManager が未設定なら、同じオブジェクトかシーン内から自動で探す
        if (playerWeaponManager == null)
        {
            playerWeaponManager = GetComponent<WeaponManager>();
        }
        if (playerWeaponManager == null)
        {
            playerWeaponManager = FindObjectOfType<WeaponManager>();
        }

        if (playerWeaponManager == null)
        {
            Debug.LogWarning("TutorialWeaponEquipper: シーン内に WeaponManager が見つかりません！プレイヤーに WeaponManager が付いているか確認してください。");
            return;
        }

        // 2. もしUI用のPrefab（WeaponModelLinker付き）がセットされていた場合は、実際の3D銃Prefabを取り出す
        // （直接3D銃のPrefabがセットされていた場合はそのまま使うので、どちらを入れても動きます）
        GameObject actualGunPrefab = tutorialWeaponPrefab;
        WeaponModelLinker linker = tutorialWeaponPrefab.GetComponentInChildren<WeaponModelLinker>(true);
        if (linker != null && linker.actualWeaponPrefab != null)
        {
            actualGunPrefab = linker.actualWeaponPrefab;
        }

        // 3. WeaponManager に銃を渡して無条件で装備・出現させる
        playerWeaponManager.ChangeWeapon(actualGunPrefab);

        Debug.Log($"チュートリアル用の銃「{actualGunPrefab.name}」を装備しました！");
    }
}