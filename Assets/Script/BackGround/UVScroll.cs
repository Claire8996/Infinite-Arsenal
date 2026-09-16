using UnityEngine;
using UnityEngine.UI; // RawImageを使うために必要

public class UVScroll : MonoBehaviour
{
    [Tooltip("スクロールさせたいRawImage")]
    public RawImage targetRawImage;

    [Tooltip("X軸（横）のスクロールスピード")]
    public float scrollSpeedX = 0.05f;

    [Tooltip("Y軸（縦）のスクロールスピード")]
    public float scrollSpeedY = 0.05f;

    void Update()
    {
        if (targetRawImage != null)
        {
            // 現在の画像の表示位置（UV）を取得
            Rect uvRect = targetRawImage.uvRect;

            // スピードに合わせて位置を少しずつズラす
            uvRect.x += scrollSpeedX * Time.deltaTime;
            uvRect.y += scrollSpeedY * Time.deltaTime;

            // ズラした位置を元に戻す（これでアニメーションする！）
            targetRawImage.uvRect = uvRect;
        }
    }
}