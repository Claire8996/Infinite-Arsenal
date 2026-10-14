using UnityEngine;

public class WeaponSizeAdjuster : MonoBehaviour
{
    [Header("【Prefabのサイズ設定(Width / Height)】")]
    [Tooltip("横幅 (Width) または 横のスケール (X)")]
    public float customWidth = 150f;

    [Tooltip("高さ (Height) または 縦のスケール (Y)")]
    public float customHeight = 150f;

    // 💡 修正：勝手にサイズが変わらないように Start と OnValidate を削除しました

    // 他のスクリプトから呼ばれた時だけ、この処理が走るようにします
    public void ApplySize()
    {
        // もしこのPrefabがUI（RectTransformを持っている）だった場合
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // sizeDelta が Width と Height に当たります
            rectTransform.sizeDelta = new Vector2(customWidth, customHeight);
        }
        else
        {
            // もしこのPrefabが3Dオブジェクト（Transformのみ）だった場合
            // WidthをX、HeightをYのスケール（大きさ）として適用します
            transform.localScale = new Vector3(customWidth, customHeight, transform.localScale.z);
        }
    }
}