using UnityEngine;
using UnityEngine.EventSystems;

namespace Sven.Utils
{
    /// <summary>
    /// ManipulableObject class to control the object.
    /// </summary>
    [DisallowMultipleComponent]
    public class ManipulableObject : MonoBehaviour
    {
        private Vector3 offset;
        private float zCoord;
        private bool isDragging = false;

        void Update()
        {
            // Ignore input if the pointer is over a UI element
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsMouseOverObject())
                {
                    isDragging = true;
                    zCoord = Camera.main.WorldToScreenPoint(gameObject.transform.position).z;
                    offset = gameObject.transform.position - GetMouseWorldPos();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                transform.position = GetMouseWorldPos() + offset;
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (IsMouseOverObject())
                {
                    isDragging = true;
                }
            }

            if (Input.GetMouseButtonUp(1))
            {
                isDragging = false;
            }

            if (isDragging && Input.GetMouseButton(1))
            {
                float rotationSpeed = 10.0f;
                float h = rotationSpeed * Input.GetAxis("Mouse X");
                float v = rotationSpeed * Input.GetAxis("Mouse Y");
                transform.Rotate(Vector3.up, -h, Space.World);
                transform.Rotate(Vector3.right, v, Space.World);
            }

            if (IsMouseOverObject())
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0.0f)
                {
                    float scaleSpeed = 1f;
                    transform.localScale += Vector3.one * scroll * scaleSpeed;
                }
            }

            if (GetComponent<Renderer>() != null && IsMouseOverObject())
            {
                if (Input.GetKeyDown(KeyCode.Keypad1)) ChangeColor(Color.red);
                if (Input.GetKeyDown(KeyCode.Keypad2)) ChangeColor(Color.green);
                if (Input.GetKeyDown(KeyCode.Keypad3)) ChangeColor(Color.blue);
                if (Input.GetKeyDown(KeyCode.Keypad4)) ChangeColor(Color.yellow);
                if (Input.GetKeyDown(KeyCode.Keypad5)) ChangeColor(Color.cyan);
                if (Input.GetKeyDown(KeyCode.Keypad6)) ChangeColor(Color.magenta);
                if (Input.GetKeyDown(KeyCode.Keypad7)) ChangeColor(Color.white);
                if (Input.GetKeyDown(KeyCode.Keypad8)) ChangeColor(Color.black);
                if (Input.GetKeyDown(KeyCode.Keypad9)) ChangeColor(Color.gray);
            }
        }

        private void ChangeColor(Color color)
        {
            GetComponent<Renderer>().material.color = color;
        }

        private Vector3 GetMouseWorldPos()
        {
            Vector3 mousePoint = Input.mousePosition;
            mousePoint.z = zCoord;
            return Camera.main.ScreenToWorldPoint(mousePoint);
        }

        private bool IsMouseOverObject()
        {
            if (Camera.main == null || Input.mousePosition == null) return false;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.collider != null && hit.collider.gameObject == gameObject;
            }
            return false;
        }
    }
}