
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("追従対象")]
    [SerializeField]
    private GameObject targetObject;

    [Header("ターゲットシステム")]
    [SerializeField]
    private TargetingSystem targetingSystem;

    [Header("カメラ設定")]
    [SerializeField]
    private float height = 1.5f;

    [SerializeField]
    private float distance = 5.0f;

    [SerializeField]
    private float rotAngle = 0.0f;

    [SerializeField]
    private float heightAngle = 10.0f;

    [SerializeField]
    private float disMin = 5.0f;

    [SerializeField]
    private float disMax = 40.0f;

    [SerializeField]
    private float disMdl = 10.0f;

    [SerializeField]
    private float rotSen = 50.0f;

    [SerializeField]
    private float heiSen = 20.0f;

    [SerializeField]
    private float heiMax = 60.0f;

    [SerializeField]
    private float heiMin = -40.0f;

    [Header("ロックオン設定")]
    [SerializeField]
    private bool isLockOn = false;
    [SerializeField] private float lockOnRotationSmooth = 5f;

    // カメラが追従を開始するまでの角度
    [SerializeField] private float lockOnDeadZone = 12f;

    [Header("Lock On Camera")]
    [SerializeField]
    private float lockOnHeightOffset = 0.5f;

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
        // カーソル表示切り替え
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = !Cursor.visible;

            Cursor.lockState = Cursor.visible
                ? CursorLockMode.None
                : CursorLockMode.Locked;
        }

        // マウス操作
        if (!isLockOn)
        {
            rotAngle -= speed.x * Time.deltaTime * rotSen;
            heightAngle += speed.y * Time.deltaTime * heiSen;
        }
        else
        {
            speed = Vector3.zero;
        }

        heightAngle = Mathf.Clamp(heightAngle, heiMin, heiMax);
        distance = Mathf.Clamp(distance, disMin, disMax);

        // プレイヤー位置
        nowPos = targetObject.transform.position
                 + Vector3.up * height;

        // ロックオン中は敵の方向へカメラを旋回させる
        if (isLockOn && targetingSystem != null)
        {
            Targetable currentLockOnTarget = targetingSystem.CurrentTarget;

            if (currentLockOnTarget != null && currentLockOnTarget.TargetPoint != null)
            {
                Vector3 toEnemy =
                    currentLockOnTarget.TargetPoint.position -
                    targetObject.transform.position;

                toEnemy.y = 0f;

                if (toEnemy.sqrMagnitude > 0.001f)
                {
                    float targetAngle = Mathf.Atan2(
                        -toEnemy.x,
                        toEnemy.z
                    ) * Mathf.Rad2Deg;

                    float angleDifference = Mathf.DeltaAngle(
                        rotAngle,
                        targetAngle
                    );

                    // 一定角度以上ずれた場合だけ追従
                    if (Mathf.Abs(angleDifference) > lockOnDeadZone)
                    {
                        float excessAngle =
                            angleDifference -
                            Mathf.Sign(angleDifference) * lockOnDeadZone;

                        float smooth = 1f - Mathf.Exp(
                            -lockOnRotationSmooth * Time.deltaTime
                        );

                        rotAngle = Mathf.LerpAngle(
                            rotAngle,
                            rotAngle + excessAngle,
                            smooth
                        );
                    }
                }
            }
        }

        // カメラ位置の計算に反映
        nowHeightAngle = heightAngle;
        nowRotAngle = rotAngle;

        // 通常時の距離調整
        if (heightAngle > 30)
        {
            distance = Mathf.Lerp(
                distance,
                disMdl * heightAngle / 30f,
                Time.deltaTime
            );
        }
        else if (heightAngle >= -3)
        {
            distance = Mathf.Lerp(
                distance,
                disMdl,
                Time.deltaTime
            );
        }
        else
        {
            distance = Mathf.Lerp(
                distance,
                disMin,
                Time.deltaTime
            );
        }

        // ロックオン対象取得
        Targetable currentTarget = null;

        if (isLockOn && targetingSystem != null)
            currentTarget = targetingSystem.CurrentTarget;

        if (currentTarget == null)
            isLockOn = false;

        // カメラ位置は常にプレイヤー基準
        transform.position = CalculateCameraPosition(distance);

        if (currentTarget != null)
        {
            // 敵の注視位置
            Vector3 enemyPosition =
                currentTarget.TargetPoint.position
                + Vector3.up * lockOnHeightOffset;

            Vector3 direction = enemyPosition - transform.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(direction.normalized);

                float t = 1f - Mathf.Exp(
                    -lockOnRotationSmooth * Time.deltaTime
                );

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    t
                );
            }
        }
        else
        {
            // 通常時はプレイヤーを見る
            Vector3 direction = nowPos - transform.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(direction.normalized);
            }
        }
    }

    // カメラ操作入力
    public void OnCamera(InputValue input)
    {
        if (isLockOn)
        {
            speed = Vector3.zero;
            return;
        }

        Vector2 move = input.Get<Vector2>();
        speed = new Vector3(move.x, move.y, 0);
    }

    // ターゲットオン・オフ

    public void OnTargetLock(InputValue input)
    {
        Debug.Log("① OnTargetLock 呼び出し");

        float inputValue = input.Get<float>();
        Debug.Log($"② InputValue : {inputValue}");

        if (inputValue <= 0)
        {
            Debug.Log("③ 入力値が0のため終了");
            return;
        }

        if (targetingSystem == null)
        {
            Debug.LogError("④ TargetingSystemが設定されていません");
            return;
        }

        Debug.Log("⑤ TargetingSystem確認OK");

        Debug.Log(
            $"⑥ ロックオン状態 : {isLockOn}, " +
            $"現在のターゲット : {targetingSystem.CurrentTarget}"
        );

        // ロックオン解除
        if (isLockOn)
        {
            Debug.Log("⑦ LockOn解除");
            isLockOn = false;
            return;
        }

        // ターゲットが存在しない場合は取得
        if (targetingSystem.CurrentTarget == null)
        {
            Debug.Log("⑧ ターゲットが存在しないため取得開始");

            targetingSystem.SelectNextTarget();

            Debug.Log(
                $"⑨ 取得後のターゲット : {targetingSystem.CurrentTarget}"
            );
        }

        // ターゲットが存在すればロックオン
        if (targetingSystem.CurrentTarget != null)
        {
            Debug.Log("⑩ LockOn");
            isLockOn = true;
        }
        else
        {
            Debug.LogWarning("⑪ ターゲットを取得できませんでした");
        }
    }

    // ターゲット変更
    public void OnTargetChange(InputValue input)
    {
        if (input.Get<float>() <= 0)
            return;

        if (targetingSystem == null)
            return;

        // 次のターゲットへ切り替え
        targetingSystem.SelectNextTarget();

        // ターゲットが存在すればロックオン
        if (targetingSystem.CurrentTarget != null)
        {
            isLockOn = true;
        }
        else
        {
            isLockOn = false;
        }
    }


    private Vector3 CalculateCameraPosition(float cameraDistance)
    {
        float deg = Mathf.Deg2Rad;

        float cx = Mathf.Sin(nowRotAngle * deg)
                 * Mathf.Cos(nowHeightAngle * deg)
                 * cameraDistance;

        float cz = -Mathf.Cos(nowRotAngle * deg)
                  * Mathf.Cos(nowHeightAngle * deg)
                  * cameraDistance;

        float cy = Mathf.Sin(nowHeightAngle * deg)
                 * cameraDistance;

        return nowPos + new Vector3(cx, cy, cz);
    }


}
