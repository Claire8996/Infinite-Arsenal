using UnityEngine;

public class EquippedWeaponDisplay : MonoBehaviour
{
    [Header("【武器を表示する親オブジェクト】")]
    [Tooltip("武器を子オブジェクトとして配置したい場所（プレイヤーの手や、専用の空オブジェクトなど）")]
    public Transform parentObject;

    [Header("【スロットごとの表示位置（Transform）】")]
    [Tooltip("スロット1の武器を表示する位置・回転の基準となるオブジェクト")]
    public Transform positionSlot1;
    [Tooltip("スロット2の武器を表示する位置・回転の基準となるオブジェクト")]
    public Transform positionSlot2;
    [Tooltip("スロット3の武器を表示する位置・回転の基準となるオブジェクト")]
    public Transform positionSlot3;

    // 現在表示している武器のインスタンス（実体）を保持しておく配列
    private GameObject[] spawnedWeapons = new GameObject[3];

    private void Start()
    {
        // 念のため親オブジェクトが設定されていなければ、このスクリプトがついているオブジェクトを親にする
        if (parentObject == null)
        {
            parentObject = transform;
        }

        // シーンが始まった時に、保存されている武器を表示する
        DisplayEquippedWeapons();
    }

    private void DisplayEquippedWeapons()
    {
        // 倍に増えるのを防ぐため、各スロット位置（親）に入っている
        // 古い武器オブジェクトを、生成前に強制的に全部お掃除（Destroy）する
        ClearOldWeapons(positionSlot1);
        ClearOldWeapons(positionSlot2);
        ClearOldWeapons(positionSlot3);
        if (positionSlot1 == null && positionSlot2 == null && positionSlot3 == null)
        {
            ClearOldWeapons(parentObject);
        }

        // WeaponDataKeeper に保存されている武器の数だけループ（基本は3つ）
        for (int i = 0; i < WeaponDataKeeper.equippedWeaponPrefabs.Length; i++)
        {
            // 保存されている武器のPrefabを取得
            GameObject weaponPrefab = WeaponDataKeeper.equippedWeaponPrefabs[i];

            if (weaponPrefab != null)
            {
                // 対応する表示位置を決める
                Transform targetPosition = null;
                if (i == 0) targetPosition = positionSlot1;
                else if (i == 1) targetPosition = positionSlot2;
                else if (i == 2) targetPosition = positionSlot3;

                // もし位置が設定されていなければ親オブジェクトと同じ場所にする
                if (targetPosition == null) targetPosition = parentObject;

                // 武器を生成して、ターゲット位置の子にする（※Instantiateの書き方をUIに強い形式に変更）
                GameObject newWeapon = Instantiate(weaponPrefab, targetPosition);
                newWeapon.transform.localPosition = Vector3.zero;
                newWeapon.transform.localRotation = Quaternion.identity;

                // サイズ変更を確実に行うため、RectTransformのスケールもリセットしてから適用する
                RectTransform rectT = newWeapon.GetComponent<RectTransform>();
                if (rectT != null)
                {
                    rectT.localScale = Vector3.one; // スケールが変になるのを防ぐ
                }

                // ==========================================
                // 💡 究極の修正：親だけでなく、子オブジェクトに付いているスクリプトも全部探し出して実行する！
                // ==========================================
                WeaponSizeAdjuster[] sizeAdjusters = newWeapon.GetComponentsInChildren<WeaponSizeAdjuster>(true);
                foreach (WeaponSizeAdjuster adjuster in sizeAdjusters)
                {
                    adjuster.ApplySize();
                }

                // 生成した武器を配列に覚えておく（後で消したり切り替えたりするため）
                spawnedWeapons[i] = newWeapon;

                Debug.Log($"スロット {i + 1} に {newWeapon.name} を表示しました！");
            }
        }
    }

    // スロット位置の子供になっている古い武器を全部消すお掃除メソッド
    private void ClearOldWeapons(Transform slotTransform)
    {
        if (slotTransform == null) return;

        // 子オブジェクトを後ろから順番に消していく
        for (int j = slotTransform.childCount - 1; j >= 0; j--)
        {
            Destroy(slotTransform.GetChild(j).gameObject);
        }
    }

    // ゲーム中に武器を持ち替えるなどの演出をしたい時に使うメソッド
    public void SwitchActiveWeapon(int slotIndex)
    {
        for (int i = 0; i < spawnedWeapons.Length; i++)
        {
            if (spawnedWeapons[i] != null)
            {
                // 指定されたスロットの武器だけを表示（アクティブ）にし、他は非表示にする
                spawnedWeapons[i].SetActive(i == slotIndex);
            }
        }
    }
}