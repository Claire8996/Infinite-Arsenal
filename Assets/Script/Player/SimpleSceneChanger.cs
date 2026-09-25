using UnityEngine;
using UnityEngine.SceneManagement; // シーン移動に必須の機能

public class SimpleSceneChanger : MonoBehaviour
{
    // ===============================================
    // ボタンの On Click () から呼び出して使うメソッド
    // （移動したいシーンの名前をInspectorから入力できるようにしています）
    // ===============================================
    public void LoadSceneByName(string sceneName)
    {
        // 念のため、シーン名が空っぽでないかチェック
        if (!string.IsNullOrEmpty(sceneName))
        {
            // 指定された名前のシーンをロードする
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("移動先のシーン名が入力されていません！");
        }
    }
}