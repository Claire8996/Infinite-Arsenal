using UnityEngine;
using UnityEngine.UI;

public class StageWeaponDisplay : MonoBehaviour
{
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
        // ==========================================
        // 1. 数字キーでの切り替えチェック
        // ==========================================
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

        // ==========================================
        // ★追加：2. マウスホイールでの切り替えチェック
        // ==========================================
        float scrollAmount = Input.GetAxis("Mouse ScrollWheel");

        if (scrollAmount > 0f)
        {
            // 上に回した時：次のスロットへ（3の次は1に戻るようにループさせる）
            int nextSlot = currentActiveSlotIndex + 1;
            if (nextSlot > 2) nextSlot = 0;

            SetActiveSlot(nextSlot);
        }
        else if (scrollAmount < 0f)
        {
            // 下に回した時：前のスロットへ（1の次は3に戻るようにループさせる）
            int prevSlot = currentActiveSlotIndex - 1;
            if (prevSlot < 0) prevSlot = 2;

            SetActiveSlot(prevSlot);
        }
    }

    // スロットの選択状態と色を更新するメソッド
    void SetActiveSlot(int slotIndex)
    {
        currentActiveSlotIndex = slotIndex;

        for (int i = 0; i < 3; i++)
        {
            // そのスロットに武器が存在していれば色を変える
            if (spawnedWeaponIcons[i] != null)
            {
                // その武器プレハブに含まれる「すべての画像（枠線や銃本体など）」を取得
                Image[] images = spawnedWeaponIcons[i].GetComponentsInChildren<Image>();

                foreach (Image img in images)
                {
                    if (i == currentActiveSlotIndex)
                    {
                        // 選ばれている武器：元の色（白）に戻して明るくする
                        img.color = Color.white;
                    }
                    else
                    {
                        // 選ばれていない武器：暗い灰色（グレー）にして目立たなくする
                        img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                }
            }
        }
    }
}