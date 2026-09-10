using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace DisjointSetMaze
{
    public sealed class FlyCamera : MonoBehaviour
    {
        [Header("Movement")]
        [FormerlySerializedAs("acceleration")]
        [SerializeField, Min(0f)] private float _acceleration = 50f;

        [FormerlySerializedAs("accSprintMultiplier")]
        [SerializeField, Min(1f)] private float _sprintMultiplier = 4f;

        [FormerlySerializedAs("dampingCoefficient")]
        [SerializeField, Min(0f)] private float _damping = 5f;

        [Header("Look")]
        [FormerlySerializedAs("lookSensitivity")]
        [SerializeField, Min(0f)] private float _lookSensitivity = 0.08f;

        [SerializeField, Range(1f, 89f)] private float _maximumPitch = 89f;

        [Header("Cursor")]
        [FormerlySerializedAs("focusOnEnable")]
        [SerializeField] private bool _focusOnEnable = true;

        private Vector3 _velocity;
        private float _yaw;
        private float _pitch;

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _focusAction;
        private InputAction _cancelAction;

        private static bool IsFocused
        {
            get => Cursor.lockState == CursorLockMode.Locked;
        }

        private void Awake()
        {
            CreateInputActions();

            var angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = NormalizeAngle(angles.x);
        }

        private void OnEnable()
        {
            SetActionsEnabled(true);
            if (_focusOnEnable) SetFocused(true);
        }

        private void OnDisable()
        {
            SetActionsEnabled(false);
            SetFocused(false);
        }

        private void OnDestroy()
        {
            _moveAction?.Dispose();
            _lookAction?.Dispose();
            _sprintAction?.Dispose();
            _focusAction?.Dispose();
            _cancelAction?.Dispose();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SetFocused(false);
        }

        private void OnValidate()
        {
            _acceleration = Mathf.Max(0f, _acceleration);
            _sprintMultiplier = Mathf.Max(1f, _sprintMultiplier);
            _damping = Mathf.Max(0f, _damping);
            _lookSensitivity = Mathf.Max(0f, _lookSensitivity);
            _maximumPitch = Mathf.Clamp(_maximumPitch, 1f, 89f);
        }

        private void Update()
        {
            if (IsFocused)
            {
                UpdateInput();
            }
            else if (_focusAction.WasPressedThisFrame())
            {
                SetFocused(true);
            }

            var deltaTime = Time.unscaledDeltaTime;
            _velocity *= Mathf.Exp(-_damping * deltaTime);
            transform.position += _velocity * deltaTime;
        }

        private void UpdateInput()
        {
            var deltaTime = Time.unscaledDeltaTime;
            var movement = _moveAction.ReadValue<Vector3>();
            var sprint = _sprintAction.IsPressed() ? _sprintMultiplier : 1f;
            _velocity += transform.TransformDirection(movement.normalized) * (_acceleration * sprint * deltaTime);

            var look = _lookAction.ReadValue<Vector2>() * _lookSensitivity;
            _yaw += look.x;
            _pitch = Mathf.Clamp(_pitch - look.y, -_maximumPitch, _maximumPitch);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            if (_cancelAction.WasPressedThisFrame())
            {
                SetFocused(false);
            }
        }

        private void CreateInputActions()
        {
            _moveAction = new InputAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("3DVector")
                .With("Forward", "<Keyboard>/w")
                .With("Backward", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Up", "<Keyboard>/e")
                .With("Down", "<Keyboard>/q");

            _lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            _sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            _focusAction = new InputAction("Focus", InputActionType.Button, "<Mouse>/leftButton");
            _cancelAction = new InputAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
        }

        private void SetActionsEnabled(bool enabled)
        {
            SetActionEnabled(_moveAction, enabled);
            SetActionEnabled(_lookAction, enabled);
            SetActionEnabled(_sprintAction, enabled);
            SetActionEnabled(_focusAction, enabled);
            SetActionEnabled(_cancelAction, enabled);
        }

        private static void SetActionEnabled(InputAction action, bool enabled)
        {
            if (enabled) action.Enable();
            else action.Disable();
        }

        private static void SetFocused(bool focused)
        {
            Cursor.lockState = focused ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !focused;
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
