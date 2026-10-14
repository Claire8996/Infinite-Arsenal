using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class InnerItem
{
    public string itemName;
    [Tooltip("ガチャ玉から飛び出す演出用のPrefab")]
    public GameObject itemPrefab;

    [Tooltip("インベントリ画面のContentに追加されるUI用Prefab")]
    public GameObject inventoryUIPrefab;

    // ==========================================
    // 💡 追加：武器か防具か（どのContent・配列に入れるか）を判別するチェックボックス
    // ==========================================
    [Tooltip("チェックを入れると「防具」として判別され、WeaponInventoryManagerの防具リスト（armorPrefabs）に入ります。\nチェックを外すと「武器」として扱われます。")]
    public bool isArmor;

    public float probabilityWeight;

    [Tooltip("チェックを入れると、このアイテムは排出されなくなります（確率0%）")]
    public bool isProbabilityZero;
}

public class CapsuleOpener : MonoBehaviour
{
    // ==========================================
    // 💡 追加：ガチャで引いた「防具のinventoryUIPrefab」を一時保存してインベントリへ渡すリスト
    // （WeaponDataKeeperを書き換えなくてもそのまま動くようにここに持たせています）
    // ==========================================
    public static List<GameObject> gachaResultArmors = new List<GameObject>();

    [Header("【カプセルのパーツ設定】")]
    public RectTransform topHalf;
    public RectTransform bottomHalf;

    [Header("【アニメーション設定】")]
    public float openDistance = 30f;
    public float openDuration = 0.3f;

    [Header("【エフェクト設定】")]
    public GameObject smokeEffectPrefab;

    [Tooltip("次のカプセルを開けられるようになるまでの待機時間（煙が消えるまでの時間）")]
    public float destroyDelay = 1.5f;

    [Header("【中身のアイテム（確率設定）】")]
    public List<InnerItem> innerItems;

    private Button myButton;
    private bool isOpened = false;

    private static bool isAnyCapsuleOpening = false;

    void Start()
    {
        myButton = GetComponent<Button>();

        if (myButton != null)
        {
            myButton.interactable = false;
            myButton.onClick.AddListener(OnClickCapsule);
        }

        gameObject.tag = "GachaCapsule";
    }

    public void OnClickCapsule()
    {
        if (isOpened || isAnyCapsuleOpening) return;

        isOpened = true;
        isAnyCapsuleOpening = true;

        if (myButton != null) myButton.interactable = false;

        StartCoroutine(OpenRoutine(true));
    }

    public void ForceOpenCapsule()
    {
        if (isOpened) return;

        isOpened = true;
        if (myButton != null) myButton.interactable = false;

        StartCoroutine(OpenRoutine(false));
    }

    private IEnumerator OpenRoutine(bool manageLock)
    {
        if (smokeEffectPrefab != null)
        {
            Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity, transform.parent);
        }

        float elapsed = 0f;
        Vector3 topStartPos = topHalf.localPosition;
        Vector3 bottomStartPos = bottomHalf.localPosition;

        Vector3 topTargetPos = topStartPos + new Vector3(0, openDistance, 0);
        Vector3 bottomTargetPos = bottomStartPos + new Vector3(0, -openDistance, 0);

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / openDuration;
            float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);

            topHalf.localPosition = Vector3.Lerp(topStartPos, topTargetPos, easeT);
            bottomHalf.localPosition = Vector3.Lerp(bottomStartPos, bottomTargetPos, easeT);

            yield return null;
        }

        topHalf.localPosition = topTargetPos;
        bottomHalf.localPosition = bottomTargetPos;

        float waitBeforeFade = 0.1f;
        float fadeDuration = 0.15f;

        yield return new WaitForSeconds(waitBeforeFade);

        Graphic[] capsuleGraphics = GetComponentsInChildren<Graphic>();
        List<float> initialAlphas = new List<float>();
        foreach (var graphic in capsuleGraphics)
        {
            initialAlphas.Add(graphic.color.a);
        }

        float fadeElapsed = 0f;

        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float fadeRatio = 1f - (fadeElapsed / fadeDuration);

            for (int i = 0; i < capsuleGraphics.Length; i++)
            {
                if (capsuleGraphics[i] != null)
                {
                    Color c = capsuleGraphics[i].color;
                    c.a = initialAlphas[i] * fadeRatio;
                    capsuleGraphics[i].color = c;
                }
            }
            yield return null;
        }

        for (int i = 0; i < capsuleGraphics.Length; i++)
        {
            if (capsuleGraphics[i] != null)
            {
                Color c = capsuleGraphics[i].color;
                c.a = 0f;
                capsuleGraphics[i].color = c;
            }
        }

        InnerItem drawnItem = DrawInnerItem();

        if (drawnItem != null)
        {
            if (drawnItem.itemPrefab != null)
            {
                GameObject spawnedItem = Instantiate(drawnItem.itemPrefab, transform.position, Quaternion.identity, transform.parent);
                spawnedItem.tag = "GachaResultItem";
            }

            if (drawnItem.inventoryUIPrefab != null)
            {
                // ==========================================
                // 💡 修正：isArmor のチェック状態を見て、防具用と武器用の保存先に振り分ける
                // ==========================================
                if (drawnItem.isArmor)
                {
                    gachaResultArmors.Add(drawnItem.inventoryUIPrefab);
                    Debug.Log($"引いた防具UI {drawnItem.inventoryUIPrefab.name} を防具持ち物リストに一時保存しました");
                }
                else
                {
                    WeaponDataKeeper.gachaResultWeapons.Add(drawnItem.inventoryUIPrefab);
                    Debug.Log($"引いた武器UI {drawnItem.inventoryUIPrefab.name} を武器持ち物リストに一時保存しました");
                }
            }
        }

        float remainingTime = destroyDelay - (waitBeforeFade + fadeDuration);
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        if (manageLock)
        {
            isAnyCapsuleOpening = false;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (isOpened && isAnyCapsuleOpening)
        {
            isAnyCapsuleOpening = false;
        }
    }

    public static void ForceResetLock()
    {
        isAnyCapsuleOpening = false;
    }

    private InnerItem DrawInnerItem()
    {
        if (innerItems == null || innerItems.Count == 0) return null;

        float totalWeight = 0;
        foreach (var item in innerItems)
        {
            if (!item.isProbabilityZero)
            {
                totalWeight += item.probabilityWeight;
            }
        }

        if (totalWeight <= 0f) return null;

        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach (var item in innerItems)
        {
            if (item.isProbabilityZero) continue;

            currentWeight += item.probabilityWeight;
            if (randomValue <= currentWeight)
            {
                return item;
            }
        }
        return null;
    }
}