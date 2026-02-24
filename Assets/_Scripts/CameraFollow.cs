using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    public float distance = 6f;
    public float height = 2f;
    public float mouseSensitivity = 3f;
    public float minY = -30f;
    public float maxY = 60f;

    float yaw;
    float pitch = 20f;

    void Start()
    {
    }

    void LateUpdate()
    {
        if (!target) return;

        // Only rotate camera while gameplay cursor lock is active.
        if (Cursor.lockState == CursorLockMode.Locked && !Cursor.visible)
        {
            float lookDt = Time.unscaledDeltaTime;
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity * 100f * lookDt;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * lookDt;
            pitch = Mathf.Clamp(pitch, minY, maxY);
        }

        // Rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        // Position
        Vector3 offset = rotation * new Vector3(0, 0, -distance);
        Vector3 desiredPosition = target.position + Vector3.up * height + offset;

        transform.position = desiredPosition;
        transform.LookAt(target.position + Vector3.up * height);
    }
}
