using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

// ==========================================
// Display Target Prefabs の各Elementに「Prefab」と「スプライト」をセットで登録するためのクラス
// ==========================================
[System.Serializable]
public class ArmorTargetElement
{
    [Tooltip("Content内へ表示する防具のPrefab")]
    public GameObject targetPrefab;

    [Tooltip("この防具がselectionFramePrefabで選択された時に、下のTarget Armor Imageへ反映するスプライト")]
    public Sprite armorSprite;
}

[System.Serializable]
public class ContentCategoryButton
{
    public string buttonName = "ボタン名（メモ用）";

    [Tooltip("（任意）押した時に色を変えたい場合はボタンのImageをセットしてください")]
    public Image buttonImage;

    [Header("【選択時に非表示にするUI】")]
    [Tooltip("このカテゴリの防具がselectionFramePrefabで選択されたら非表示（SetActive(false)）にするUIオブジェクト")]
    public GameObject uiToHideOnSelect;

    [Header("【選択された防具のスプライトを表示するImage】")]
    [Tooltip("選択された防具のスプライト（armorSprite）をアタッチする先のImageコンポーネント")]
    public Image targetArmorImage;

    [Header("【このボタンで表示する防具とスプライトのリスト】")]
    [Tooltip("このボタンを押した時にContent内へ表示する防具Prefabと、選択時用のスプライトのペア（※ここに入っていないPrefabは表示されません）")]
    public ArmorTargetElement[] displayTargetPrefabs;
}

// ==========================================
// Slot1〜3ごとの「攻撃力・弾の飛ぶ速度・弾のブレ」テキストと表示切替ボタンの設定クラス
// ==========================================
[System.Serializable]
public class WeaponSlotStatUI
{
    public string slotName = "Slot";

    [Tooltip("このSlotのステータステキストを表示する切替ボタン（Image）")]
    public Image statSwitchButtonImage;

    [Tooltip("このSlotの「攻撃力」を数字で表示するテキスト")]
    public TextMeshProUGUI attackValueText;

    [Tooltip("This Slotの「弾の飛ぶ速度」を数字で表示するテキスト")]
    public TextMeshProUGUI bulletSpeedValueText;

    [Tooltip("This Slotの条件の「弾のブレ」を数字で表示するテキスト")]
    public TextMeshProUGUI spreadValueText;
}

// ==========================================
// 6種類のレアリティ絞り込みボタンごとの設定クラス
// ==========================================
[System.Serializable]
public class RarityFilterButton
{
    public string rarityName = "レアリティ名（メモ用）";

    [Tooltip("このレアリティで絞り込むボタン（Image）")]
    public Image buttonImage;

    [Tooltip("（任意）ボタンとは別に灰色にしたい連動UI（Image）があればここにセットしてください")]
    public Image targetUIImage;

    [Tooltip("このレアリティに該当するPrefabのリスト（※ここに入っているPrefabだけが表示されます）")]
    public GameObject[] rarityTargetPrefabs;
}

// ==========================================
// 進化（合成）に必要な条件を設定するクラス
// ==========================================
[System.Serializable]
public class EvolutionRule
{
    [Tooltip("進化元のPrefab")]
    public GameObject sourcePrefab;

    [Tooltip("このPrefabを何個選択したら進化できるか")]
    public int requiredCount = 3;

    [Header("【演出用ポジション設定】")]
    [Tooltip("この進化ルールで使用する演出用のPosition（requiredCountと同数、または複数個のTransformを登録してください）")]
    public Transform[] evolutionSpawnPositions;
}

// ==========================================
// 武器や防具の「種類ごと」に全6ランクの進化順序を登録するクラス
// ==========================================
[System.Serializable]
public class WeaponRankLine
{
    [Tooltip("武器・防具の種類名（メモ用：例「ピストル系」「アサルトライフル系」など）")]
    public string lineName = "武器グループ";

    [Tooltip("ランク1〜ランク6までのPrefabを順番に6個登録してください")]
    public GameObject[] ranks = new GameObject[6];
}

public class WeaponInventoryManager : MonoBehaviour
{
    [Header("スロット（Selectボタン）設定")]
    public Image[] slotButtons;
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;

    // ==========================================
    // Slot1〜3のボタンに対応する「個別のUI（Image等）」の参照欄
    // ==========================================
    [Header("【Slot1〜3の個別連動UI設定】")]
    [Tooltip("Slot1〜3の選択状態に合わせて色が変わるUI（Image）をそれぞれ設定してください")]
    public Image[] slotTargetImages = new Image[3];

    [Header("UI設定")]
    public Transform contentPanel;
    public GameObject selectionFramePrefab;

    // ==========================================
    // 現在の体力・防御力・移動速度を表示する3つのテキスト参照欄
    // ==========================================
    [Header("【プレイヤーのステータス表示テキスト設定（3つ）】")]
    [Tooltip("現在の体力を数字で表示するテキスト")]
    public TextMeshProUGUI healthValueText;

    [Tooltip("現在の防御力を数字で表示するテキスト")]
    public TextMeshProUGUI defenseValueText;

    [Tooltip("現在の移動速度を数字で表示するテキスト")]
    public TextMeshProUGUI speedValueText;

    // ==========================================
    // Slot1〜3の武器ステータス（攻撃力・弾速・ブレ）表示＆切替ボタン設定
    // ==========================================
    [Header("【Slot1〜3の武器ステータステキスト＆表示切替ボタン設定】")]
    [Tooltip("Element 0 = Slot1、Element 1 = Slot2、Element 2 = Slot3 のテキストと切替ボタンを設定してください")]
    public WeaponSlotStatUI[] weaponSlotStatUIs = new WeaponSlotStatUI[3];

    [Tooltip("武器ステータス切替ボタンの「選択されている」時の色")]
    public Color activeStatButtonColor = Color.white;

    [Tooltip("武器ステータス切替ボタンのアイコンなどの多様な選択状態の色（灰色）")]
    public Color inactiveStatButtonColor = Color.gray;

    [Header("所持している武器のリスト")]
    public GameObject[] testWeaponPrefabs;

    [Header("所持している防具のリスト")]
    public GameObject[] armorPrefabs;

    [Header("武器の並び順（ここに設定したPrefabの順番で並び替わります）")]
    [Tooltip("ここに設定されていない武器は、最後にまとめて表示されます")]
    public GameObject[] weaponSortOrder;

    [Header("【防具のContent表示切り替えボタン設定】")]
    [Tooltip("各ボタンを押した時に表示したい防具Prefabとスプライト、非表示にするUI、反映先のImageをそれぞれ登録してください")]
    public ContentCategoryButton[] categoryButtons;
    public Color activeCategoryColor = Color.white;
    public Color inactiveCategoryColor = Color.gray;

    // ==========================================
    // レアリティ絞り込みボタン設定（全7個：すべて表示1個 ＋ レアリティ6種類）
    // ==========================================
    [Header("【レアリティ絞り込みボタン設定（計7個）】")]
    [Tooltip("1つ目のボタン：すべてのPrefabを表示するボタン（Image）")]
    public Image showAllRarityButtonImage;

    [Tooltip("（任意）すべて表示ボタンと連動して色を変えたい別UI（Image）があればセットしてください")]
    public Image showAllRarityTargetUIImage;

    [Tooltip("残り6つのボタン：6種類のレアリティそれぞれのボタンと、該当するPrefabを登録してください")]
    public RarityFilterButton[] rarityButtons = new RarityFilterButton[6];

    [Tooltip("レアリティボタンの「選択されている」時の色（通常色）")]
    public Color activeRarityColor = Color.white;

    [Tooltip("レアリティボタンのゲージや選択されていない時の色（灰色）")]
    public Color inactiveRarityColor = Color.gray;

    // ==========================================
    // 複数選択・一括削除モード用のボタン設定
    // ==========================================
    [Header("【複数選択・削除モード設定】")]
    [Tooltip("押すと複数選択モードを開始するボタン")]
    public Button enterMultiSelectButton;

    [Tooltip("選択ボタンが押された時に非表示になり、選択モードをやめた時に再び表示される2つのボタン（GameObject）")]
    public GameObject[] buttonsToHideInMultiSelect = new GameObject[2];

    [Tooltip("3つのボタン①：今表示されているPrefabすべてをselectionFramePrefabで選択するボタン")]
    public Button selectAllButton;

    [Tooltip("（任意）すべて選択ボタンの親オブジェクトのUI")]
    public GameObject selectAllParentUI;

    [Tooltip("3つのボタン②：selectionFramePrefabで選択しているPrefabを削除するボタン")]
    public Button deleteSelectedButton;

    [Tooltip("（任意）削除ボタンの親オブジェクトのUI")]
    public GameObject deleteSelectedParentUI;

    [Tooltip("3つのボタン③：選択モードをやめて3つのボタンを非表示にし、2つのボタンを再表示するボタン")]
    public Button exitMultiSelectButton;

    [Tooltip("（任意）選択モード終了ボタンの親オブジェクトのUI")]
    public GameObject exitMultiSelectParentUI;

    // ==========================================
    // 進化（合成）システムの設定
    // ==========================================
    [Header("【武器・防具の進化（合成）システム設定】")]
    [Tooltip("進化に必要な素材の個数条件とポジション参照リスト")]
    public EvolutionRule[] evolutionRules;

    [Tooltip("【重要】種類ごとのランク順配列（Rank1〜Rank6をグループごとに設定してください）")]
    public WeaponRankLine[] weaponRankLines;

    [Tooltip("条件を満たしている時だけ押せるようになる「進化実行ボタン」")]
    public Button executeEvolutionButton;

