using UnityEngine;

public class FaceCameraOnSpawn : MonoBehaviour
{
    Camera cam;

    private void Start()
    {
        cam = Camera.main;

        if (cam == null) return;

        // カメラの方向を向く
        transform.LookAt(
            transform.position + cam.transform.rotation * Vector3.forward,
            cam.transform.rotation * Vector3.up
        );
    }

    private void FixedUpdate()
    {
        if (cam == null) {
            cam = Camera.main;
            return;
        }

        transform.LookAt(
            transform.position + cam.transform.rotation * Vector3.forward,
            cam.transform.rotation * Vector3.up
        );
    }
}
