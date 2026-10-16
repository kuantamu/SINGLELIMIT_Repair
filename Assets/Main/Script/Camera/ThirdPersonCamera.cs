using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField]
    private GameObject targetObject;
    [SerializeField, Tooltip("カメラの高さオフセット")]
    private float height = 1.5f;
    [SerializeField, Tooltip("カメラとのオフセット")]
    private float distance = 5.0f;
    [SerializeField, Tooltip("縦のカメラ角度")]
    private float rotAngle = 0.0f;
    [SerializeField, Tooltip("横のカメラ角度")]
    private float heightAngle = 10.0f;
    [SerializeField, Tooltip("見上げた時の距離")]
    private float disMin = 5.0f;
    [SerializeField, Tooltip("横感度")]
    private float disMax = 40.0f;
    [SerializeField, Tooltip("通常の距離")]
    private float disMdl = 10.0f;
    [SerializeField, Tooltip("横感度")]
    private float rotSen = 50.0f;
    [SerializeField, Tooltip("縦感度")]
    private float heiSen = 20.0f;
    [SerializeField, Tooltip("カメラ縦角度上限")]
    private float heiMax = 60.0f;
    [SerializeField, Tooltip("カメラ縦角度下限")]
    private float heiMin = -40.0f;
    private Vector3 nowPos;
    private float nowRotAngle;
    private float nowHeightAngle;

    private Vector3 speed = Vector3.zero;


    private void Start()
    {
        nowPos = targetObject.transform.position;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }



    private void LateUpdate()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = !Cursor.visible;
            if (Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        rotAngle -= speed.x * Time.deltaTime * rotSen;
        heightAngle += speed.y * Time.deltaTime * heiSen;
        heightAngle = Mathf.Clamp(heightAngle, heiMin, heiMax);
        distance = Mathf.Clamp(distance, disMin, disMax);

        nowPos = targetObject.transform.position + Vector3.up * height;
        nowHeightAngle = heightAngle;
        nowRotAngle = rotAngle;

        if (heightAngle > 30)
        {
            distance = Mathf.Lerp(distance, disMdl * heightAngle / 30.0f, Time.deltaTime);
        }
        else if (heightAngle <= 30 && heightAngle >= -3)
        {
            distance = Mathf.Lerp(distance, disMdl, Time.deltaTime);
        }
        else if (heightAngle < -3)
        {
            distance = Mathf.Lerp(distance, disMin, Time.deltaTime);
        }

        var deg = Mathf.Deg2Rad;
        var cx = Mathf.Sin(nowRotAngle * deg) * Mathf.Cos(nowHeightAngle * deg) * distance;
        var cz = -Mathf.Cos(nowRotAngle * deg) * Mathf.Cos(nowHeightAngle * deg) * distance;
        var cy = Mathf.Sin(nowHeightAngle * deg) * distance;
        transform.position = nowPos + new Vector3(cx, cy, cz);

        var rot = Quaternion.LookRotation((nowPos - transform.position).normalized);
        transform.rotation = rot;

    }

    public void OnCamera(InputValue input)
    {
        Vector2 move = input.Get<Vector2>();
        speed = new Vector3(move.x, move.y, 0);
    }
}
