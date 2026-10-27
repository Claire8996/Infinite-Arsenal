using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageWeaponDisplay : MonoBehaviour
{
    [Header("【連携設定】")]
    [Tooltip("プレイヤーについている WeaponManager をドラッグ＆ドロップしてください")]
    public WeaponManager playerWeaponManager;

    [Header("【武器ランク配列（Weapon SelectSceneから自動同期されます）】")]
    public WeaponRankLine[] weaponRankLines;

    // 生成した武器アイコンを管理・記憶する配列
    private GameObject[] spawnedWeaponIcons = new GameObject[3];

    // 現在装備中のスロット番号（0 = Slot1, 1 = Slot2, 2 = Slot3）
    private int currentActiveSlotIndex = 0;

    // セーブ・ロード用のキー（WeaponInventoryManagerと共通・座標とインデックスのみ使用）
    private const string SAVE_KEY_EQUIPPED_WEAPONS = "SavedInventory_EquippedWeapons_Indices";
    private const string SAVE_KEY_WEAPONS_COORDS = "SavedInventory_Weapons_ExactCoords";
    private const string SAVE_KEY_EQUIPPED_WEAPON_COORDS = "SavedInventory_EquippedWeapons_Coords";

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            SyncWeaponRankLinesFromSelectScene();
        }
    }

    // Weapon SelectScene 内の WeaponInventoryManager に設定されている weaponRankLines をGUIDベースで自動同期する（名前検索は一切使用しない）
    private void SyncWeaponRankLinesFromSelectScene()
    {
        WeaponInventoryManager inv = FindObjectOfType<WeaponInventoryManager>();
        if (inv != null && inv.weaponRankLines != null && inv.weaponRankLines.Length > 0)
        {
            weaponRankLines = inv.weaponRankLines;
            return;
        }

        string[] sceneGuids = UnityEditor.AssetDatabase.FindAssets("t:Scene");
        foreach (string sceneGuid in sceneGuids)
        {
            string scenePath = UnityEditor.AssetDatabase.GUIDToAssetPath(sceneGuid);
            if (string.IsNullOrEmpty(scenePath) || !scenePath.EndsWith(".unity")) continue;

            string fileText = System.IO.File.ReadAllText(scenePath);
            int idx = fileText.IndexOf("weaponRankLines:");
            if (idx < 0) continue;

            string[] lines = fileText.Substring(idx).Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            List<WeaponRankLine> parsedLines = new List<WeaponRankLine>();
            List<GameObject> currentRanks = null;
            bool inRanks = false;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];

                // 次のシリアライズ変数に到達したら終了
                if (line.StartsWith("  ") && !line.StartsWith("  -") && !line.StartsWith("    "))
                {
                    break;
                }

                if (line.Contains("- lineName:"))
                {
                    if (currentRanks != null)
                    {
                        WeaponRankLine wrl = new WeaponRankLine();
                        wrl.ranks = currentRanks.ToArray();
                        parsedLines.Add(wrl);
                    }
                    currentRanks = new List<GameObject>();
                    inRanks = false;
                }
                else if (line.Contains("ranks:"))
                {
                    inRanks = true;
                }
                else if (inRanks && line.TrimStart().StartsWith("- {"))
                {
                    int guidIdx = line.IndexOf("guid: ");
                    if (guidIdx >= 0 && guidIdx + 38 <= line.Length)
                    {
                        string guid = line.Substring(guidIdx + 6, 32);
                        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                        if (currentRanks != null) currentRanks.Add(prefab);
                    }
                    else
                    {
                        if (currentRanks != null) currentRanks.Add(null);
                    }
                }
            }

            if (currentRanks != null)
            {
                WeaponRankLine wrl = new WeaponRankLine();
                wrl.ranks = currentRanks.ToArray();
                parsedLines.Add(wrl);
            }

            if (parsedLines.Count > 0)
            {
                weaponRankLines = parsedLines.ToArray();
                break;
            }
        }
    }
