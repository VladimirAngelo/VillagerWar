using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleThirdPersonController : MonoBehaviour
{
    public Transform cameraRig;
    public float moveSpeed = 6f;
    public float gravity = -20f;
    public float turnSpeed = 12f;

    CharacterController cc;
    float yVel;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        // WASD
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v);
        input = Vector3.ClampMagnitude(input, 1f);

        // move relative to camera
        Vector3 forward = cameraRig ? cameraRig.forward : Vector3.forward;
        Vector3 right   = cameraRig ? cameraRig.right   : Vector3.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();

        Vector3 move = (forward * input.z + right * input.x);

        // rotate toward move
        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // gravity
        if (cc.isGrounded && yVel < 0) yVel = -2f;
        yVel += gravity * Time.deltaTime;

        Vector3 vel = move * moveSpeed;
        vel.y = yVel;

        cc.Move(vel * Time.deltaTime);
    }
}