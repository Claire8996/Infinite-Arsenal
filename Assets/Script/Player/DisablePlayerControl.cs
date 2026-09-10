using UnityEngine;

public class DisablePlayerControl : MonoBehaviour
{
    void Start()
    {
        // 1. "Player"というタグがついているオブジェクトを探す
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // 2. PlayerMovement（移動）スクリプトをオフにする
            PlayerMovement moveScript = player.GetComponent<PlayerMovement>();
            if (moveScript != null)
            {
                moveScript.enabled = false;
            }

            // 3. MouseLook（視点移動）スクリプトをオフにする
            // カメラはPlayerの子オブジェクトなので GetComponentInChildren を使います
            MouseLook lookScript = player.GetComponentInChildren<MouseLook>();
            if (lookScript != null)
            {
                lookScript.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning("Playerタグがついたオブジェクトが見つかりませんでした。");
        }

        // 4. UI（ボタンなど）をクリックできるように、マウスカーソルのロックを解除して表示する
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}