#endif

    void Start()
    {
        if (playerWeaponManager == null)
        {
            playerWeaponManager = FindObjectOfType<WeaponManager>();
        }

#if UNITY_EDITOR
        if (weaponRankLines == null || weaponRankLines.Length == 0)
        {
            SyncWeaponRankLinesFromSelectScene();
        }
#endif

        // Weapon SelectSceneを経由していなくても、座標セーブデータから装備武器を復元する
        LoadSavedEquippedWeaponsForStage();

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

    // 名前を一切使わず、座標（groupIdx:rankIdx）とインデックスから装備武器を復元するメソッド
    private void LoadSavedEquippedWeaponsForStage()
    {
        if (WeaponDataKeeper.equippedWeaponPrefabs == null || WeaponDataKeeper.equippedWeaponPrefabs.Length < 3)
        {
            WeaponDataKeeper.equippedWeaponPrefabs = new GameObject[3];
        }

        // すでに有効な武器データが入っている場合はそのまま使用（防具が誤って入っている場合は破棄）
        bool hasValidEquipped = false;
        for (int i = 0; i < 3; i++)
        {
            GameObject w = WeaponDataKeeper.equippedWeaponPrefabs[i];
            if (w != null)
            {
                if (w.GetComponentInChildren<WeaponModelLinker>(true) != null)
                {
                    hasValidEquipped = true;
                }
                else
                {
                    WeaponDataKeeper.equippedWeaponPrefabs[i] = null;
                }
            }
        }
        if (hasValidEquipped) return;

        // 1. 直接保存された装備スロットの座標（groupIdx:rankIdx）から復元
        if (PlayerPrefs.HasKey(SAVE_KEY_EQUIPPED_WEAPON_COORDS))
        {
            string equippedCoordsStr = PlayerPrefs.GetString(SAVE_KEY_EQUIPPED_WEAPON_COORDS, "");
            if (!string.IsNullOrEmpty(equippedCoordsStr))
            {
                string[] slotCoords = equippedCoordsStr.Split(',');
                bool restoredAny = false;
                for (int s = 0; s < slotCoords.Length && s < 3; s++)
                {
                    string[] parts = slotCoords[s].Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int gIdx) && int.TryParse(parts[1], out int rIdx))
                    {
                        if (gIdx >= 0 && rIdx >= 0)
                        {
                            GameObject prefab = GetWeaponPrefabByCoordinates(gIdx, rIdx);
                            if (prefab != null)
                            {
                                WeaponDataKeeper.equippedWeaponPrefabs[s] = prefab;
                                restoredAny = true;
                            }
                        }
                    }
                }
                if (restoredAny) return;
            }
        }

        // 2. 所持武器座標リスト（SAVE_KEY_WEAPONS_COORDS）と装備インデックス（SAVE_KEY_EQUIPPED_WEAPONS）から復元
        if (PlayerPrefs.HasKey(SAVE_KEY_EQUIPPED_WEAPONS) && PlayerPrefs.HasKey(SAVE_KEY_WEAPONS_COORDS))
        {
            string savedIndicesStr = PlayerPrefs.GetString(SAVE_KEY_EQUIPPED_WEAPONS, "");
            string savedCoordsStr = PlayerPrefs.GetString(SAVE_KEY_WEAPONS_COORDS, "");

            if (!string.IsNullOrEmpty(savedIndicesStr) && !string.IsNullOrEmpty(savedCoordsStr))
            {
                string[] coordCodes = savedCoordsStr.Split(',');
                List<GameObject> restoredWeaponList = new List<GameObject>();

                foreach (string code in coordCodes)
                {
                    string[] parts = code.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int groupIdx) && int.TryParse(parts[1], out int rankIdx))
                    {
                        GameObject prefab = GetWeaponPrefabByCoordinates(groupIdx, rankIdx);
                        if (prefab != null) restoredWeaponList.Add(prefab);
                    }
                }

                string[] indexParts = savedIndicesStr.Split(',');
                for (int s = 0; s < indexParts.Length && s < 3; s++)
                {
                    if (int.TryParse(indexParts[s], out int invIdx))
                    {
                        if (invIdx >= 0 && invIdx < restoredWeaponList.Count)
                        {
                            WeaponDataKeeper.equippedWeaponPrefabs[s] = restoredWeaponList[invIdx];
                        }
                    }
                }
            }
        }
    }

    // 座標（groupIdx, rankIdx）から該当の武器Prefabを取得するメソッド
    private GameObject GetWeaponPrefabByCoordinates(int groupIdx, int rankIdx)
    {
        if (weaponRankLines != null && groupIdx >= 0 && groupIdx < weaponRankLines.Length)
        {
            var line = weaponRankLines[groupIdx];
            if (line != null && line.ranks != null && rankIdx >= 0 && rankIdx < line.ranks.Length)
            {
                return line.ranks[rankIdx];
            }
        }
        return null;
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
        // 選択されたスロットのPrefabから、実際の3Dモデルを取得して装備する
        // ==========================================
        if (playerWeaponManager != null)
        {
            GameObject selectedSlotPrefab = WeaponDataKeeper.equippedWeaponPrefabs[currentActiveSlotIndex];

            if (selectedSlotPrefab != null)
            {
                WeaponModelLinker linker = selectedSlotPrefab.GetComponentInChildren<WeaponModelLinker>(true);

                if (linker != null && linker.actualWeaponPrefab != null)
                {
                    playerWeaponManager.ChangeWeapon(linker.actualWeaponPrefab);
                }
            }
        }
    }
}