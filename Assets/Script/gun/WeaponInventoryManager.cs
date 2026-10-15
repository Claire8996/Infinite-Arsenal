using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
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

    [Tooltip("このSlotの「弾の飛ぶ速度」を数字で表示するテキスト")]
    public TextMeshProUGUI bulletSpeedValueText;

    [Tooltip("このSlotの「弾のブレ」を数字で表示するテキスト")]
    public TextMeshProUGUI spreadValueText;
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

    [Tooltip("武器ステータス切替ボタンの「選択されていない」時の色（灰色）")]
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

    private GameObject currentSelectionFrame;

    // 武器スロットごとに選択された武器（Transform / Prefab）とインデックスを記憶する配列
    private Transform[] selectedWeapons;
    private GameObject[] selectedSlotWeaponPrefabs;
    private int[] selectedWeaponItemIndices;

    // 防具カテゴリごとに選択された防具（Transform）とインデックスを記憶する配列
    private Transform[] selectedArmors;
    private int[] selectedArmorItemIndices;

    private int currentSlotIndex = 0;
    private int currentCategoryIndex = -1;
    private bool isShowingArmor = false;

    // 現在表示中の武器ステータスのSlot番号（0 = Slot1, 1 = Slot2, 2 = Slot3）
    private int currentStatSlotIndex = 0;

    void Start()
    {
        // もし同じシーン内に PlayerHealth や PlayerMovement があれば、その初期値を基準値として同期する
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

        // 武器の選択記憶用配列の初期化
        selectedWeapons = new Transform[slotButtons.Length];
        selectedSlotWeaponPrefabs = new GameObject[slotButtons.Length];
        selectedWeaponItemIndices = new int[slotButtons.Length];
        for (int i = 0; i < selectedWeaponItemIndices.Length; i++)
        {
            selectedWeaponItemIndices[i] = -1;
        }

        // 武器ステータス切替ボタンの自動色変化（ホバー時など）を無効化
        if (weaponSlotStatUIs != null)
        {
            for (int i = 0; i < weaponSlotStatUIs.Length; i++)
            {
                if (weaponSlotStatUIs[i] != null && weaponSlotStatUIs[i].statSwitchButtonImage != null)
                {
                    DisableButtonColorTransition(weaponSlotStatUIs[i].statSwitchButtonImage);
                }
            }
        }

        // 防具の選択記憶用配列の初期化と、Target Armor Imageの初期Alpha設定
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

        // ガチャ結果から武器と防具を受け取ってそれぞれの配列に追加
        CheckAndAddNewItemsFromKeeper();

        // 武器の並び替え
        SortWeapons();

        // 最初に武器を生成して並べ、スロット0番を選択状態にする
        PopulateTestWeapons();
        OnSlotButtonClicked(0);

        // ゲーム開始時にプレイヤーの3つのステータステキストを更新する
        UpdateStatusTexts();

        // ゲーム開始時はSlot1（インデックス0）の武器ステータス切替ボタンを選択状態にする
        OnClickWeaponStatSlotButton(0);
    }

    // ボタンの標準のホバー色変化などを無効化し、スクリプト側の色制御を維持する処理
    private void DisableButtonColorTransition(Image targetImage)
    {
        if (targetImage == null) return;

        Button btn = targetImage.GetComponent<Button>();
        if (btn == null) btn = targetImage.GetComponentInParent<Button>();

        if (btn != null)
        {
            btn.transition = Selectable.Transition.None;
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
        }
    }

    // ==========================================
    // 3つのテキストに「現在の体力・防御力・移動速度」を数字で表示するメソッド
    // ==========================================
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

    // ==========================================
    // Slot1〜3の武器ステータステキストの表示切替ボタン用メソッド
    // ==========================================
    public void OnClickWeaponStatSlotButton(int statSlotIndex)
    {
        currentStatSlotIndex = statSlotIndex;
        UpdateWeaponStatTexts();
    }

    // ==========================================
    // ★修正：武器のPrefabが選択されていないSlotのテキストは非表示にし、
    // 選択されている時だけ数値を表示する処理
    // ==========================================
    public void UpdateWeaponStatTexts()
    {
        if (weaponSlotStatUIs == null || weaponSlotStatUIs.Length == 0) return;

        // 1. まず全Slotの切替ボタンの色を更新し、すべてのSlotのテキストを一旦非表示にする
        for (int i = 0; i < weaponSlotStatUIs.Length; i++)
        {
            WeaponSlotStatUI slotUI = weaponSlotStatUIs[i];
            if (slotUI == null) continue;

            // 選択されているボタンを通常色、選択されていないボタンを灰色にする
            if (slotUI.statSwitchButtonImage != null)
            {
                slotUI.statSwitchButtonImage.color = (i == currentStatSlotIndex) ? activeStatButtonColor : inactiveStatButtonColor;
            }

            // すべてのテキストを一旦非表示にする
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

        // 2. 現在表示切替ボタンで選ばれているSlot（currentStatSlotIndex）を確認する
        if (currentStatSlotIndex >= 0 && currentStatSlotIndex < weaponSlotStatUIs.Length)
        {
            WeaponSlotStatUI activeSlotUI = weaponSlotStatUIs[currentStatSlotIndex];
            if (activeSlotUI != null)
            {
                // そのSlotで現在選択されている武器のPrefabを取得する
                GameObject activeWeaponPrefab = GetEquippedWeaponPrefabForSlot(currentStatSlotIndex);

                // ★重要：武器のPrefabが選択されている（nullではない）時だけ、数値を反映してテキストを表示する！
                // （未選択の場合は上のループで SetActive(false) になっているため非表示のままになります）
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

    // ★修正：このインベントリ画面でそのSlotに選択されている武器のPrefabを取得する
    // （未選択の場合は確実に null を返し、テキストを非表示にします）
    private GameObject GetEquippedWeaponPrefabForSlot(int slotIndex)
    {
        if (selectedSlotWeaponPrefabs != null && slotIndex >= 0 && slotIndex < selectedSlotWeaponPrefabs.Length)
        {
            return selectedSlotWeaponPrefabs[slotIndex];
        }

        return null;
    }

    // 武器のPrefabから「攻撃力（Bulletのダメージ＋防具ボーナス）」「弾の飛ぶ速度」「弾のブレ」を読み取るメソッド
    private void GetWeaponStatsFromPrefab(GameObject itemPrefab, out int totalAttack, out float bulletSpeed, out float spreadAmount)
    {
        totalAttack = 0;
        bulletSpeed = 0f;
        spreadAmount = 0f;

        if (itemPrefab == null) return;

        // 1. WeaponModelLinker がアタッチされていれば、実際の3D銃Prefab（actualWeaponPrefab）を参照する
        GameObject targetGunObj = itemPrefab;
        WeaponModelLinker linker = itemPrefab.GetComponentInChildren<WeaponModelLinker>(true);
        if (linker != null && linker.actualWeaponPrefab != null)
        {
            targetGunObj = linker.actualWeaponPrefab;
        }

        GameObject bulletPrefab = null;
        bool isGunFound = false;

        // 2. 各銃スクリプト（AK47, Shotgun, LMG, M4, M107, M249, Pistol, RPG7）から弾速とブレを取得する
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

        // 3. 銃に設定されている弾（Bullet または RocketBullet）からダメージを取得し、防具の攻撃力ボーナスを加算する
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
    // 武器のSlot切り替えボタン（3つ）を押した時の処理
    // ==========================================
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

        // 武器ステータステキストを最新に更新
        UpdateWeaponStatTexts();
    }

    void PopulateTestWeapons()
    {
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
    // 武器・防具をクリックして選択した時の処理
    // ==========================================
    public void SelectWeapon(Transform clickedItemTransform, GameObject itemPrefab)
    {
        // ------------------------------------------
        // パターンA：防具のContent表示中に防具がクリックされた場合
        // ------------------------------------------
        if (isShowingArmor)
        {
            if (categoryButtons == null || currentCategoryIndex < 0 || currentCategoryIndex >= categoryButtons.Length) return;

            selectedArmors[currentCategoryIndex] = clickedItemTransform;

            selectedArmorItemIndices[currentCategoryIndex] = -1;
            for (int i = 0; i < contentPanel.childCount; i++)
            {
                if (contentPanel.GetChild(i) == clickedItemTransform)
                {
                    selectedArmorItemIndices[currentCategoryIndex] = i;
                    break;
                }
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

            // プレイヤーのステータステキストと、武器の攻撃力テキスト（防具ボーナス反映分）を両方更新する
            UpdateStatusTexts();
            UpdateWeaponStatTexts();

            return;
        }

        // ------------------------------------------
        // パターンB：武器のContent表示中に武器がクリックされた場合
        // ------------------------------------------
        selectedWeapons[currentSlotIndex] = clickedItemTransform;
        selectedSlotWeaponPrefabs[currentSlotIndex] = itemPrefab;

        selectedWeaponItemIndices[currentSlotIndex] = -1;
        for (int i = 0; i < contentPanel.childCount; i++)
        {
            if (contentPanel.GetChild(i) == clickedItemTransform)
            {
                selectedWeaponItemIndices[currentSlotIndex] = i;
                break;
            }
        }

        WeaponDataKeeper.equippedWeaponPrefabs[currentSlotIndex] = itemPrefab;

        if (currentSelectionFrame != null)
        {
            currentSelectionFrame.SetActive(true);
            currentSelectionFrame.transform.SetParent(clickedItemTransform, false);
            currentSelectionFrame.transform.localPosition = Vector3.zero;
            currentSelectionFrame.transform.SetAsLastSibling();
        }

        // 武器が選択されたので、Slot1〜3の武器ステータステキスト（攻撃力・弾速・ブレ）を更新して表示する
        UpdateWeaponStatTexts();
    }

    // ==========================================
    // 防具のContent表示切り替えボタンを押した時の処理
    // ==========================================
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

        if (currentSelectionFrame != null && selectedArmors != null && currentCategoryIndex >= 0 && currentCategoryIndex < selectedArmors.Length)
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

    void PopulateArmorContent()
    {
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

        ArmorTargetElement[] allowedList = categoryButtons[currentCategoryIndex].displayTargetPrefabs;
        List<GameObject> itemsToDisplay = new List<GameObject>();

        if (allowedList != null && allowedList.Length > 0)
        {
            List<GameObject> tempOwnedArmors = new List<GameObject>(armorPrefabs);

            foreach (ArmorTargetElement allowedElement in allowedList)
            {
                if (allowedElement == null || allowedElement.targetPrefab == null) continue;

                for (int i = 0; i < tempOwnedArmors.Count; i++)
                {
                    if (tempOwnedArmors[i] == allowedElement.targetPrefab)
                    {
                        itemsToDisplay.Add(tempOwnedArmors[i]);
                        tempOwnedArmors.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        for (int i = 0; i < itemsToDisplay.Count; i++)
        {
            GameObject prefab = itemsToDisplay[i];
            if (prefab != null)
            {
                GameObject newArmor = Instantiate(prefab, contentPanel);

                WeaponItemUI itemUI = newArmor.GetComponent<WeaponItemUI>();
                if (itemUI == null) itemUI = newArmor.AddComponent<WeaponItemUI>();

                itemUI.Setup(this, prefab);

                if (selectedArmorItemIndices != null && currentCategoryIndex < selectedArmorItemIndices.Length)
                {
                    if (selectedArmorItemIndices[currentCategoryIndex] == i)
                    {
                        selectedArmors[currentCategoryIndex] = newArmor.transform;
                    }
                }
            }
        }
    }
}