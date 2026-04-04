using UnityEngine;
using UnityEngine.InputSystem;

namespace Case15.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerTopDownMovement : MonoBehaviour
    {
        public enum InputMode
        {
            Local,
            External
        }

        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private InputMode inputMode = InputMode.Local;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 externalMoveInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        private void Update()
        {
            if (inputMode == InputMode.External)
            {
                moveInput = NormalizeInput(externalMoveInput);
                return;
            }

            var x = 0f;
            var y = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    x -= 1f;
                }

                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    x += 1f;
                }

                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                {
                    y -= 1f;
                }

                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                {
                    y += 1f;
                }
            }

            if (Gamepad.current != null)
            {
                var stick = Gamepad.current.leftStick.ReadValue();
                x += stick.x;
                y += stick.y;
            }

            moveInput = NormalizeInput(new Vector2(x, y));
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }

        public void SetExternalInput(Vector2 input)
        {
            externalMoveInput = input;
        }

        public void SetInputMode(InputMode mode)
        {
            inputMode = mode;
        }

        private static Vector2 NormalizeInput(Vector2 input)
        {
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            return input;
        }
    }
}
