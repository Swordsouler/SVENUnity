using Sven.Context;
using UnityEngine;

namespace Sven.Demo
{
    [RequireComponent(typeof(Rigidbody))]
    public class DemoCharacterController : User
    {
        [Range(1, 10)]
        public float mouseSensitivity = 5;
        public float moveSpeed = 5f;
        private Rigidbody _rb;

        private float yRotation = 0F;

        private bool isGrounded = true;
        public float jumpForce = 5f;

        private float horizontalInput;
        private float verticalInput;
        private bool jumpInput;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            pointOfView.cameraComponent.transform.SetParent(transform, false);
            pointOfView.cameraComponent.transform.localPosition = new Vector3(0, 0.5f, 0);
        }

        private void Update()
        {
            float xRotation = pointOfView.cameraComponent.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;

            yRotation += Input.GetAxis("Mouse Y") * mouseSensitivity;
            yRotation = Mathf.Clamp(yRotation, -90f, 90f);

            pointOfView.cameraComponent.transform.localEulerAngles = new Vector3(-yRotation, xRotation, 0);

            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            jumpInput = Input.GetButton("Jump");
        }

        private void FixedUpdate()
        {
            Vector3 forward = pointOfView.cameraComponent.transform.forward;
            Vector3 right = pointOfView.cameraComponent.transform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * verticalInput + right * horizontalInput).normalized;

            Vector3 targetVelocity = moveDirection * moveSpeed;
            targetVelocity.y = _rb.linearVelocity.y;
            _rb.linearVelocity = targetVelocity;

            if (jumpInput && isGrounded)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpForce, _rb.linearVelocity.z);
                isGrounded = false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
            }
        }
    }
}