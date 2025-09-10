using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if HAVE_INPUTSYSTEM
using UnityEngine.InputSystem; // New Input System
#endif

namespace Polyperfect.Animals
{
    public class Polyperfect_CameraController : MonoBehaviour
    {
        public Transform target, podium;
        public float panSpeed;
        public float zoomSpeed;
        public Vector2 ZoomDistanceMinMax;
        Vector2 clickedPosition, mousePos;
        public float threshold = 0.2f;
        public float zoomAmount = 0;

        Camera mainCam;
        bool canControl, pressedLastFrame;

        // Use this for initialization
        void Start()
        {
            mainCam = Camera.main;
        }

        // Update is called once per frame
        void Update()
        {
            // Read mouse position
#if HAVE_INPUTSYSTEM
            if (Mouse.current != null)
                mousePos = Mouse.current.position.ReadValue();
#else
            mousePos = Input.mousePosition;
#endif

            // Mouse button state (left)
            bool leftDown = false, leftUp = false, leftHeld = false;
#if HAVE_INPUTSYSTEM
            leftDown = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            leftUp = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            leftHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            leftDown = Input.GetMouseButtonDown(0);
            leftUp = Input.GetMouseButtonUp(0);
            leftHeld = Input.GetMouseButton(0);
#endif

            if (leftDown)
            {
                clickedPosition = mousePos;
                canControl = true;
            }

            if (leftUp)
            {
                canControl = false;
            }

            // Detect if pointer is over UI (Input System UI module compatible)
            bool overUI = false;
            if (EventSystem.current != null)
            {
                int pointerId = -1; // default mouse pointer id
#if HAVE_INPUTSYSTEM
                try { pointerId = UnityEngine.InputSystem.UI.PointerId.mousePointerId; } catch { pointerId = -1; }
#endif
                overUI = EventSystem.current.IsPointerOverGameObject(pointerId);
            }

            if (!overUI)
            {
                // Scroll wheel
                float scrollY = 0f;
#if HAVE_INPUTSYSTEM
                scrollY = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#else
                scrollY = Input.GetAxis("Mouse ScrollWheel") * 120f; // match normalization below
#endif
                mainCam.fieldOfView += -(scrollY / 120f) * zoomSpeed; // normalize typical 120 units per notch
                mainCam.fieldOfView = Mathf.Clamp(mainCam.fieldOfView, ZoomDistanceMinMax.x, ZoomDistanceMinMax.y);
            }
            else
            {
                canControl = false;
            }

            if (canControl && leftHeld)
            {
                var dragDirection = clickedPosition - mousePos;

                if (dragDirection.magnitude > threshold)
                {
                    float speed = Time.deltaTime * dragDirection.magnitude;
                    if (mousePos.x > clickedPosition.x)
                        podium.RotateAround(target.position, -target.transform.up, speed);
                    else
                        podium.RotateAround(target.position, target.transform.up, speed);
                }
            }
        }
    }
}
