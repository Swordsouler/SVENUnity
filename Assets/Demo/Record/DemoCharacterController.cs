using Sven.Content;
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

        public Transform holderTransform;
        private GameObject heldObject;
        public float pickupRange = 2f;

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

            if (Input.GetKeyDown(KeyCode.F))
            {
                if (heldObject == null)
                {
                    TryPickupObject();
                }
                else
                {
                    DropObject();
                }
            }
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

        private void TryPickupObject()
        {
            foreach (Pointer pointer in pointers)
            {
                foreach (SemantizationCore semanticObject in pointer.currentInteractedObjects)
                {
                    GameObject obj = semanticObject.gameObject;
                    if (obj.CompareTag("Pickup"))
                    {
                        heldObject = obj;
                        heldObject.GetComponent<Rigidbody>().isKinematic = true;
                        heldObject.transform.SetParent(holderTransform);
                        heldObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                        return;
                    }
                }
            }
        }

        private void DropObject()
        {
            if (heldObject != null)
            {
                heldObject.GetComponent<Rigidbody>().isKinematic = false;
                heldObject.transform.SetParent(null);
                heldObject = null;
            }
        }
    }
}