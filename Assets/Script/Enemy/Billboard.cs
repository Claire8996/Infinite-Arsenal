using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // メインカメラを自動で見つける
        mainCamera = Camera.main;
    }

    // UpdateではなくLateUpdateを使うことで、カメラが動いた「後」に確実に向きを合わせる
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // UIが裏返らないように、カメラと同じ方向を向かせる
            transform.LookAt(transform.position + mainCamera.transform.forward);
        }
    }
}