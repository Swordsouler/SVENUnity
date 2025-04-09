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

        private float xRotation = 0f;

        private bool isGrounded = true;
        public float jumpForce = 5f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            xRotation = pointOfView.cameraComponent.transform.localRotation.eulerAngles.x;

            if (xRotation > 180f) xRotation -= 360f;
        }

        private void Update()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 250 * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 250 * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            pointOfView.cameraComponent.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void FixedUpdate()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 forward = pointOfView.cameraComponent.transform.forward;
            Vector3 right = pointOfView.cameraComponent.transform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;

            _rb.AddForce(moveDirection * moveSpeed, ForceMode.Force);

            if (Input.GetButton("Jump") && isGrounded)
            {
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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