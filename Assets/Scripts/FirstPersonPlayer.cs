using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonPlayer : MonoBehaviour
{
    private CharacterController controller;
    public float moveSpeed = 4;
    public float mouseSensitivity = 0.1f;
    public Transform playerCamera;
    private float verticalSpeed;
    private float cameraPitch = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        gameObject.AddComponent<MusicRoom>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        Vector3 movement = Vector3.zero;

        transform.Rotate(Vector3.up, mouseDelta.x * mouseSensitivity);

        // Keep the player upright and tilt only the camera.
        cameraPitch -= mouseDelta.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        
        if (Keyboard.current.wKey.isPressed)
        {
            movement += transform.forward;
        }
        if (Keyboard.current.sKey.isPressed)
        {
            movement -= transform.forward;
        }
        if (Keyboard.current.aKey.isPressed)
        {
            movement -= transform.right;
        }
        if (Keyboard.current.dKey.isPressed)
        {
            movement += transform.right;
        }
        movement = Vector3.ClampMagnitude(movement, 1f);
        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += Physics.gravity.y * Time.deltaTime;
        controller.Move((movement * moveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);
    }
}
