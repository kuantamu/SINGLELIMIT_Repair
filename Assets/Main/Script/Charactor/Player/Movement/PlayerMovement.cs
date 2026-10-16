using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Camera mainCam;
    private Vector3 moveForward;

    [SerializeField,Tooltip("移動速度")]
    private float moveSpeed;
    [SerializeField,Tooltip("停止率")]
    private float stopRate = 0.9f;
    [SerializeField,Tooltip("走行速度倍率")]
    private float ranSpeedRate = 2.0f;

    [SerializeField,Tooltip("回転時間率")]
    private float turnTimeRate = 0.5f;

    [SerializeField,Tooltip("空中力")]
    private float AirForce = 5f;

    [SerializeField,Tooltip("最大水平速度")]
    float maxHorizontalSpeed = 5f;

    private bool isRun;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 9f;

    [Header("Gravity")]
    [SerializeField] private float riseGravity = 25f;
    [SerializeField] private float fallGravity = 50f;

    

    [SerializeField]
    private float atkHopPower = 300f;

    [SerializeField] 
    private float jumpCooldown = 0.3f;

    [Header("Dodge")]
    [SerializeField] 
    private float dodgeDistance = 4f;

    private Coroutine dodgeCoroutine;


    void Start()
    {
        mainCam = Camera.main;
        rb = this.gameObject.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void Move(Vector3 move)//地上移動処理
    {
        Vector3 cameraForward = Vector3.Scale(
            mainCam.transform.forward,
            new Vector3(1, 0, 1)
        ).normalized;

        moveForward = (cameraForward * move.z
                    + mainCam.transform.right * move.x).normalized;

        float speed = moveSpeed;

        if(isRun) speed *= ranSpeedRate;

        if (move.magnitude > 0.1f)
        {
            rb.linearVelocity = moveForward * speed
                        + new Vector3(0, rb.linearVelocity.y, 0);
        }
        else
        {
            Vector2 horizontalVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z);
            horizontalVelocity = horizontalVelocity * stopRate;
            if (horizontalVelocity.magnitude < 0.1f)
            {
                horizontalVelocity = Vector2.zero;
            }
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.y);
        }
        Rotation(move);
    }
    public void AirMove(Vector3 move)//空中移動処理
    {
        Vector3 cameraForward = Vector3.Scale(
            mainCam.transform.forward,
            new Vector3(1, 0, 1)
        ).normalized;

        moveForward = cameraForward * move.z
                    + mainCam.transform.right * move.x;

        moveForward = moveForward.normalized;

        if (move.magnitude > 0)
        {
            rb.AddForce(moveForward * AirForce * move.magnitude, ForceMode.Force);
        }
        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);

        if (horizontal.magnitude > maxHorizontalSpeed)
        {
            horizontal = horizontal.normalized * maxHorizontalSpeed;
            rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
        }
        //Rotation(move);
    }
    public void CanDush()
    {
        isRun = true;
    }
    public void DushEnd()
    {
        isRun = false;
    }
    private void Rotation(Vector3 move) //回転処理
    {
        Vector3 cameraForward = Vector3.Scale(
            mainCam.transform.forward,
            new Vector3(1, 0, 1)
        ).normalized;

        moveForward = cameraForward * move.z
                    + mainCam.transform.right * move.x; 

        moveForward = moveForward.normalized;

        if (move.magnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveForward);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * turnTimeRate
            );
        }
        else // �������͂��Ă��Ȃ��Ƃ�
        {
            //Quaternion targetRotation = transform.rotation;
            //transform.rotation = targetRotation;
        }
    }
    public void RotationCameraFoward()
    {
        Vector3 cameraForward = Vector3.Scale(
            mainCam.transform.forward,
            new Vector3(1, 0, 1)
        ).normalized;
        moveForward = cameraForward * 1
                    + mainCam.transform.right * 0; 
        moveForward = moveForward.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(moveForward);
        transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 180
            );
    }



    public void Jump()
    {
        Vector3 velocity = rb.linearVelocity;

        // 現在のY速度をリセット
        velocity.y = jumpSpeed;

        rb.linearVelocity = velocity;
    }

    public void ApplyGravity()
    {
        Vector3 velocity = rb.linearVelocity;

        // 上昇中
        if (velocity.y > 0f)
        {
            velocity.y -= riseGravity * Time.fixedDeltaTime;
        }
        // 下降中
        else
        {
            velocity.y -= fallGravity * Time.fixedDeltaTime;
        }

        rb.linearVelocity = velocity;
    }
    public void AttackHop()
    {
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(transform.up * atkHopPower, ForceMode.Impulse);
    }

    public void StartDodge(Vector3 input, float duration)
    {
        Debug.Log($"StartDodge called with input: {input}, duration: {duration}");
        if (input.sqrMagnitude < 0.01f) return;

        Vector3 direction = GetCameraRelativeDirection(input);
        if (direction.sqrMagnitude < 0.001f) return;

        direction.Normalize();

        if (dodgeCoroutine != null)
        {
            return; // すでにドッジ中の場合は無視する
        }

        dodgeCoroutine = StartCoroutine(DodgeRoutine(direction, duration));
    }

    private Vector3 GetCameraRelativeDirection(Vector3 input)
    {
        Vector3 forward;
        Vector3 right;

        if (mainCam != null)
        {
            forward = Vector3.ProjectOnPlane(mainCam.transform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(mainCam.transform.right, Vector3.up).normalized;
        }
        else
        {
            forward = Vector3.forward;
            right = Vector3.right;
        }

        Vector3 moveDir = right * input.x + forward * input.z;
        return moveDir;
    }

    private IEnumerator DodgeRoutine(Vector3 direction, float duration)
    {
        Debug.Log($"DodgeRoutine started with direction: {direction}, duration: {duration}");
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 最初が最大、最後が0に近づく減速カーブ
            // speed = 2D(1-t) / T
            float speed = (2f * dodgeDistance * (1f - t)) / duration;

            Vector3 horizontalVelocity = direction * speed;

            // Yは重力などをそのまま残す
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);

            yield return new WaitForFixedUpdate();
        }

        // 横移動だけ止める
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        dodgeCoroutine = null;
    }
}
