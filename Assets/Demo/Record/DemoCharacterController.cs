using System.Collections.Generic;
using Sven.Content;
using Sven.Context;
using UnityEngine;

namespace Sven.Demo
{
    [RequireComponent(typeof(Rigidbody))]
    public class DemoCharacterController : User
    {
        public bool lockMouse = false;

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

        private Material _baseMaterial, _focusMaterial;

        private List<GameObject> _focusObjects = new();
        private List<TextMesh> _textMeshes = new();

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;
        }

        private void Start()
        {
            if (lockMouse) Cursor.lockState = CursorLockMode.Locked;

            pointOfView.cameraComponent.transform.SetParent(transform, false);
            pointOfView.cameraComponent.transform.localPosition = new Vector3(0, 0.5f, 0);

            _baseMaterial = Resources.Load<Material>("Materials/Base");
            _focusMaterial = Resources.Load<Material>("Materials/Focus");
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

            // enter
            List<GameObject> newFocusObjects = new();
            foreach (Pointer pointer in pointers)
            {
                foreach (SemantizationCore semanticObject in pointer.currentInteractedObjects)
                {
                    if (semanticObject.gameObject != heldObject) newFocusObjects.Add(semanticObject.gameObject);
                    break;
                }
            }

            // enter if the object is not already in the list
            foreach (GameObject obj in newFocusObjects)
            {
                if (!_focusObjects.Contains(obj))
                {
                    _focusObjects.Add(obj);
                    obj.GetComponent<Renderer>().material = _focusMaterial;

                    TextMesh textMesh = obj.GetComponentInChildren<TextMesh>();
                    if (textMesh == null)
                    {
                        GameObject textObject = new("PickupText");
                        textObject.transform.SetParent(obj.transform);
                        textObject.transform.localPosition = new Vector3(0, 0.5f, 0);
                        textObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

                        textMesh = textObject.AddComponent<TextMesh>();
                        textMesh.anchor = TextAnchor.MiddleCenter;
                        textMesh.text = "F to pickup";
                        textMesh.fontSize = 100;
                        textMesh.color = Color.white;
                        textMesh.alignment = TextAlignment.Center;
                        _textMeshes.Add(textMesh);
                    }
                }
            }

            // exit if the object is not in the new list
            foreach (GameObject obj in _focusObjects)
            {
                if (!newFocusObjects.Contains(obj))
                {
                    obj.GetComponent<Renderer>().material = _baseMaterial;

                    TextMesh textMesh = obj.GetComponentInChildren<TextMesh>();
                    if (textMesh != null)
                    {
                        _textMeshes.Remove(textMesh);
                        Destroy(textMesh.gameObject);
                    }

                    _focusObjects.Remove(obj);
                    break;
                }
            }

            // rotate text meshes to face the camera
            foreach (TextMesh textMesh in _textMeshes)
            {
                if (textMesh != null)
                {
                    textMesh.transform.rotation = Quaternion.LookRotation(
                        textMesh.transform.position - pointOfView.cameraComponent.transform.position
                    );
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
            foreach (GameObject obj in _focusObjects)
            {
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