using UnityEngine;
using UnityEngine.UI;

// このスクリプトを付けると、自動的にButtonコンポーネントも一緒についてきます
[RequireComponent(typeof(Button))]
public class WeaponItemUI : MonoBehaviour
{
    private WeaponInventoryManager manager;

    // ★追加：自分のもとになったプレハブを記憶しておくための枠
    private GameObject myWeaponPrefab;

    // ===============================================
    // ★改修：マネージャーから初期設定を受け取る際、プレハブの情報も一緒に受け取る
    // ===============================================
    public void Setup(WeaponInventoryManager mgr, GameObject prefab)
    {
        manager = mgr;
        myWeaponPrefab = prefab; // 受け取ったプレハブの情報を自分の中に記憶しておく

        // ボタンが押された時の処理を登録する
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClickWeapon);
    }

    // 武器がクリックされた時に呼ばれる
    void OnClickWeapon()
    {
        if (manager != null)
        {
            // ★改修：マネージャーに「自分が選択されたよ！」と伝える際、
            // 一緒に「自分のプレハブ情報」も渡してあげる！
            manager.SelectWeapon(this.transform, myWeaponPrefab);
        }
    }
}