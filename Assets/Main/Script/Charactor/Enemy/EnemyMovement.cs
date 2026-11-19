
using UnityEngine;

public class EnemyMovement
{
    private readonly Rigidbody rb;
    private readonly Transform body;

    private readonly float moveSpeed;
    private readonly float strafeSpeed;
    private readonly float rotationSpeed;

    public EnemyMovement(
        Rigidbody rb,
        Transform body,
        float moveSpeed,
        float strafeSpeed,
        float rotationSpeed)
    {
        this.rb = rb;
        this.body = body;
        this.moveSpeed = moveSpeed;
        this.strafeSpeed = strafeSpeed;
        this.rotationSpeed = rotationSpeed;
    }

    // 水平方向への移動
    public void Move(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        Vector3 velocity = direction * moveSpeed;

        rb.linearVelocity = new Vector3(
            velocity.x,
            rb.linearVelocity.y,
            velocity.z
        );
    }
    public void StrafeMove(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        Vector3 velocity = direction * strafeSpeed;

        rb.linearVelocity = new Vector3(
            velocity.x,
            rb.linearVelocity.y,
            velocity.z
        );
    }

    // 移動停止
    public void Stop()
    {
        rb.linearVelocity = new Vector3(
            0f,
            rb.linearVelocity.y,
            0f
        );
    }

    // 対象の方向を向く
    // 対象の方向を向く
    public void LookAt(Vector3 targetPosition, float deltaTime)
    {
        Vector3 direction = targetPosition - rb.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Quaternion nextRotation = Quaternion.RotateTowards(
            rb.rotation,
            targetRotation,
            rotationSpeed * deltaTime
        );

        rb.MoveRotation(nextRotation);
    }
}
