using UnityEngine;

public class CameraFollowBehaviour : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 2, -10);
    public float smoothTime = 0.25f;

    Vector3 currentVelocity;

    private void Start()
    {
        transform.position = new Vector3(GameManager.Instance.Player.transform.position.x, transform.position.y, transform.position.z);
    }

    private void LateUpdate()
    {
        Vector3 targetPosition = Vector3.SmoothDamp(
            transform.position,
            GameManager.Instance.Player.transform.position + offset,
            ref currentVelocity,
            smoothTime
            );

        transform.position = new Vector3(targetPosition.x, transform.position.y, transform.position.z);
    }
}
