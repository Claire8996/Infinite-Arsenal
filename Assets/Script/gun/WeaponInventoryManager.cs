using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class ContentCategoryButton
{
    public string buttonName = "ボタン名（メモ用）";
    [Tooltip("（任意）押した時に色を変えたい場合はボタンのImageをセットしてください")]
    public Image buttonImage;
    [Tooltip("このボタンを押した時にContent内へ表示する防具Prefabのリスト（※ここに入っていないPrefabは表示されません）")]
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

    [Header("【防具のContent表示切り替えボタン設定】")]
    [Tooltip("各ボタンを押した時に表示したい防具Prefab（inventoryUIPrefabと同じもの）をそれぞれ登録してください")]
    public ContentCategoryButton[] categoryButtons;
    public Color activeCategoryColor = Color.white;
    public Color inactiveCategoryColor = Color.gray;

    private GameObject currentSelectionFrame;

    // ★復活：既存の選択された武器（Transform）を記憶する配列
    private Transform[] selectedWeapons;
    // 防具タブから武器タブへ戻ってきた時に、どの武器だったか復元するための補助配列
    private int[] selectedWeaponItemIndices;

    private int currentSlotIndex = 0;
    private int currentCategoryIndex = -1;
    private bool isShowingArmor = false;

    void Start()
    {
        selectedWeapons = new Transform[slotButtons.Length];
        selectedWeaponItemIndices = new int[slotButtons.Length];
        for (int i = 0; i < selectedWeaponItemIndices.Length; i++)
        {
            selectedWeaponItemIndices[i] = -1;
        }

        if (selectionFramePrefab != null)
        {
            currentSelectionFrame = Instantiate(selectionFramePrefab);
            currentSelectionFrame.SetActive(false);
        }

        // ガチャ結果から武器と防具を受け取ってそれぞれの配列に追加
        CheckAndAddNewItemsFromKeeper();

        // 武器の並び替え
        SortWeapons();

        // ★既存通り：最初に武器を生成して並べ、スロット0番を選択状態にする
        PopulateTestWeapons();
        OnSlotButtonClicked(0);
    }

    // ガチャ結果から武器と防具をそれぞれのリストに追加する
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
    // ★完全復活：武器のSlot切り替えボタン（3つ）を押した時の既存の処理
    // ==========================================
    public void OnSlotButtonClicked(int slotIndex)
    {
        // もし「防具のContent」を表示している状態から武器Slotボタンが押されたら、
        // Contentの中身を武器一覧に戻す
        if (isShowingArmor)
        {
            isShowingArmor = false;
            currentCategoryIndex = -1;

            // 防具ボタンの色を非選択色に戻す
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

            // 武器をContentに再表示する（selectedWeaponsも復元される）
            PopulateTestWeapons();
        }

        // --- ここから下は元のスクリプトと全く同じ処理です ---
        currentSlotIndex = slotIndex;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] != null)
            {
                slotButtons[i].color = (i == currentSlotIndex) ? activeColor : inactiveColor;
            }
        }

        Transform savedWeapon = selectedWeapons[currentSlotIndex];
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

    // ==========================================
    // ★復活：武器をContentに並べる既存のメソッド
    // ==========================================
    void PopulateTestWeapons()
    {
        // 選択枠が一緒に消えないように退避しておく
        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.transform.SetParent(this.transform, false);
            currentSelectionFrame.SetActive(false);
        }

        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        if (testWeaponPrefabs == null) return;

        for (int i = 0; i < testWeaponPrefabs.Length; i++)
        {
            GameObject prefab = testWeaponPrefabs[i];
            if (prefab != null)
            {
                GameObject newWeapon = Instantiate(prefab, contentPanel);

                WeaponItemUI itemUI = newWeapon.GetComponent<WeaponItemUI>();
                if (itemUI == null) itemUI = newWeapon.AddComponent<WeaponItemUI>();

                itemUI.Setup(this, prefab);

                // 防具画面から武器画面に戻ってきた時、以前に選択していた武器のTransformを復元する
                for (int s = 0; s < selectedWeapons.Length; s++)
                {
                    if (selectedWeaponItemIndices[s] == i)
                    {
                        selectedWeapons[s] = newWeapon.transform;
                    }
                }
            }
        }
    }

    // ==========================================
    // ★完全復活：武器をクリックして選択した時の既存の処理
    // ==========================================
    public void SelectWeapon(Transform clickedWeaponTransform, GameObject weaponPrefab)
    {
        if (isShowingArmor) return;

        selectedWeapons[currentSlotIndex] = clickedWeaponTransform;

        // 何番目の武器を選んだかも記録しておく（防具画面から戻った時の復元用）
        selectedWeaponItemIndices[currentSlotIndex] = -1;
        for (int i = 0; i < contentPanel.childCount; i++)
        {
            if (contentPanel.GetChild(i) == clickedWeaponTransform)
            {
                selectedWeaponItemIndices[currentSlotIndex] = i;
                break;
            }
        }

        WeaponDataKeeper.equippedWeaponPrefabs[currentSlotIndex] = weaponPrefab;

        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.SetActive(true);
            currentSelectionFrame.transform.SetParent(clickedWeaponTransform, false);
            currentSelectionFrame.transform.localPosition = Vector3.zero;
            currentSelectionFrame.transform.SetAsLastSibling();
        }
    }

    // ==========================================
    // 防具のContent表示切り替えボタンを押した時の処理
    // ==========================================
    public void OnClickCategoryButton(int buttonIndex)
    {
        isShowingArmor = true;
        currentCategoryIndex = buttonIndex;

        // 防具ボタンの色を更新
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
    }

    // 防具だけをContentに並べる処理（選択枠やWeaponItemUIは一切つけない）
    void PopulateArmorContent()
    {
        // 防具を表示する時は、選択枠が削除されないように退避して非表示にする
        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.transform.SetParent(this.transform, false);
            currentSelectionFrame.SetActive(false);
        }

        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        if (armorPrefabs == null || categoryButtons == null) return;
        if (currentCategoryIndex < 0 || currentCategoryIndex >= categoryButtons.Length) return;

        GameObject[] allowedList = categoryButtons[currentCategoryIndex].displayTargetPrefabs;
        List<GameObject> itemsToDisplay = new List<GameObject>();

        if (allowedList != null && allowedList.Length > 0)
        {
            List<GameObject> tempOwnedArmors = new List<GameObject>(armorPrefabs);

            foreach (GameObject allowedPrefab in allowedList)
            {
                if (allowedPrefab == null) continue;

                for (int i = 0; i < tempOwnedArmors.Count; i++)
                {
                    if (tempOwnedArmors[i] == allowedPrefab)
                    {
                        itemsToDisplay.Add(tempOwnedArmors[i]);
                        tempOwnedArmors.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        foreach (GameObject prefab in itemsToDisplay)
        {
            if (prefab != null)
            {
                GameObject newArmor = Instantiate(prefab, contentPanel);

                // 防具には WeaponItemUI（クリックして選択枠をつける機能）がつかないようにする
                WeaponItemUI itemUI = newArmor.GetComponent<WeaponItemUI>();
                if (itemUI != null)
                {
                    Destroy(itemUI);
                }
            }
        }
    }
}