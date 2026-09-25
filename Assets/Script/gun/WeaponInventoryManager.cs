using UnityEngine;
using UnityEngine.UI;

public class WeaponInventoryManager : MonoBehaviour
{
    [Header("スロット（Selectボタン）設定")]
    public Image[] slotButtons;
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;

    [Header("UI設定")]
    public Transform contentPanel;
    public GameObject selectionFramePrefab;

    [Header("デバッグ（テスト）用設定")]
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

        PopulateTestWeapons();
        OnSlotButtonClicked(0);
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

                // ★改修：プレハブの情報も一緒に渡す
                itemUI.Setup(this, prefab);
            }
        }
    }

    public void AddWeaponFromGacha(GameObject newWeaponPrefab)
    {
        if (newWeaponPrefab != null && contentPanel != null)
        {
            GameObject newWeapon = Instantiate(newWeaponPrefab, contentPanel);

            WeaponItemUI itemUI = newWeapon.GetComponent<WeaponItemUI>();
            if (itemUI == null) itemUI = newWeapon.AddComponent<WeaponItemUI>();

            itemUI.Setup(this, newWeaponPrefab);
        }
    }

    // ★改修：プレハブの情報を受け取るように変更
    public void SelectWeapon(Transform clickedWeaponTransform, GameObject weaponPrefab)
    {
        selectedWeapons[currentSlotIndex] = clickedWeaponTransform;

        // ===============================================
        // ★追加：選んだ武器をシーンまたぎ用のカバンに保存！
        // ===============================================
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