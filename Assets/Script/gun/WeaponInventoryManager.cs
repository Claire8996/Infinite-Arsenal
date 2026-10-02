using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WeaponInventoryManager : MonoBehaviour
{
    [Header("スロット（Selectボタン）設定")]
    public Image[] slotButtons;
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;

    [Header("UI設定")]
    public Transform contentPanel;
    public GameObject selectionFramePrefab;

    [Header("所持している武器のリスト")] // ※テスト用から「所持リスト」に名前を変えました
    public GameObject[] testWeaponPrefabs;

    private GameObject currentSelectionFrame;
    private Transform[] selectedWeapons;
    private int currentSlotIndex = 0;

    void Start()
    {
        selectedWeapons = new Transform[slotButtons.Length];

        if (selectionFramePrefab != null)
        {
            currentSelectionFrame = Instantiate(selectionFramePrefab);
            currentSelectionFrame.SetActive(false);
        }

        // ★追加：ガチャ結果の一時保存データを確認して、新しい武器をリストに追加する
        CheckAndAddNewWeaponsFromKeeper();

        PopulateTestWeapons();
        OnSlotButtonClicked(0);
    }

    // ★追加：シーン開始時に呼ばれる。Keeperから武器を取り出す処理
    private void CheckAndAddNewWeaponsFromKeeper()
    {
        // もし保存された武器が1つでもあれば
        if (WeaponDataKeeper.gachaResultWeapons.Count > 0)
        {
            // 今の配列をListに変換する
            List<GameObject> currentList = new List<GameObject>(testWeaponPrefabs);

            // Keeperに入っている新しい武器をすべて追加する
            foreach (GameObject newWeapon in WeaponDataKeeper.gachaResultWeapons)
            {
                if (newWeapon != null)
                {
                    currentList.Add(newWeapon);
                }
            }

            // 配列に戻して保存
            testWeaponPrefabs = currentList.ToArray();

            // 受け取ったので、Keeperのリストは空っぽにしてリセットする
            WeaponDataKeeper.gachaResultWeapons.Clear();

            Debug.Log("ガチャ結果の新しい武器をインベントリに追加しました！");
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

    void PopulateTestWeapons()
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (GameObject prefab in testWeaponPrefabs)
        {
            if (prefab != null)
            {
                GameObject newWeapon = Instantiate(prefab, contentPanel);

                WeaponItemUI itemUI = newWeapon.GetComponent<WeaponItemUI>();
                if (itemUI == null) itemUI = newWeapon.AddComponent<WeaponItemUI>();

                itemUI.Setup(this, prefab);
            }
        }
    }

    public void SelectWeapon(Transform clickedWeaponTransform, GameObject weaponPrefab)
    {
        selectedWeapons[currentSlotIndex] = clickedWeaponTransform;

        WeaponDataKeeper.equippedWeaponPrefabs[currentSlotIndex] = weaponPrefab;

        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.SetActive(true);
            currentSelectionFrame.transform.SetParent(clickedWeaponTransform, false);
            currentSelectionFrame.transform.localPosition = Vector3.zero;
            currentSelectionFrame.transform.SetAsLastSibling();
        }
    }
}