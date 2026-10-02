using UnityEngine;

public class OpenAllCapsulesButton : MonoBehaviour
{
    // 一括開封ボタンが押された時に呼ばれるメソッド
    public void OnClickOpenAll()
    {
        // 画面に存在する「GachaCapsule」タグがついたオブジェクトを全て探す
        GameObject[] capsules = GameObject.FindGameObjectsWithTag("GachaCapsule");

        // 見つかったカプセルすべてに対して処理を行う
        foreach (GameObject cap in capsules)
        {
            // カプセルに付いている CapsuleOpener スクリプトを取得
            CapsuleOpener opener = cap.GetComponent<CapsuleOpener>();

            if (opener != null)
            {
                // 全カプセルを強制的に開くメソッドを呼び出す
                opener.ForceOpenCapsule();
            }
        }
    }
}