    [Tooltip("進化ボタンのUI（Image）")]
    public Image executeEvolutionButtonImage;

    [Tooltip("条件を満たしていない時（通常時）の合成ボタンの色")]
    public Color normalEvolutionButtonColor = Color.gray;

    [Tooltip("条件を満たしている時（合成可能時）の合成ボタンの色")]
    public Color readyEvolutionButtonColor = Color.white;

    [Tooltip("進化ボタンが押せるようになった時に表示したい、ホーム画面などのとあるUI")]
    public GameObject conditionalVisibleUI;

    [Tooltip("進化演出中に画面全体を明るく光らせるためのパネル（UI Imageなど。最初は非表示に設定されます）")]
    public Image flashPanelImage;

    [Tooltip("Content内の位置から指定Positionへ移動する際にかかる時間（秒）※Inspectorで変更可能")]
    public float moveToPositionDuration = 0.5f;

    [Tooltip("ボタン①：押したらとあるUIを非表示にするボタン")]
    public Button hideButton1;

    [Tooltip("ボタン②（合成ボタン）：押したらとあるUIを非表示にして、選択してるPrefabが指定Positionへ移動し円を描いてワープする演出を開始するボタン")]
    public Button warpAnimationButton;

    [Header("【合成ボタンの親オブジェクトUI（複数選択時のみ表示用）】")]
    [Tooltip("合成ボタン（warpAnimationButton等）やその背景枠などの親オブジェクト。選択モード時のみ表示されます")]
    public GameObject warpButtonParentUI;

    // ==========================================
    // 合成の残り必要個数テキスト＆親オブジェクト設定
    // ==========================================
    [Header("【合成の必要個数テキスト＆親オブジェクト設定】")]
    [Tooltip("合成ボタンと同様に、選択ボタン（複数選択モード）を押した時に表示されるテキストの親オブジェクト")]
    public GameObject evolutionTextParentUI;

    [Tooltip("あと何個選択したら合成できるかの「数字」を表示するテキスト")]
    public TextMeshProUGUI evolutionRemainingCountText;

    [Tooltip("複数の種類のPrefabを選択している際や、何も選択していない時に非表示にする「もう1つのテキスト」")]
    public TextMeshProUGUI evolutionOtherText;

    // ==========================================
    // デバッグ設定
    // ==========================================
    [Header("【デバッグ設定】")]
    [Tooltip("チェックを入れている時、Pキーを押すとContentに入るPrefabをすべて一括削除します")]
    public bool enableDebugDeleteKey = false;

    private GameObject currentSelectionFrame;

    // 武器スロットごとに選択された武器（Transform / Prefab）とインデックスを記憶する配列
    private Transform[] selectedWeapons;
    private GameObject[] selectedSlotWeaponPrefabs;
    private int[] selectedWeaponItemIndices;

    // 防具カテゴリごとに選択された防具（Transform）とインデックスを記憶する配列
    private Transform[] selectedArmors;
    private int[] selectedArmorItemIndices;

    // レアリティ絞り込み時にも元のリストの何番目かを正確に記憶するための辞書
    private Dictionary<Transform, int> spawnedItemToIndexMap = new Dictionary<Transform, int>();
    private Dictionary<Transform, int> spawnedArmorMasterIndexMap = new Dictionary<Transform, int>();

    private bool isMultiSelectMode = false;
    private Dictionary<Transform, GameObject> multiSelectFrames = new Dictionary<Transform, GameObject>();

    private int currentSlotIndex = 0;
    private int currentCategoryIndex = -1;
    private bool isShowingArmor = false;

    private int currentStatSlotIndex = 0;
    private int currentRarityIndex = -1;

    // 進化演出中フラグ
    private bool isEvolutionAnimating = false;

    // セーブ・ロード用のキー
    private const string SAVE_KEY_WEAPONS_COORDS = "SavedInventory_Weapons_ExactCoords";
    private const string SAVE_KEY_ARMORS_COORDS = "SavedInventory_Armors_ExactCoords";
    private const string SAVE_KEY_EQUIPPED_WEAPONS = "SavedInventory_EquippedWeapons_Indices";
    private const string SAVE_KEY_EQUIPPED_ARMORS = "SavedInventory_EquippedArmors_Indices";

