using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class ContentCategoryButton
{
    public string buttonName = "ボタン名（メモ用）";
    [Tooltip("（任意）押した時に色を変えたい場合はボタンのImageをセットしてください")]
    public Image buttonImage;
    [Tooltip("このボタンを押した時にContent内へ表示するPrefabのリスト（※ここに入っていないPrefabは表示されません）")]
    public GameObject[] displayTargetPrefabs;
}

public class WeaponInventoryManager : MonoBehaviour
{
    [Header("スロット（Selectボタン）設定")]
    public Image[] slotButtons;
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;

    [Header("UI設定")]
    public Transform contentPanel;
    public GameObject selectionFramePrefab;

    [Header("所持している武器のリスト")]
    public GameObject[] testWeaponPrefabs;

    [Header("所持している防具のリスト")]
    public GameObject[] armorPrefabs;

    [Header("武器の並び順（ここに設定したPrefabの順番で並び替わります）")]
    [Tooltip("ここに設定されていない武器は、最後にまとめて表示されます")]
    public GameObject[] weaponSortOrder;

    [Header("【Content表示切り替えボタン設定】")]
    [Tooltip("各ボタンを押した時に表示したいPrefab（inventoryUIPrefabと同じもの）をそれぞれ登録してください")]
    public ContentCategoryButton[] categoryButtons;
    public Color activeCategoryColor = Color.white;
    public Color inactiveCategoryColor = Color.gray;

    private GameObject currentSelectionFrame;
    private GameObject[] selectedWeaponPrefabs;
    private int currentSlotIndex = 0;
    private int currentCategoryIndex = 0;

    void Start()
    {
        selectedWeaponPrefabs = new GameObject[slotButtons.Length];

        if (selectionFramePrefab != null)
        {
            currentSelectionFrame = Instantiate(selectionFramePrefab);
            currentSelectionFrame.SetActive(false);
        }

        // ガチャ結果から武器と防具を受け取ってそれぞれの配列に追加
        CheckAndAddNewItemsFromKeeper();

        // 武器の並び替え
        SortWeapons();

        // 最初は0番目のカテゴリボタン（初期表示）を開く
        OnClickCategoryButton(0);
        OnSlotButtonClicked(0);
    }

    // ==========================================
    // 💡 修正：ArmorItemLinkerを使わず、CapsuleOpenerで振り分けられたリストから直接追加する
    // ==========================================
    private void CheckAndAddNewItemsFromKeeper()
    {
        // 1. 武器の追加チェック
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
            Debug.Log("ガチャ結果の新しい武器をインベントリに追加しました！");
        }

        // 2. 防具の追加チェック
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
            Debug.Log("ガチャ結果の新しい防具をインベントリに追加しました！");
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

    // ==========================================
    // 複数のボタンを押した時にContentの表示を切り替えるメソッド
    // ==========================================
    public void OnClickCategoryButton(int buttonIndex)
    {
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

        PopulateContent();
    }

    void PopulateContent()
    {
        // 赤い選択枠が削除されないように一時退避
        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.transform.SetParent(this.transform, false);
            currentSelectionFrame.SetActive(false);
        }

        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        // 現在所持しているすべての武器・防具（inventoryUIPrefab）をまとめる
        List<GameObject> allOwnedItems = new List<GameObject>();
        if (testWeaponPrefabs != null) allOwnedItems.AddRange(testWeaponPrefabs);
        if (armorPrefabs != null) allOwnedItems.AddRange(armorPrefabs);

        List<GameObject> itemsToDisplay = new List<GameObject>();

        if (categoryButtons != null && categoryButtons.Length > currentCategoryIndex)
        {
            GameObject[] allowedList = categoryButtons[currentCategoryIndex].displayTargetPrefabs;

            if (allowedList != null && allowedList.Length > 0)
            {
                // 押されたボタンの displayTargetPrefabs に登録されているPrefabだけを抽出する
                // （※ここに登録されていないPrefabは一切表示されません）
                List<GameObject> tempOwned = new List<GameObject>(allOwnedItems);

                foreach (GameObject allowedPrefab in allowedList)
                {
                    if (allowedPrefab == null) continue;

                    for (int i = 0; i < tempOwned.Count; i++)
                    {
                        if (tempOwned[i] == allowedPrefab)
                        {
                            itemsToDisplay.Add(tempOwned[i]);
                            tempOwned.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
        }
        else
        {
            // categoryButtons が未設定の場合は武器リストを表示
            if (testWeaponPrefabs != null) itemsToDisplay.AddRange(testWeaponPrefabs);
        }

        // 対象となったPrefabだけをContent内に生成
        foreach (GameObject prefab in itemsToDisplay)
        {
            if (prefab != null)
            {
                GameObject newItem = Instantiate(prefab, contentPanel);

                WeaponItemUI itemUI = newItem.GetComponent<WeaponItemUI>();
                if (itemUI == null) itemUI = newItem.AddComponent<WeaponItemUI>();

                itemUI.Setup(this, prefab);

                if (selectedWeaponPrefabs[currentSlotIndex] == prefab && currentSelectionFrame != null)
                {
                    currentSelectionFrame.SetActive(true);
                    currentSelectionFrame.transform.SetParent(newItem.transform, false);
                    currentSelectionFrame.transform.localPosition = Vector3.zero;
                    currentSelectionFrame.transform.SetAsLastSibling();
                }
            }
        }
    }

    public void OnSlotButtonClicked(int slotIndex)
    {
        currentSlotIndex = slotIndex;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] != null)
            {
                slotButtons[i].color = (i == currentSlotIndex) ? activeColor : inactiveColor;
            }
        }

        UpdateSelectionFramePosition();
    }

    public void SelectWeapon(Transform clickedWeaponTransform, GameObject weaponPrefab)
    {
        selectedWeaponPrefabs[currentSlotIndex] = weaponPrefab;
        WeaponDataKeeper.equippedWeaponPrefabs[currentSlotIndex] = weaponPrefab;

        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.SetActive(true);
            currentSelectionFrame.transform.SetParent(clickedWeaponTransform, false);
            currentSelectionFrame.transform.localPosition = Vector3.zero;
            currentSelectionFrame.transform.SetAsLastSibling();
        }
    }

    private void UpdateSelectionFramePosition()
    {
        if (currentSelectionFrame == null) return;

        GameObject targetPrefab = selectedWeaponPrefabs[currentSlotIndex];
        if (targetPrefab == null)
        {
            currentSelectionFrame.SetActive(false);
            return;
        }

        foreach (Transform child in contentPanel)
        {
            WeaponItemUI itemUI = child.GetComponent<WeaponItemUI>();
            if (itemUI != null && child.name.StartsWith(targetPrefab.name))
            {
                currentSelectionFrame.SetActive(true);
                currentSelectionFrame.transform.SetParent(child, false);
                currentSelectionFrame.transform.localPosition = Vector3.zero;
                currentSelectionFrame.transform.SetAsLastSibling();
                return;
            }
        }

        currentSelectionFrame.SetActive(false);
    }
}