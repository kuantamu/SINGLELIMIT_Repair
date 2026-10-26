using UnityEngine;

/// <summary>
/// 弾をまっすぐ飛ばすだけの簡単な移動スクリプト。
/// HitBox と一緒に弾のプレハブに付けて使います。
/// (誘導弾など凝った動きにしたいときは、これを置き換えてください)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ProjectileMover : MonoBehaviour
{
    [Header("飛ぶ速さ(メートル/秒)")]
    [SerializeField] private float speed = 20f;

    [Header("飛ぶ向き(自分のローカル座標。前方=0,0,1)")]
    [SerializeField] private Vector3 localDirection = Vector3.forward;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>速さを後から変える</summary>
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    private void FixedUpdate()
    {
        // 向いている方向へ、物理の更新ごとに進める
        Vector3 move = transform.TransformDirection(localDirection.normalized) * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }
}
