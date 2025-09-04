using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float crouchSpeedMultiplier = 0.5f; // move slower when crouching
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float acceleration = 10f;
    public float deceleration = 10f;

    [Header("Camera Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 100f;
    public float crouchCameraOffset = 0.5f; // camera lowers when crouching

    private float xRotation = 0f;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpInput;
    private bool crouchInput;

    private Vector3 velocity;
    private Vector3 smoothMovement = Vector3.zero;

    private bool isGrounded;
    private bool isCrouching = false;

    private Input inputActions;
    private CharacterController controller;

    private float originalHeight;
    private Vector3 originalCameraPos;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new Input();

        inputActions.Player.Enable();

        // Move
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Look
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        // Jump
        inputActions.Player.Jump.performed += ctx => jumpInput = true;

        // Crouch
        inputActions.Player.Crouch.performed += ctx => StartCrouch();
        inputActions.Player.Crouch.canceled += ctx => StopCrouch();

        Cursor.lockState = CursorLockMode.Locked;

        // Save original values
        originalHeight = controller.height;
        originalCameraPos = playerCamera.localPosition;
    }

    void OnDestroy()
    {
        inputActions.Player.Move.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled -= ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed -= ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled -= ctx => lookInput = Vector2.zero;

        inputActions.Player.Jump.performed -= ctx => jumpInput = true;

        inputActions.Player.Crouch.performed -= ctx => StartCrouch();
        inputActions.Player.Crouch.canceled -= ctx => StopCrouch();
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Target movement
        Vector3 targetMovement = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Apply crouch speed
        float currentSpeed = speed * (isCrouching ? crouchSpeedMultiplier : 1f);
        targetMovement *= currentSpeed;

        // Smooth movement
        smoothMovement = Vector3.Lerp(smoothMovement, targetMovement, (moveInput.magnitude > 0 ? acceleration : deceleration) * Time.deltaTime);
        controller.Move(smoothMovement * Time.deltaTime);

        // Jump
        if (jumpInput && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpInput = false;
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void StartCrouch()
    {
        if (!isCrouching)
        {
            controller.height = originalHeight / 2f;
            playerCamera.localPosition = originalCameraPos - new Vector3(0, crouchCameraOffset, 0);
            isCrouching = true;
        }
    }

    void StopCrouch()
    {
        if (isCrouching && CanUncrouch())
        {
            controller.height = originalHeight;
            playerCamera.localPosition = originalCameraPos;
            isCrouching = false;
        }
    }

    // Check if there's enough space above to uncrouch
    bool CanUncrouch()
    {
        float radius = controller.radius;
        Vector3 start = transform.position + Vector3.up * controller.height / 2f;
        float distance = originalHeight - controller.height;
        // Cast a capsule upward to check for obstacles
        return !Physics.SphereCast(start, radius, Vector3.up, out _, distance);
    }
}
