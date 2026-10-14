using UnityEngine;
using UnityEngine.UI;

public class StageWeaponDisplay : MonoBehaviour
{
    [Header("【連携設定】")]
    [Tooltip("プレイヤーについている WeaponManager をドラッグ＆ドロップしてください")]
    public WeaponManager playerWeaponManager;

    // 生成した武器アイコンを管理・記憶する配列
    private GameObject[] spawnedWeaponIcons = new GameObject[3];

    // 現在装備中のスロット番号（0 = Slot1, 1 = Slot2, 2 = Slot3）
    private int currentActiveSlotIndex = 0;

    void Start()
    {
        // ご要望の座標を設定（Z軸は0）
        Vector3[] targetPositions = new Vector3[3]
        {
            new Vector3(-188.2999f, -10.62f, 0f),
            new Vector3(12.06f, -11.36f, 0f),
            new Vector3(204.76f, -9.17f, 0f)
        };

        // スロット1〜3（配列の0〜2）の武器を取り出して表示
        for (int i = 0; i < 3; i++)
        {
            GameObject weaponPrefab = WeaponDataKeeper.equippedWeaponPrefabs[i];

            if (weaponPrefab != null)
            {
                // 自分（インベントリ欄）の子オブジェクトとして生成
                GameObject weaponIcon = Instantiate(weaponPrefab, this.transform);

                // 指定された座標に配置
                weaponIcon.transform.localPosition = targetPositions[i];

                // サイズが崩れないようにリセット
                weaponIcon.transform.localScale = Vector3.one;

                // 戦闘画面ではボタンとしての機能は邪魔になるので削除する
                WeaponItemUI itemUI = weaponIcon.GetComponent<WeaponItemUI>();
                if (itemUI != null) Destroy(itemUI);

                Button btn = weaponIcon.GetComponent<Button>();
                if (btn != null) Destroy(btn);

                // 生成したアイコンを配列に記憶しておく（後で色を変えるため）
                spawnedWeaponIcons[i] = weaponIcon;
            }
        }

        // ゲーム開始時は強制的に「Slot1（インデックス0）」を選択状態にする
        SetActiveSlot(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SetActiveSlot(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SetActiveSlot(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SetActiveSlot(2);
        }

        float scrollAmount = Input.GetAxis("Mouse ScrollWheel");

        if (scrollAmount > 0f)
        {
            int nextSlot = currentActiveSlotIndex + 1;
            if (nextSlot > 2) nextSlot = 0;
            SetActiveSlot(nextSlot);
        }
        else if (scrollAmount < 0f)
        {
            int prevSlot = currentActiveSlotIndex - 1;
            if (prevSlot < 0) prevSlot = 2;
            SetActiveSlot(prevSlot);
        }
    }

    // スロットの選択状態と色を更新し、武器を持ち替えるメソッド
    void SetActiveSlot(int slotIndex)
    {
        currentActiveSlotIndex = slotIndex;

        for (int i = 0; i < 3; i++)
        {
            if (spawnedWeaponIcons[i] != null)
            {
                Image[] images = spawnedWeaponIcons[i].GetComponentsInChildren<Image>();

                foreach (Image img in images)
                {
                    if (i == currentActiveSlotIndex)
                    {
                        img.color = Color.white;
                    }
                    else
                    {
                        img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                }
            }
        }

        // ==========================================
        // ★追加：選択されたスロットのPrefabから、実際の3Dモデルを取得して装備する
        // ==========================================
        if (playerWeaponManager != null)
        {
            // インベントリから引き継いだ元のPrefabデータを確認
            GameObject selectedSlotPrefab = WeaponDataKeeper.equippedWeaponPrefabs[currentActiveSlotIndex];

            if (selectedSlotPrefab != null)
            {
                // アタッチした新しいスクリプト（WeaponModelLinker）を見つける
                WeaponModelLinker linker = selectedSlotPrefab.GetComponent<WeaponModelLinker>();

                if (linker != null && linker.actualWeaponPrefab != null)
                {
                    // WeaponManager の weaponPrefab に安全に上書きし、再装備させる
                    playerWeaponManager.ChangeWeapon(linker.actualWeaponPrefab);
                }
            }
        }
    }
}