    void Start()
    {
        if (activeRarityColor.a == 0f) activeRarityColor = Color.white;
        if (inactiveRarityColor.a == 0f || inactiveRarityColor == activeRarityColor) inactiveRarityColor = Color.gray;

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            ArmorStats.BaseMaxHealth = playerHealth.maxHealth;
        }

        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            ArmorStats.BaseWalkSpeed = playerMovement.walkSpeed;
        }

        selectedWeapons = new Transform[slotButtons.Length];
        selectedSlotWeaponPrefabs = new GameObject[slotButtons.Length];
        selectedWeaponItemIndices = new int[slotButtons.Length];
        for (int i = 0; i < selectedWeaponItemIndices.Length; i++)
        {
            selectedWeaponItemIndices[i] = -1;
        }

        // 各種セットアップ
        SetupWeaponStatSlotButtons();
        SetupRarityButtons();
        SetupMultiSelectButtons();
        SetupEvolutionSystemButtons();

        if (flashPanelImage != null)
        {
            flashPanelImage.gameObject.SetActive(false);
        }

        int categoryCount = (categoryButtons != null) ? categoryButtons.Length : 0;
        selectedArmors = new Transform[categoryCount];
        selectedArmorItemIndices = new int[categoryCount];
        for (int i = 0; i < categoryCount; i++)
        {
            selectedArmorItemIndices[i] = -1;

            if (categoryButtons[i] != null && categoryButtons[i].targetArmorImage != null)
            {
                UpdateArmorImageAlpha(categoryButtons[i].targetArmorImage);
            }
        }

        if (selectionFramePrefab != null)
        {
            currentSelectionFrame = Instantiate(selectionFramePrefab);
            currentSelectionFrame.SetActive(false);
        }

        LoadInventoryData();

        CheckAndAddNewItemsFromKeeper();
        SortWeapons();

        // 装備情報の復元（セーブデータからロード）
        LoadEquippedSelections();

        currentRarityIndex = -1;
        UpdateRarityButtonColors();

        PopulateTestWeapons();
        OnSlotButtonClicked(0);

        UpdateStatusTexts();
        OnClickWeaponStatSlotButton(0);
        UpdateEvolutionButtonState();
    }

    void Update()
    {
        if (enableDebugDeleteKey && Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("【デバッグ】Pキーが押されたため、インベントリ内のPrefabをすべて削除します。");

            if (!isShowingArmor)
            {
                testWeaponPrefabs = new GameObject[0];
                for (int s = 0; s < selectedWeapons.Length; s++)
                {
                    selectedWeapons[s] = null;
                    selectedSlotWeaponPrefabs[s] = null;
                    selectedWeaponItemIndices[s] = -1;
                    if (WeaponDataKeeper.equippedWeaponPrefabs != null && s < WeaponDataKeeper.equippedWeaponPrefabs.Length)
                    {
                        WeaponDataKeeper.equippedWeaponPrefabs[s] = null;
                    }
                }
                PopulateTestWeapons();
            }
            else
            {
                armorPrefabs = new GameObject[0];
                for (int c = 0; c < selectedArmors.Length; c++)
                {
                    selectedArmors[c] = null;
                    selectedArmorItemIndices[c] = -1;
                    ArmorStats.EquipArmor(c, null);
                    if (categoryButtons != null && c < categoryButtons.Length && categoryButtons[c].targetArmorImage != null)
                    {
                        categoryButtons[c].targetArmorImage.sprite = null;
                        UpdateArmorImageAlpha(categoryButtons[c].targetArmorImage);
                    }
                }
                PopulateArmorContent();
            }

            SaveInventoryData();
            SaveEquippedSelections();
            UpdateStatusTexts();
            UpdateWeaponStatTexts();
            UpdateEvolutionButtonState();
        }
    }

    private void SaveInventoryData()
    {
        if (testWeaponPrefabs != null)
        {
            List<string> itemCodes = new List<string>();
            foreach (var w in testWeaponPrefabs)
            {
                if (w != null && TryGetWeaponCoordinates(w, out int groupIdx, out int rankIdx))
                {
                    itemCodes.Add($"{groupIdx}:{rankIdx}");
                }
            }
            PlayerPrefs.SetString(SAVE_KEY_WEAPONS_COORDS, string.Join(",", itemCodes));
        }

        if (armorPrefabs != null)
        {
            List<string> itemCodes = new List<string>();
            foreach (var a in armorPrefabs)
            {
                if (a != null && TryGetArmorCoordinates(a, out int categoryIdx, out int itemIdx))
                {
                    itemCodes.Add($"{categoryIdx}:{itemIdx}");
                }
            }
            PlayerPrefs.SetString(SAVE_KEY_ARMORS_COORDS, string.Join(",", itemCodes));
        }

        PlayerPrefs.Save();
    }

    private void LoadInventoryData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY_WEAPONS_COORDS))
        {
            string savedStr = PlayerPrefs.GetString(SAVE_KEY_WEAPONS_COORDS, "");
            if (!string.IsNullOrEmpty(savedStr))
            {
                string[] codes = savedStr.Split(',');
                List<GameObject> restoredList = new List<GameObject>();

                foreach (string code in codes)
                {
                    string[] parts = code.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int groupIdx) && int.TryParse(parts[1], out int rankIdx))
                    {
                        GameObject prefab = GetWeaponPrefabByCoordinates(groupIdx, rankIdx);
                        if (prefab != null)
                        {
                            restoredList.Add(prefab);
                        }
                    }
                }
                if (restoredList.Count > 0)
                {
                    testWeaponPrefabs = restoredList.ToArray();
                }
            }
        }

        if (PlayerPrefs.HasKey(SAVE_KEY_ARMORS_COORDS))
        {
            string savedStr = PlayerPrefs.GetString(SAVE_KEY_ARMORS_COORDS, "");
            if (!string.IsNullOrEmpty(savedStr))
            {
                string[] codes = savedStr.Split(',');
                List<GameObject> restoredList = new List<GameObject>();

                foreach (string code in codes)
                {
                    string[] parts = code.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int catIdx) && int.TryParse(parts[1], out int itmIdx))
                    {
                        GameObject prefab = GetArmorPrefabByCoordinates(catIdx, itmIdx);
                        if (prefab != null)
                        {
                            restoredList.Add(prefab);
                        }
                    }
                }
                if (restoredList.Count > 0)
                {
                    armorPrefabs = restoredList.ToArray();
                }
            }
        }
    }

    // ==========================================
    // 💡 追加：装備・選択状態（武器スロット・防具カテゴリ）のセーブとロード
    // ==========================================
    private void SaveEquippedSelections()
    {
        // 武器スロットのインデックスをカンマ区切りで保存（未選択は-1）
        if (selectedWeaponItemIndices != null)
        {
            List<string> indices = new List<string>();
            foreach (int idx in selectedWeaponItemIndices)
            {
                indices.Add(idx.ToString());
            }
            PlayerPrefs.SetString(SAVE_KEY_EQUIPPED_WEAPONS, string.Join(",", indices));
        }

        // 防具カテゴリのインデックスをカンマ区切りで保存（未選択は-1）
        if (selectedArmorItemIndices != null)
        {
            List<string> indices = new List<string>();
            foreach (int idx in selectedArmorItemIndices)
            {
                indices.Add(idx.ToString());
            }
            PlayerPrefs.SetString(SAVE_KEY_EQUIPPED_ARMORS, string.Join(",", indices));
        }

        PlayerPrefs.Save();
    }

    private void LoadEquippedSelections()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY_EQUIPPED_WEAPONS))
        {
            string savedStr = PlayerPrefs.GetString(SAVE_KEY_EQUIPPED_WEAPONS, "");
            if (!string.IsNullOrEmpty(savedStr))
            {
                string[] parts = savedStr.Split(',');
                for (int s = 0; s < parts.Length && s < selectedWeaponItemIndices.Length; s++)
                {
                    if (int.TryParse(parts[s], out int idx))
                    {
                        selectedWeaponItemIndices[s] = idx;
                        if (idx >= 0 && testWeaponPrefabs != null && idx < testWeaponPrefabs.Length)
                        {
                            selectedSlotWeaponPrefabs[s] = testWeaponPrefabs[idx];
                            if (WeaponDataKeeper.equippedWeaponPrefabs != null && s < WeaponDataKeeper.equippedWeaponPrefabs.Length)
                            {
                                WeaponDataKeeper.equippedWeaponPrefabs[s] = testWeaponPrefabs[idx];
                            }
                        }
                    }
                }
            }
        }

        if (PlayerPrefs.HasKey(SAVE_KEY_EQUIPPED_ARMORS))
        {
            string savedStr = PlayerPrefs.GetString(SAVE_KEY_EQUIPPED_ARMORS, "");
            if (!string.IsNullOrEmpty(savedStr))
            {
                string[] parts = savedStr.Split(',');
                for (int c = 0; c < parts.Length && c < selectedArmorItemIndices.Length; c++)
                {
                    if (int.TryParse(parts[c], out int idx))
                    {
                        selectedArmorItemIndices[c] = idx;
                        if (idx >= 0 && armorPrefabs != null && idx < armorPrefabs.Length)
                        {
                            GameObject armorPrefab = armorPrefabs[idx];
                            ArmorStats stats = armorPrefab != null ? armorPrefab.GetComponentInChildren<ArmorStats>(true) : null;
                            ArmorStats.EquipArmor(c, stats);

                            // スプライトの復元
                            if (categoryButtons != null && c < categoryButtons.Length && categoryButtons[c] != null)
                            {
                                ContentCategoryButton cat = categoryButtons[c];
                                if (cat.uiToHideOnSelect != null) cat.uiToHideOnSelect.SetActive(false);
                                if (cat.targetArmorImage != null && cat.displayTargetPrefabs != null)
                                {
                                    foreach (ArmorTargetElement elem in cat.displayTargetPrefabs)
                                    {
                                        if (elem != null && elem.targetPrefab == armorPrefab)
                                        {
                                            cat.targetArmorImage.sprite = elem.armorSprite;
                                            cat.targetArmorImage.enabled = true;
                                            UpdateArmorImageAlpha(cat.targetArmorImage);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private bool TryGetWeaponCoordinates(GameObject targetPrefab, out int groupIdx, out int rankIdx)
    {
        groupIdx = -1;
        rankIdx = -1;

        if (weaponRankLines == null || targetPrefab == null) return false;

        for (int g = 0; g < weaponRankLines.Length; g++)
        {
            var line = weaponRankLines[g];
            if (line != null && line.ranks != null)
            {
                for (int r = 0; r < line.ranks.Length; r++)
                {
                    if (line.ranks[r] == targetPrefab)
                    {
                        groupIdx = g;
                        rankIdx = r;
                        return true;
                    }
                }
            }
        }
        return false;
    }

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

    private bool TryGetArmorCoordinates(GameObject targetPrefab, out int categoryIdx, out int itemIdx)
    {
        categoryIdx = -1;
        itemIdx = -1;

        if (categoryButtons == null || targetPrefab == null) return false;

        for (int c = 0; c < categoryButtons.Length; c++)
        {
            var cat = categoryButtons[c];
            if (cat != null && cat.displayTargetPrefabs != null)
            {
                for (int d = 0; d < cat.displayTargetPrefabs.Length; d++)
                {
                    var elem = cat.displayTargetPrefabs[d];
                    if (elem != null && elem.targetPrefab == targetPrefab)
                    {
                        categoryIdx = c;
                        itemIdx = d;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private GameObject GetArmorPrefabByCoordinates(int categoryIdx, int itemIdx)
    {
        if (categoryButtons != null && categoryIdx >= 0 && categoryIdx < categoryButtons.Length)
        {
            var cat = categoryButtons[categoryIdx];
            if (cat != null && cat.displayTargetPrefabs != null && itemIdx >= 0 && itemIdx < cat.displayTargetPrefabs.Length)
            {
                var elem = cat.displayTargetPrefabs[itemIdx];
                if (elem != null) return elem.targetPrefab;
            }
        }
        return null;
    }

    private void SetupWeaponStatSlotButtons()
    {
        if (weaponSlotStatUIs == null) return;

        for (int i = 0; i < weaponSlotStatUIs.Length; i++)
        {
            if (weaponSlotStatUIs[i] != null && weaponSlotStatUIs[i].statSwitchButtonImage != null)
            {
                DisableButtonColorTransition(weaponSlotStatUIs[i].statSwitchButtonImage);

                Button statBtn = weaponSlotStatUIs[i].statSwitchButtonImage.GetComponent<Button>();
                if (statBtn == null) statBtn = weaponSlotStatUIs[i].statSwitchButtonImage.GetComponentInParent<Button>();

                if (statBtn != null)
                {
                    int capturedSlotIndex = i;
                    statBtn.onClick.AddListener(() => OnClickWeaponStatSlotButton(capturedSlotIndex));
                }
            }
        }
    }

    private void SetupMultiSelectButtons()
    {
        if (enterMultiSelectButton != null)
        {
            enterMultiSelectButton.onClick.RemoveListener(OnClickEnterMultiSelectMode);
            enterMultiSelectButton.onClick.AddListener(OnClickEnterMultiSelectMode);
        }

        if (selectAllButton != null)
        {
            selectAllButton.onClick.RemoveListener(OnClickSelectAllDisplayedPrefabs);
            selectAllButton.onClick.AddListener(OnClickSelectAllDisplayedPrefabs);
        }

        if (deleteSelectedButton != null)
        {
            deleteSelectedButton.onClick.RemoveListener(OnClickDeleteSelectedPrefabs);
            deleteSelectedButton.onClick.AddListener(OnClickDeleteSelectedPrefabs);
        }

        if (exitMultiSelectButton != null)
        {
            exitMultiSelectButton.onClick.RemoveListener(OnClickExitMultiSelectMode);
            exitMultiSelectButton.onClick.AddListener(OnClickExitMultiSelectMode);
        }

        isMultiSelectMode = false;
        UpdateMultiSelectModeButtonVisibility();
    }

    private void SetupEvolutionSystemButtons()
    {
        if (hideButton1 != null)
        {
            hideButton1.onClick.RemoveListener(OnClickHideButton1);
            hideButton1.onClick.AddListener(OnClickHideButton1);
        }

        if (warpAnimationButton != null)
        {
            DisableButtonColorTransition(warpAnimationButton.image);
            warpAnimationButton.onClick.RemoveListener(OnClickWarpAnimationButton);
            warpAnimationButton.onClick.AddListener(OnClickWarpAnimationButton);
        }

        if (executeEvolutionButton != null)
        {
            DisableButtonColorTransition(executeEvolutionButton.image);
            executeEvolutionButton.onClick.RemoveListener(OnClickWarpAnimationButton);
            executeEvolutionButton.onClick.AddListener(OnClickWarpAnimationButton);
        }
    }

    public void OnClickHideButton1()
    {
        if (conditionalVisibleUI != null)
        {
            conditionalVisibleUI.SetActive(false);
        }
    }

    public void OnClickWarpAnimationButton()
    {
        if (isEvolutionAnimating) return;
        if (!CheckCurrentSelectionForEvolution(out EvolutionRule rule, out List<Transform> selectedTransforms)) return;

        StartCoroutine(EvolutionAnimationRoutine(rule, selectedTransforms));
    }

    private void UpdateMultiSelectModeButtonVisibility()
    {
        if (selectAllButton != null) selectAllButton.gameObject.SetActive(isMultiSelectMode);
        if (deleteSelectedButton != null) deleteSelectedButton.gameObject.SetActive(isMultiSelectMode);
        if (exitMultiSelectButton != null) exitMultiSelectButton.gameObject.SetActive(isMultiSelectMode);

        if (selectAllParentUI != null) selectAllParentUI.SetActive(isMultiSelectMode);
        if (deleteSelectedParentUI != null) deleteSelectedParentUI.SetActive(isMultiSelectMode);
        if (exitMultiSelectParentUI != null) exitMultiSelectParentUI.SetActive(isMultiSelectMode);

        if (warpAnimationButton != null) warpAnimationButton.gameObject.SetActive(isMultiSelectMode);
        if (executeEvolutionButton != null) executeEvolutionButton.gameObject.SetActive(isMultiSelectMode);
        if (warpButtonParentUI != null) warpButtonParentUI.SetActive(isMultiSelectMode);

        if (evolutionTextParentUI != null) evolutionTextParentUI.SetActive(isMultiSelectMode);

        if (buttonsToHideInMultiSelect != null)
        {
            for (int i = 0; i < buttonsToHideInMultiSelect.Length; i++)
            {
                if (buttonsToHideInMultiSelect[i] != null)
                {
                    buttonsToHideInMultiSelect[i].SetActive(!isMultiSelectMode);
                }
            }
        }

        if (isMultiSelectMode)
        {
            UpdateEvolutionButtonState();
        }
        else
        {
            if (conditionalVisibleUI != null) conditionalVisibleUI.SetActive(false);
            UpdateEvolutionRemainingCountTexts();
        }
    }

    public void OnClickEnterMultiSelectMode()
    {
        isMultiSelectMode = true;

        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.transform.SetParent(this.transform, false);
            currentSelectionFrame.SetActive(false);
        }

        ClearAllMultiSelectFrames();
        UpdateMultiSelectModeButtonVisibility();
    }

    public void OnClickSelectAllDisplayedPrefabs()
    {
        if (!isMultiSelectMode)
        {
            OnClickEnterMultiSelectMode();
        }

        if (contentPanel == null || selectionFramePrefab == null) return;

        foreach (Transform child in contentPanel)
        {
            if (child == null) continue;

            if (!multiSelectFrames.ContainsKey(child) || multiSelectFrames[child] == null)
            {
                GameObject frame = Instantiate(selectionFramePrefab, child, false);
                frame.transform.localPosition = Vector3.zero;
                frame.transform.SetAsLastSibling();
                frame.SetActive(true);
                multiSelectFrames[child] = frame;
            }
        }
        UpdateEvolutionButtonState();
    }

    public void OnClickExitMultiSelectMode()
    {
        isMultiSelectMode = false;
        ClearAllMultiSelectFrames();
        UpdateMultiSelectModeButtonVisibility();

        if (!isShowingArmor)
        {
            UpdateWeaponSelectionFrame();
        }
        else
        {
            UpdateArmorSelectionFrame();
        }
        UpdateEvolutionButtonState();
    }

    private void ClearAllMultiSelectFrames()
    {
        foreach (var pair in multiSelectFrames)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value);
            }
        }
        multiSelectFrames.Clear();
    }

    private void UpdateEvolutionButtonState()
    {
        if (!isMultiSelectMode)
        {
            if (warpAnimationButton != null) warpAnimationButton.gameObject.SetActive(false);
            if (executeEvolutionButton != null) executeEvolutionButton.gameObject.SetActive(false);
            if (warpButtonParentUI != null) warpButtonParentUI.SetActive(false);
            if (evolutionTextParentUI != null) evolutionTextParentUI.SetActive(false);
            if (conditionalVisibleUI != null) conditionalVisibleUI.SetActive(false);
            UpdateEvolutionRemainingCountTexts();
            return;
        }

        if (warpAnimationButton != null) warpAnimationButton.gameObject.SetActive(true);
        if (executeEvolutionButton != null) executeEvolutionButton.gameObject.SetActive(true);
        if (warpButtonParentUI != null) warpButtonParentUI.SetActive(true);
        if (evolutionTextParentUI != null) evolutionTextParentUI.SetActive(true);

        UpdateEvolutionRemainingCountTexts();

        bool canEvolve = CheckCurrentSelectionForEvolution(out EvolutionRule matchedRule, out List<Transform> selectedTransforms);

        Color targetColor = (canEvolve && !isEvolutionAnimating) ? readyEvolutionButtonColor : normalEvolutionButtonColor;

        if (warpAnimationButton != null)
        {
            warpAnimationButton.interactable = canEvolve && !isEvolutionAnimating;
            if (warpAnimationButton.image != null)
            {
                Color currentC = warpAnimationButton.image.color;
                warpAnimationButton.image.color = new Color(targetColor.r, targetColor.g, targetColor.b, currentC.a);
            }
        }

        if (executeEvolutionButton != null)
        {
            executeEvolutionButton.interactable = canEvolve && !isEvolutionAnimating;
            if (executeEvolutionButton.image != null)
            {
                Color currentC = executeEvolutionButton.image.color;
                executeEvolutionButton.image.color = new Color(targetColor.r, targetColor.g, targetColor.b, currentC.a);
            }
        }

        if (executeEvolutionButtonImage != null)
        {
            Color currentC = executeEvolutionButtonImage.color;
            executeEvolutionButtonImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, currentC.a);
        }
    }

    private void UpdateEvolutionRemainingCountTexts()
    {
        if (!isMultiSelectMode || multiSelectFrames == null || multiSelectFrames.Count == 0)
        {
            if (evolutionRemainingCountText != null)
            {
                evolutionRemainingCountText.text = "";
                evolutionRemainingCountText.color = Color.black;
                evolutionRemainingCountText.gameObject.SetActive(false);
            }
            if (evolutionOtherText != null)
            {
                evolutionOtherText.gameObject.SetActive(false);
            }
            return;
        }

        Dictionary<GameObject, int> prefabCounts = new Dictionary<GameObject, int>();

        foreach (var pair in multiSelectFrames)
        {
            Transform t = pair.Key;
            if (t == null) continue;

            GameObject prefab = GetPrefabFromSpawnedTransform(t);
            if (prefab == null) continue;

            if (!prefabCounts.ContainsKey(prefab))
            {
                prefabCounts[prefab] = 0;
            }
            prefabCounts[prefab]++;
        }

        if (prefabCounts.Count != 1)
        {
            if (evolutionRemainingCountText != null)
            {
                evolutionRemainingCountText.text = "";
                evolutionRemainingCountText.color = Color.black;
                evolutionRemainingCountText.gameObject.SetActive(false);
            }
            if (evolutionOtherText != null)
            {
                evolutionOtherText.gameObject.SetActive(false);
            }
            return;
        }

        GameObject selectedPrefab = null;
        int selectedCount = 0;
        foreach (var kvp in prefabCounts)
        {
            selectedPrefab = kvp.Key;
            selectedCount = kvp.Value;
            break;
        }

        EvolutionRule targetRule = null;
        if (evolutionRules != null && selectedPrefab != null)
        {
            foreach (EvolutionRule rule in evolutionRules)
            {
                if (rule != null && rule.sourcePrefab == selectedPrefab)
                {
                    targetRule = rule;
                    break;
                }
            }
        }

        if (targetRule != null)
        {
            int diffCount = targetRule.requiredCount - selectedCount;

            if (evolutionRemainingCountText != null)
            {
                evolutionRemainingCountText.text = diffCount.ToString();

                if (diffCount < 0)
                {
                    evolutionRemainingCountText.color = Color.red;
                }
                else
                {
                    evolutionRemainingCountText.color = Color.black;
                }

                evolutionRemainingCountText.gameObject.SetActive(true);
            }
            if (evolutionOtherText != null)
            {
                evolutionOtherText.gameObject.SetActive(true);
            }
        }
        else
        {
            if (evolutionRemainingCountText != null)
            {
                evolutionRemainingCountText.text = "";
                evolutionRemainingCountText.color = Color.black;
                evolutionRemainingCountText.gameObject.SetActive(false);
            }
            if (evolutionOtherText != null)
            {
                evolutionOtherText.gameObject.SetActive(false);
            }
        }
    }

    private bool CheckCurrentSelectionForEvolution(out EvolutionRule matchedRule, out List<Transform> selectedTransforms)
    {
        matchedRule = null;
        selectedTransforms = new List<Transform>();

        if (multiSelectFrames == null || multiSelectFrames.Count == 0) return false;

        Dictionary<GameObject, List<Transform>> prefabToTransformsMap = new Dictionary<GameObject, List<Transform>>();

        foreach (var pair in multiSelectFrames)
        {
            Transform t = pair.Key;
            if (t == null) continue;

            GameObject prefab = GetPrefabFromSpawnedTransform(t);
            if (prefab == null) return false;

            if (!prefabToTransformsMap.ContainsKey(prefab))
            {
                prefabToTransformsMap[prefab] = new List<Transform>();
            }
            prefabToTransformsMap[prefab].Add(t);
        }

        if (prefabToTransformsMap.Count != 1) return false;

        foreach (var kvp in prefabToTransformsMap)
        {
            GameObject selectedPrefab = kvp.Key;
            List<Transform> transforms = kvp.Value;

            if (evolutionRules != null)
            {
                foreach (EvolutionRule rule in evolutionRules)
                {
                    if (rule != null && rule.sourcePrefab == selectedPrefab && transforms.Count == rule.requiredCount)
                    {
                        matchedRule = rule;
                        selectedTransforms = transforms;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private GameObject GetPrefabFromSpawnedTransform(Transform spawnedTransform)
    {
        if (!isShowingArmor)
        {
            if (spawnedItemToIndexMap.TryGetValue(spawnedTransform, out int idx))
            {
                if (testWeaponPrefabs != null && idx >= 0 && idx < testWeaponPrefabs.Length)
                {
                    return testWeaponPrefabs[idx];
                }
            }
        }
        else
        {
            if (spawnedItemToIndexMap.TryGetValue(spawnedTransform, out int idx))
            {
                if (armorPrefabs != null && idx >= 0 && idx < armorPrefabs.Length)
                {
                    return armorPrefabs[idx];
                }
            }
        }
        return null;
    }

    private IEnumerator EvolutionAnimationRoutine(EvolutionRule rule, List<Transform> selectedTransforms)
    {
        isEvolutionAnimating = true;
        UpdateEvolutionButtonState();

        if (conditionalVisibleUI != null) conditionalVisibleUI.SetActive(false);
        if (hideButton1 != null) hideButton1.gameObject.SetActive(false);

        float moveDuration = Mathf.Max(0.05f, moveToPositionDuration);
        float moveElapsed = 0f;

        Vector3[] startPosForMove = new Vector3[selectedTransforms.Count];
        Vector3[] targetPosForMove = new Vector3[selectedTransforms.Count];

        for (int i = 0; i < selectedTransforms.Count; i++)
        {
            if (selectedTransforms[i] != null)
            {
                startPosForMove[i] = selectedTransforms[i].position;

                if (rule.evolutionSpawnPositions != null && rule.evolutionSpawnPositions.Length > 0)
                {
                    int posIdx = Mathf.Min(i, rule.evolutionSpawnPositions.Length - 1);
                    if (rule.evolutionSpawnPositions[posIdx] != null)
                    {
                        targetPosForMove[i] = rule.evolutionSpawnPositions[posIdx].position;
                    }
                    else
                    {
                        targetPosForMove[i] = startPosForMove[i];
                    }
                }
                else
                {
                    targetPosForMove[i] = startPosForMove[i];
                }
            }
        }

        while (moveElapsed < moveDuration)
        {
            moveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(moveElapsed / moveDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < selectedTransforms.Count; i++)
            {
                if (selectedTransforms[i] != null)
                {
                    selectedTransforms[i].position = Vector3.Lerp(startPosForMove[i], targetPosForMove[i], smoothT);
                    selectedTransforms[i].localScale = Vector3.one;
                }
            }
            yield return null;
        }

        Vector3 centerPos = Vector3.zero;
        if (selectedTransforms.Count > 0)
        {
            foreach (Transform t in selectedTransforms)
            {
                if (t != null) centerPos += t.position;
            }
            centerPos /= selectedTransforms.Count;
        }

        float animDuration = 1.0f;
        float elapsed = 0f;

        Vector3[] startPositions = new Vector3[selectedTransforms.Count];
        for (int i = 0; i < selectedTransforms.Count; i++)
        {
            if (selectedTransforms[i] != null)
            {
                startPositions[i] = selectedTransforms[i].position;
            }
        }

        bool dataProcessed = false;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            int count = selectedTransforms.Count;
            for (int i = 0; i < count; i++)
            {
                Transform tr = selectedTransforms[i];
                if (tr == null) continue;

                float angle = (i * (360f / count)) + (smoothT * 720f);
                float radius = Mathf.Lerp(50f, 0f, smoothT);
                float rad = angle * Mathf.Deg2Rad;

                Vector3 offset = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
                Vector3 targetPos = centerPos + offset;

                tr.position = Vector3.Lerp(startPositions[i], targetPos, smoothT);
                tr.Rotate(Vector3.forward, 360f * Time.deltaTime * 2f);
                tr.localScale = Vector3.one;
            }

            if (flashPanelImage != null && !flashPanelImage.gameObject.activeSelf)
            {
                flashPanelImage.gameObject.SetActive(true);
            }

            if (flashPanelImage != null)
            {
                float flashProgress = Mathf.Clamp01(elapsed / (animDuration * 0.8f));
                Color c = flashPanelImage.color;
                c.a = flashProgress;
                flashPanelImage.color = c;

                if (!dataProcessed && flashProgress >= 0.99f)
                {
                    ExecuteEvolutionDataProcess(rule);
                    dataProcessed = true;
                }
            }

            yield return null;
        }

        if (!dataProcessed)
        {
            ExecuteEvolutionDataProcess(rule);
        }

        foreach (Transform tr in selectedTransforms)
        {
            if (tr != null)
            {
                Destroy(tr.gameObject);
            }
        }
        selectedTransforms.Clear();

        yield return new WaitForSeconds(0.5f);

        if (flashPanelImage != null)
        {
            float fadeElapsed = 0f;
            float fadeDuration = 0.5f;

            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (fadeElapsed / fadeDuration));
                Color c = flashPanelImage.color;
                c.a = alpha;
                flashPanelImage.color = c;
                yield return null;
            }
            flashPanelImage.gameObject.SetActive(false);
        }

        isEvolutionAnimating = false;

        OnClickExitMultiSelectMode();
        if (!isShowingArmor)
        {
            PopulateTestWeapons();
        }
        else
        {
            PopulateArmorContent();
        }
    }

    private void ExecuteEvolutionDataProcess(EvolutionRule rule)
    {
        GameObject nextRankPrefab = null;

        if (weaponRankLines != null)
        {
            foreach (WeaponRankLine line in weaponRankLines)
            {
                if (line != null && line.ranks != null)
                {
                    for (int i = 0; i < line.ranks.Length; i++)
                    {
                        if (line.ranks[i] == rule.sourcePrefab)
                        {
                            if (i + 1 < line.ranks.Length)
                            {
                                nextRankPrefab = line.ranks[i + 1];
                            }
                            break;
                        }
                    }
                }
                if (nextRankPrefab != null) break;
            }
        }

        if (nextRankPrefab == null)
        {
            Debug.LogWarning("進化先のPrefabが weaponRankLines の配列内で見つかりませんでした！");
            return;
        }

        if (!isShowingArmor)
        {
            if (testWeaponPrefabs == null || rule.sourcePrefab == null) return;

            List<GameObject> weaponList = new List<GameObject>(testWeaponPrefabs);
            int removedCount = 0;

            for (int i = weaponList.Count - 1; i >= 0; i--)
            {
                if (weaponList[i] == rule.sourcePrefab && removedCount < rule.requiredCount)
                {
                    for (int s = 0; s < selectedWeaponItemIndices.Length; s++)
                    {
                        if (selectedWeaponItemIndices[s] == i)
                        {
                            selectedWeapons[s] = null;
                            selectedSlotWeaponPrefabs[s] = null;
                            selectedWeaponItemIndices[s] = -1;
                            if (WeaponDataKeeper.equippedWeaponPrefabs != null && s < WeaponDataKeeper.equippedWeaponPrefabs.Length)
                            {
                                WeaponDataKeeper.equippedWeaponPrefabs[s] = null;
                            }
                        }
                    }

                    weaponList.RemoveAt(i);
                    removedCount++;
                }
            }

            weaponList.Add(nextRankPrefab);
            testWeaponPrefabs = weaponList.ToArray();

            SortWeapons();
            ClearAllMultiSelectFrames();
            UpdateWeaponStatTexts();
        }
        else
        {
            if (armorPrefabs == null || rule.sourcePrefab == null) return;

            List<GameObject> armorList = new List<GameObject>(armorPrefabs);
            int removedCount = 0;

            for (int i = armorList.Count - 1; i >= 0; i--)
            {
                if (armorList[i] == rule.sourcePrefab && removedCount < rule.requiredCount)
                {
                    armorList.RemoveAt(i);
                    removedCount++;
                }
            }

            armorList.Add(nextRankPrefab);
            armorPrefabs = armorList.ToArray();

            ClearAllMultiSelectFrames();
            UpdateStatusTexts();
            UpdateWeaponStatTexts();
        }

        SaveInventoryData();
        SaveEquippedSelections();
    }

    public void OnClickDeleteSelectedPrefabs()
    {
        if (!isMultiSelectMode) return;
        if (multiSelectFrames.Count == 0) return;

        if (!isShowingArmor)
        {
            if (testWeaponPrefabs == null) return;

            HashSet<int> indicesToDelete = new HashSet<int>();
            foreach (var pair in multiSelectFrames)
            {
                Transform selectedTransform = pair.Key;
                if (selectedTransform != null && spawnedItemToIndexMap.TryGetValue(selectedTransform, out int originalIdx))
                {
                    indicesToDelete.Add(originalIdx);
                }
            }

            if (indicesToDelete.Count == 0) return;

            List<GameObject> newWeaponList = new List<GameObject>();
            int[] newSelectedSlotIndices = new int[selectedWeaponItemIndices.Length];
            for (int s = 0; s < newSelectedSlotIndices.Length; s++)
            {
                newSelectedSlotIndices[s] = -1;
            }

            for (int oldIdx = 0; oldIdx < testWeaponPrefabs.Length; oldIdx++)
            {
                if (indicesToDelete.Contains(oldIdx))
                {
                    for (int s = 0; s < selectedWeaponItemIndices.Length; s++)
                    {
                        if (selectedWeaponItemIndices[s] == oldIdx)
                        {
                            selectedWeapons[s] = null;
                            selectedSlotWeaponPrefabs[s] = null;
                            selectedWeaponItemIndices[s] = -1;
                            if (WeaponDataKeeper.equippedWeaponPrefabs != null && s < WeaponDataKeeper.equippedWeaponPrefabs.Length)
                            {
                                WeaponDataKeeper.equippedWeaponPrefabs[s] = null;
                            }
                        }
                    }
                }
                else
                {
                    int newIdx = newWeaponList.Count;
                    newWeaponList.Add(testWeaponPrefabs[oldIdx]);

                    for (int s = 0; s < selectedWeaponItemIndices.Length; s++)
                    {
                        if (selectedWeaponItemIndices[s] == oldIdx)
                        {
                            newSelectedSlotIndices[s] = newIdx;
                        }
                    }
                }
            }

            testWeaponPrefabs = newWeaponList.ToArray();
            selectedWeaponItemIndices = newSelectedSlotIndices;

            ClearAllMultiSelectFrames();
            PopulateTestWeapons();
            UpdateWeaponStatTexts();
        }
        else
        {
            if (armorPrefabs == null) return;

            HashSet<int> masterIndicesToDelete = new HashSet<int>();
            HashSet<int> categoryLocalIndicesToDelete = new HashSet<int>();

            foreach (var pair in multiSelectFrames)
            {
                Transform selectedTransform = pair.Key;
                if (selectedTransform != null)
                {
                    if (spawnedArmorMasterIndexMap.TryGetValue(selectedTransform, out int masterIdx))
                    {
                        masterIndicesToDelete.Add(masterIdx);
                    }
                    if (spawnedItemToIndexMap.TryGetValue(selectedTransform, out int localIdx))
                    {
                        categoryLocalIndicesToDelete.Add(localIdx);
                    }
                }
            }

            if (masterIndicesToDelete.Count == 0) return;

            if (currentCategoryIndex >= 0 && currentCategoryIndex < selectedArmorItemIndices.Length)
            {
                int currentEquippedLocalIdx = selectedArmorItemIndices[currentCategoryIndex];
                if (currentEquippedLocalIdx != -1)
                {
                    if (categoryLocalIndicesToDelete.Contains(currentEquippedLocalIdx))
                    {
                        selectedArmorItemIndices[currentCategoryIndex] = -1;
                        selectedArmors[currentCategoryIndex] = null;
                        ArmorStats.EquipArmor(currentCategoryIndex, null);

                        ContentCategoryButton currentCategory = categoryButtons[currentCategoryIndex];
                        if (currentCategory != null)
                        {
                            if (currentCategory.targetArmorImage != null)
                            {
                                currentCategory.targetArmorImage.sprite = null;
                                UpdateArmorImageAlpha(currentCategory.targetArmorImage);
                            }
                            if (currentCategory.uiToHideOnSelect != null)
                            {
                                currentCategory.uiToHideOnSelect.SetActive(true);
                            }
                        }
                    }
                    else
                    {
                        int shiftCount = 0;
                        foreach (int delLocalIdx in categoryLocalIndicesToDelete)
                        {
                            if (delLocalIdx < currentEquippedLocalIdx)
                            {
                                shiftCount++;
                            }
                        }
                        selectedArmorItemIndices[currentCategoryIndex] -= shiftCount;
                    }
                }
            }

            List<GameObject> newArmorList = new List<GameObject>();
            for (int i = 0; i < armorPrefabs.Length; i++)
            {
                if (!masterIndicesToDelete.Contains(i))
                {
                    newArmorList.Add(armorPrefabs[i]);
                }
            }
            armorPrefabs = newArmorList.ToArray();

            ClearAllMultiSelectFrames();
            PopulateArmorContent();
            UpdateStatusTexts();
            UpdateWeaponStatTexts();
        }

        SaveInventoryData();
        SaveEquippedSelections();
        UpdateEvolutionButtonState();
    }

    private void SetupRarityButtons()
    {
        if (showAllRarityButtonImage != null)
        {
            DisableButtonColorTransition(showAllRarityButtonImage);
            Button allBtn = showAllRarityButtonImage.GetComponent<Button>();
            if (allBtn == null) allBtn = showAllRarityButtonImage.GetComponentInParent<Button>();
            if (allBtn != null)
            {
                allBtn.onClick.RemoveListener(OnClickShowAllRarityButton);
                allBtn.onClick.AddListener(OnClickShowAllRarityButton);
            }
        }

        if (rarityButtons != null)
        {
            for (int i = 0; i < rarityButtons.Length; i++)
            {
                if (rarityButtons[i] != null && rarityButtons[i].buttonImage != null)
                {
                    DisableButtonColorTransition(rarityButtons[i].buttonImage);
                    Button rBtn = rarityButtons[i].buttonImage.GetComponent<Button>();
                    if (rBtn == null) rBtn = rarityButtons[i].buttonImage.GetComponentInParent<Button>();
                    if (rBtn != null)
                    {
                        int capturedIndex = i;
                        rBtn.onClick.AddListener(() => OnClickRarityButton(capturedIndex));
                    }
                }
            }
        }
    }

    public void OnClickShowAllRarityButton()
    {
        OnClickRarityButton(-1);
    }

    public void OnClickRarityButton(int rarityIndex)
    {
        currentRarityIndex = rarityIndex;
        UpdateRarityButtonColors();

        if (!isShowingArmor)
        {
            PopulateTestWeapons();
            if (!isMultiSelectMode) UpdateWeaponSelectionFrame();
        }
        else
        {
            PopulateArmorContent();
            if (!isMultiSelectMode) UpdateArmorSelectionFrame();
        }
    }

    private void UpdateRarityButtonColors()
    {
        bool isAllSelected = (currentRarityIndex == -1);
        ApplyRarityUIColor(showAllRarityButtonImage, isAllSelected ? activeRarityColor : inactiveRarityColor);
        ApplyRarityUIColor(showAllRarityTargetUIImage, isAllSelected ? activeRarityColor : inactiveRarityColor);

        if (rarityButtons != null)
        {
            for (int i = 0; i < rarityButtons.Length; i++)
            {
                if (rarityButtons[i] != null)
                {
                    bool isThisRaritySelected = (currentRarityIndex == i);
                    Color targetColor = isThisRaritySelected ? activeRarityColor : inactiveRarityColor;

                    ApplyRarityUIColor(rarityButtons[i].buttonImage, targetColor);
                    ApplyRarityUIColor(rarityButtons[i].targetUIImage, targetColor);
                }
            }
        }
    }

    private void ApplyRarityUIColor(Image rootImage, Color targetColor)
    {
        if (rootImage == null) return;

        rootImage.color = targetColor;

        Image[] childImages = rootImage.GetComponentsInChildren<Image>(true);
        foreach (Image img in childImages)
        {
            if (img != null)
            {
                img.color = targetColor;
            }
        }
    }

    private bool IsPrefabMatchCurrentRarity(GameObject prefab)
    {
        if (prefab == null) return false;

        if (currentRarityIndex < 0) return true;

        if (rarityButtons == null || currentRarityIndex >= rarityButtons.Length) return true;
        if (rarityButtons[currentRarityIndex] == null) return false;

        GameObject[] targetList = rarityButtons[currentRarityIndex].rarityTargetPrefabs;
        if (targetList == null || targetList.Length == 0) return false;

        foreach (GameObject target in targetList)
        {
            if (target == prefab)
            {
                return true;
            }
        }

        return false;
    }

    private void DisableButtonColorTransition(Image targetImage)
    {
        if (targetImage == null) return;

        Button[] buttons = targetImage.GetComponentsInChildren<Button>(true);
        foreach (Button b in buttons)
        {
            b.transition = Selectable.Transition.None;
            Navigation nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
        }

        Button parentBtn = targetImage.GetComponentInParent<Button>();
        if (parentBtn != null)
        {
            parentBtn.transition = Selectable.Transition.None;
            Navigation nav = parentBtn.navigation;
            nav.mode = Navigation.Mode.None;
            parentBtn.navigation = nav;
        }
    }

    public void UpdateStatusTexts()
    {
        if (healthValueText != null)
        {
            healthValueText.text = ArmorStats.CurrentTotalHealth.ToString();
        }

        if (defenseValueText != null)
        {
            defenseValueText.text = ArmorStats.CurrentTotalDefense.ToString();
        }

        if (speedValueText != null)
        {
            speedValueText.text = ArmorStats.CurrentTotalSpeed.ToString();
        }
    }

    public void OnClickWeaponStatSlotButton(int statSlotIndex)
    {
        currentStatSlotIndex = statSlotIndex;
        UpdateWeaponStatTexts();
    }

    public void UpdateWeaponStatTexts()
    {
        if (weaponSlotStatUIs == null || weaponSlotStatUIs.Length == 0) return;

        for (int i = 0; i < weaponSlotStatUIs.Length; i++)
        {
            WeaponSlotStatUI slotUI = weaponSlotStatUIs[i];
            if (slotUI == null) continue;

            if (slotUI.statSwitchButtonImage != null)
            {
                slotUI.statSwitchButtonImage.color = (i == currentStatSlotIndex) ? activeStatButtonColor : inactiveStatButtonColor;
            }

            if (slotUI.attackValueText != null)
            {
                slotUI.attackValueText.gameObject.SetActive(false);
            }
            if (slotUI.bulletSpeedValueText != null)
            {
                slotUI.bulletSpeedValueText.gameObject.SetActive(false);
            }
            if (slotUI.spreadValueText != null)
            {
                slotUI.spreadValueText.gameObject.SetActive(false);
            }
        }

        if (currentStatSlotIndex >= 0 && currentStatSlotIndex < weaponSlotStatUIs.Length)
        {
            WeaponSlotStatUI activeSlotUI = weaponSlotStatUIs[currentStatSlotIndex];
            if (activeSlotUI != null)
            {
                GameObject activeWeaponPrefab = GetEquippedWeaponPrefabForSlot(currentStatSlotIndex);

                if (activeWeaponPrefab != null)
                {
                    GetWeaponStatsFromPrefab(activeWeaponPrefab, out int activeAtk, out float activeSpd, out float activeSpr);

                    if (activeSlotUI.attackValueText != null)
                    {
                        activeSlotUI.attackValueText.text = activeAtk.ToString();
                        activeSlotUI.attackValueText.gameObject.SetActive(true);
                    }
                    if (activeSlotUI.bulletSpeedValueText != null)
                    {
                        activeSlotUI.bulletSpeedValueText.text = activeSpd.ToString();
                        activeSlotUI.bulletSpeedValueText.gameObject.SetActive(true);
                    }
                    if (activeSlotUI.spreadValueText != null)
                    {
                        activeSlotUI.spreadValueText.text = activeSpr.ToString();
                        activeSlotUI.spreadValueText.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    private GameObject GetEquippedWeaponPrefabForSlot(int slotIndex)
    {
        if (selectedSlotWeaponPrefabs != null && slotIndex >= 0 && slotIndex < selectedSlotWeaponPrefabs.Length)
        {
            return selectedSlotWeaponPrefabs[slotIndex];
        }

        return null;
    }

    private void GetWeaponStatsFromPrefab(GameObject itemPrefab, out int totalAttack, out float bulletSpeed, out float spreadAmount)
    {
        totalAttack = 0;
        bulletSpeed = 0f;
        spreadAmount = 0f;

        if (itemPrefab == null) return;

        GameObject targetGunObj = itemPrefab;
        WeaponModelLinker linker = itemPrefab.GetComponentInChildren<WeaponModelLinker>(true);
        if (linker != null && linker.actualWeaponPrefab != null)
        {
            targetGunObj = linker.actualWeaponPrefab;
        }

        GameObject bulletPrefab = null;
        bool isGunFound = false;

        AK47 ak = targetGunObj.GetComponentInChildren<AK47>(true);
        if (ak != null)
        {
            bulletPrefab = ak.bulletPrefab;
            bulletSpeed = ak.bulletSpeed;
            spreadAmount = ak.spreadAmount;
            isGunFound = true;
        }

        if (!isGunFound)
        {
            Shotgun shotgun = targetGunObj.GetComponentInChildren<Shotgun>(true);
            if (shotgun != null)
            {
                bulletPrefab = shotgun.bulletPrefab;
                bulletSpeed = shotgun.bulletSpeed;
                spreadAmount = shotgun.spreadAmount;
                isGunFound = true;
            }
        }

        if (!isGunFound)
        {
            LMG lmg = targetGunObj.GetComponentInChildren<LMG>(true);
            if (lmg != null)
            {
                bulletPrefab = lmg.bulletPrefab;
                bulletSpeed = lmg.bulletSpeed;
                spreadAmount = lmg.spreadAmount;
                isGunFound = true;
            }
        }

        if (!isGunFound)
        {
            M4 m4 = targetGunObj.GetComponentInChildren<M4>(true);
            if (m4 != null)
            {
                bulletPrefab = m4.bulletPrefab;
                bulletSpeed = m4.bulletSpeed;
                spreadAmount = m4.spreadAmount;
                isGunFound = true;
            }
        }

        if (!isGunFound)
        {
            M107 m107 = targetGunObj.GetComponentInChildren<M107>(true);
            if (m107 != null)
            {
                bulletPrefab = m107.bulletPrefab;
                bulletSpeed = m107.bulletSpeed;
                spreadAmount = m107.spreadAmount;
                isGunFound = true;
            }
        }

        if (!isGunFound)
        {
            M249 m249 = targetGunObj.GetComponentInChildren<M249>(true);
            if (m249 != null)
            {
                bulletPrefab = m249.bulletPrefab;
                bulletSpeed = m249.bulletSpeed;
                spreadAmount = m249.spreadAmount;
                isGunFound = true;
            }
        }

        if (!isGunFound)
        {
            Pistol pistol = targetGunObj.GetComponentInChildren<Pistol>(true);
            if (pistol != null)
            {
                bulletPrefab = pistol.bulletPrefab;
                bulletSpeed = pistol.bulletSpeed;
                spreadAmount = pistol.spreadAmount;
                isGunFound = true;
            }
        }

        if (!isGunFound)
        {
            RPG7 rpg7 = targetGunObj.GetComponentInChildren<RPG7>(true);
            if (rpg7 != null)
            {
                bulletPrefab = rpg7.bulletPrefab;
                bulletSpeed = rpg7.bulletSpeed;
                spreadAmount = rpg7.spreadAmount;
                isGunFound = true;
            }
        }

        int baseDamage = 0;
        if (bulletPrefab != null)
        {
            Bullet normalBullet = bulletPrefab.GetComponentInChildren<Bullet>(true);
            if (normalBullet != null)
            {
                baseDamage = normalBullet.damage;
            }
            else
            {
                RocketBullet rocketBullet = bulletPrefab.GetComponentInChildren<RocketBullet>(true);
                if (rocketBullet != null)
                {
                    baseDamage = rocketBullet.explosionDamage;
                }
            }
        }

        if (isGunFound)
        {
            totalAttack = baseDamage + ArmorStats.TotalAttackBonus;
        }
    }

    private void UpdateArmorImageAlpha(Image targetImg)
    {
        if (targetImg == null) return;

        Color c = targetImg.color;
        c.a = (targetImg.sprite != null) ? 1f : 0f;
        targetImg.color = c;
    }

    private void CheckAndAddNewItemsFromKeeper()
    {
        bool isUpdated = false;

        if (WeaponDataKeeper.gachaResultWeapons.Count > 0)
        {
            List<GameObject> weaponList = new List<GameObject>(testWeaponPrefabs != null ? testWeaponPrefabs : new GameObject[0]);

            foreach (GameObject newWeapon in WeaponDataKeeper.gachaResultWeapons)
            {
                if (newWeapon != null)
                {
                    weaponList.Add(newWeapon);
                }
            }

            testWeaponPrefabs = weaponList.ToArray();
            WeaponDataKeeper.gachaResultWeapons.Clear();
            isUpdated = true;
            Debug.Log("ガチャ結果の新しい武器をインベントリに追加しました！");
        }

        if (CapsuleOpener.gachaResultArmors.Count > 0)
        {
            List<GameObject> armorList = new List<GameObject>(armorPrefabs != null ? armorPrefabs : new GameObject[0]);

            foreach (GameObject newArmor in CapsuleOpener.gachaResultArmors)
            {
                if (newArmor != null)
                {
                    armorList.Add(newArmor);
                }
            }

            armorPrefabs = armorList.ToArray();
            CapsuleOpener.gachaResultArmors.Clear();
            isUpdated = true;
            Debug.Log("ガチャ結果の新しい防具をインベントリに追加しました！");
        }

        if (isUpdated)
        {
            SaveInventoryData();
        }
    }

    private void SortWeapons()
    {
        if (weaponSortOrder == null || weaponSortOrder.Length == 0) return;
        if (testWeaponPrefabs == null || testWeaponPrefabs.Length == 0) return;

        List<GameObject> sortedList = new List<GameObject>();
        List<GameObject> currentWeapons = new List<GameObject>(testWeaponPrefabs);

        foreach (GameObject sortTarget in weaponSortOrder)
        {
            if (sortTarget == null) continue;

            for (int i = currentWeapons.Count - 1; i >= 0; i--)
            {
                if (currentWeapons[i] == sortTarget)
                {
                    sortedList.Add(currentWeapons[i]);
                    currentWeapons.RemoveAt(i);
                }
            }
        }

        sortedList.AddRange(currentWeapons);
        testWeaponPrefabs = sortedList.ToArray();
    }

    public void OnSlotButtonClicked(int slotIndex)
    {
        if (isShowingArmor)
        {
            isShowingArmor = false;
            currentCategoryIndex = -1;

            if (categoryButtons != null)
            {
                for (int i = 0; i < categoryButtons.Length; i++)
                {
                    if (categoryButtons[i].buttonImage != null)
                    {
                        categoryButtons[i].buttonImage.color = inactiveCategoryColor;
                    }
                }
            }

            PopulateTestWeapons();
        }

        currentSlotIndex = slotIndex;
        currentStatSlotIndex = slotIndex;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] != null)
            {
                slotButtons[i].color = (i == currentSlotIndex) ? activeColor : inactiveColor;
            }
        }

        if (slotTargetImages != null)
        {
            for (int i = 0; i < slotTargetImages.Length; i++)
            {
                if (slotTargetImages[i] != null)
                {
                    slotTargetImages[i].color = (i == currentSlotIndex) ? activeColor : inactiveColor;
                }
            }
        }

        if (!isMultiSelectMode)
        {
            UpdateWeaponSelectionFrame();
        }

        UpdateWeaponStatTexts();
    }

    private void UpdateWeaponSelectionFrame()
    {
        if (currentSelectionFrame == null) return;
        if (isMultiSelectMode)
        {
            currentSelectionFrame.SetActive(false);
            return;
        }

        Transform savedWeapon = (selectedWeapons != null && currentSlotIndex >= 0 && currentSlotIndex < selectedWeapons.Length)
            ? selectedWeapons[currentSlotIndex]
            : null;

        if (savedWeapon != null)
        {
            currentSelectionFrame.SetActive(true);
            currentSelectionFrame.transform.SetParent(savedWeapon, false);
            currentSelectionFrame.transform.localPosition = Vector3.zero;
            currentSelectionFrame.transform.SetAsLastSibling();
        }
        else
        {
            currentSelectionFrame.SetActive(false);
        }
    }

    private void UpdateArmorSelectionFrame()
    {
        if (currentSelectionFrame == null) return;
        if (isMultiSelectMode)
        {
            currentSelectionFrame.SetActive(false);
            return;
        }

        if (selectedArmors != null && currentCategoryIndex >= 0 && currentCategoryIndex < selectedArmors.Length)
        {
            Transform savedArmor = selectedArmors[currentCategoryIndex];
            if (savedArmor != null)
            {
                currentSelectionFrame.SetActive(true);
                currentSelectionFrame.transform.SetParent(savedArmor, false);
                currentSelectionFrame.transform.localPosition = Vector3.zero;
                currentSelectionFrame.transform.SetAsLastSibling();
            }
            else
            {
                currentSelectionFrame.SetActive(false);
            }
        }
    }

    void PopulateTestWeapons()
    {
        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.transform.SetParent(this.transform, false);
            currentSelectionFrame.SetActive(false);
        }

        ClearAllMultiSelectFrames();

        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        spawnedItemToIndexMap.Clear();
        spawnedArmorMasterIndexMap.Clear();

        if (selectedWeapons != null)
        {
            for (int s = 0; s < selectedWeapons.Length; s++)
            {
                selectedWeapons[s] = null;
            }
        }

        if (testWeaponPrefabs == null) return;

        for (int i = 0; i < testWeaponPrefabs.Length; i++)
        {
            GameObject prefab = testWeaponPrefabs[i];

            if (prefab != null && IsPrefabMatchCurrentRarity(prefab))
            {
                GameObject newWeapon = Instantiate(prefab, contentPanel);

                WeaponItemUI itemUI = newWeapon.GetComponent<WeaponItemUI>();
                if (itemUI == null) itemUI = newWeapon.AddComponent<WeaponItemUI>();

                itemUI.Setup(this, prefab);

                spawnedItemToIndexMap[newWeapon.transform] = i;

                for (int s = 0; s < selectedWeapons.Length; s++)
                {
                    if (selectedWeaponItemIndices[s] == i)
                    {
                        selectedWeapons[s] = newWeapon.transform;
                    }
                }
            }
        }

        UpdateEvolutionButtonState();
    }

    public void SelectWeapon(Transform clickedItemTransform, GameObject itemPrefab)
    {
        if (clickedItemTransform == null) return;

        if (isMultiSelectMode)
        {
            if (multiSelectFrames.TryGetValue(clickedItemTransform, out GameObject existingFrame))
            {
                if (existingFrame != null)
                {
                    Destroy(existingFrame);
                }
                multiSelectFrames.Remove(clickedItemTransform);
            }
            else
            {
                if (selectionFramePrefab != null)
                {
                    GameObject newFrame = Instantiate(selectionFramePrefab, clickedItemTransform, false);
                    newFrame.transform.localPosition = Vector3.zero;
                    newFrame.transform.SetAsLastSibling();
                    newFrame.SetActive(true);
                    multiSelectFrames[clickedItemTransform] = newFrame;
                }
            }

            UpdateEvolutionButtonState();
            return;
        }

        if (isShowingArmor)
        {
            if (categoryButtons == null || currentCategoryIndex < 0 || currentCategoryIndex >= categoryButtons.Length) return;

            selectedArmors[currentCategoryIndex] = clickedItemTransform;

            if (spawnedItemToIndexMap.TryGetValue(clickedItemTransform, out int originalArmorIndex))
            {
                selectedArmorItemIndices[currentCategoryIndex] = originalArmorIndex;
            }

            if (currentSelectionFrame != null)
            {
                currentSelectionFrame.SetActive(true);
                currentSelectionFrame.transform.SetParent(clickedItemTransform, false);
                currentSelectionFrame.transform.localPosition = Vector3.zero;
                currentSelectionFrame.transform.SetAsLastSibling();
            }

            ContentCategoryButton currentCategory = categoryButtons[currentCategoryIndex];

            if (currentCategory.uiToHideOnSelect != null)
            {
                currentCategory.uiToHideOnSelect.SetActive(false);
            }

            if (currentCategory.targetArmorImage != null && currentCategory.displayTargetPrefabs != null)
            {
                foreach (ArmorTargetElement element in currentCategory.displayTargetPrefabs)
                {
                    if (element != null && element.targetPrefab == itemPrefab)
                    {
                        currentCategory.targetArmorImage.sprite = element.armorSprite;
                        currentCategory.targetArmorImage.enabled = true;
                        UpdateArmorImageAlpha(currentCategory.targetArmorImage);
                        break;
                    }
                }
            }

            ArmorStats stats = null;
            if (itemPrefab != null)
            {
                stats = itemPrefab.GetComponentInChildren<ArmorStats>(true);
            }
            if (stats == null && clickedItemTransform != null)
            {
                stats = clickedItemTransform.GetComponentInChildren<ArmorStats>(true);
            }
            ArmorStats.EquipArmor(currentCategoryIndex, stats);

            SaveEquippedSelections();

            UpdateStatusTexts();
            UpdateWeaponStatTexts();

            return;
        }

        selectedWeapons[currentSlotIndex] = clickedItemTransform;
        selectedSlotWeaponPrefabs[currentSlotIndex] = itemPrefab;

        if (spawnedItemToIndexMap.TryGetValue(clickedItemTransform, out int originalWeaponIndex))
        {
            selectedWeaponItemIndices[currentSlotIndex] = originalWeaponIndex;
        }

        WeaponDataKeeper.equippedWeaponPrefabs[currentSlotIndex] = itemPrefab;

        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.SetActive(true);
            currentSelectionFrame.transform.SetParent(clickedItemTransform, false);
            currentSelectionFrame.transform.localPosition = Vector3.zero;
            currentSelectionFrame.transform.SetAsLastSibling();
        }

        SaveEquippedSelections();

        currentStatSlotIndex = currentSlotIndex;
        UpdateWeaponStatTexts();
    }

    public void OnClickCategoryButton(int buttonIndex)
    {
        isShowingArmor = true;
        currentCategoryIndex = buttonIndex;

        if (categoryButtons != null)
        {
            for (int i = 0; i < categoryButtons.Length; i++)
            {
                if (categoryButtons[i].buttonImage != null)
                {
                    categoryButtons[i].buttonImage.color = (i == currentCategoryIndex) ? activeCategoryColor : inactiveCategoryColor;
                }
            }
        }

        PopulateArmorContent();
        if (!isMultiSelectMode)
        {
            UpdateArmorSelectionFrame();
        }
    }

    void PopulateArmorContent()
    {
        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.transform.SetParent(this.transform, false);
            currentSelectionFrame.SetActive(false);
        }

        ClearAllMultiSelectFrames();

        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        spawnedItemToIndexMap.Clear();
        spawnedArmorMasterIndexMap.Clear();

        if (armorPrefabs == null || categoryButtons == null) return;
        if (currentCategoryIndex < 0 || currentCategoryIndex >= categoryButtons.Length) return;

        if (selectedArmors != null && currentCategoryIndex < selectedArmors.Length)
        {
            selectedArmors[currentCategoryIndex] = null;
        }

        ArmorTargetElement[] allowedList = categoryButtons[currentCategoryIndex].displayTargetPrefabs;
        List<GameObject> itemsToDisplay = new List<GameObject>();
        List<int> itemsMasterIndices = new List<int>();

        if (allowedList != null && allowedList.Length > 0)
        {
            bool[] usedMasterFlags = new bool[armorPrefabs.Length];

            foreach (ArmorTargetElement allowedElement in allowedList)
            {
                if (allowedElement == null || allowedElement.targetPrefab == null) continue;

                for (int m = 0; m < armorPrefabs.Length; m++)
                {
                    if (!usedMasterFlags[m] && armorPrefabs[m] == allowedElement.targetPrefab)
                    {
                        itemsToDecision(armorPrefabs[m], itemsToDisplay, itemsMasterIndices, m, usedMasterFlags);
                    }
                }
            }
        }

        for (int i = 0; i < itemsToDisplay.Count; i++)
        {
            GameObject prefab = itemsToDisplay[i];

            if (prefab != null && IsPrefabMatchCurrentRarity(prefab))
            {
                GameObject newArmor = Instantiate(prefab, contentPanel);

                WeaponItemUI itemUI = newArmor.GetComponent<WeaponItemUI>();
                if (itemUI == null) itemUI = newArmor.AddComponent<WeaponItemUI>();

                itemUI.Setup(this, prefab);

                spawnedItemToIndexMap[newArmor.transform] = i;
                spawnedArmorMasterIndexMap[newArmor.transform] = itemsMasterIndices[i];

                if (selectedArmorItemIndices != null && currentCategoryIndex < selectedArmorItemIndices.Length)
                {
                    if (selectedArmorItemIndices[currentCategoryIndex] == i)
                    {
                        selectedArmors[currentCategoryIndex] = newArmor.transform;
                    }
                }
            }
        }

        UpdateEvolutionButtonState();
    }

    private void itemsToDecision(GameObject prefab, List<GameObject> list, List<int> indices, int m, bool[] flags)
    {
        list.Add(prefab);
        indices.Add(m);
        flags[m] = true;
    }
}