using UnityEngine;

namespace WizardWars.LocalMultiplayer
{
    /// <summary>
    /// Provides a unified input interface for local multiplayer.
    /// Abstracts the legacy per-player Input.GetAxis/GetButton patterns into a slot-based model.
    /// </summary>
    public class InputProvider : MonoBehaviour
    {
        private static InputProvider instance;

        private const string LeftStickXFormat = "L_XAxis_{0}";
        private const string LeftStickYFormat = "L_YAxis_{0}";
        private const string RightStickXFormat = "R_XAxis_{0}";
        private const string RightStickYFormat = "R_YAxis_{0}";

        private const string ActionAFormat = "A_{0}";
        private const string ActionBFormat = "B_{0}";
        private const string ActionXFormat = "X_{0}";
        private const string ActionYFormat = "Y_{0}";

        private const string LBFormat = "LB_{0}";
        private const string RBFormat = "RB_{0}";
        private const string BackFormat = "Back_{0}";
        private const string StartFormat = "Start_{0}";
        private const string LSFormat = "LS_{0}";
        private const string RSFormat = "RS_{0}";

        private const string TriggersFormat = "Triggers_{0}";

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public static InputProvider Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("InputProvider");
                    instance = go.AddComponent<InputProvider>();
                }

                return instance;
            }
        }

        /// <summary>
        /// Get the left analog stick input for a player slot (1-based).
        /// </summary>
        public Vector2 GetLeftStick(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1; // Convert 0-based to 1-based joystick

            float x = Input.GetAxis(string.Format(LeftStickXFormat, joystickNumber));
            float y = Input.GetAxis(string.Format(LeftStickYFormat, joystickNumber));

            return new Vector2(x, y);
        }

        /// <summary>
        /// Get the right analog stick input for a player slot (1-based).
        /// </summary>
        public Vector2 GetRightStick(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;

            float x = Input.GetAxis(string.Format(RightStickXFormat, joystickNumber));
            float y = Input.GetAxis(string.Format(RightStickYFormat, joystickNumber));

            return new Vector2(x, y);
        }

        /// <summary>
        /// Get action button A (face button 1) state.
        /// </summary>
        public bool GetActionADown(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButtonDown(string.Format(ActionAFormat, joystickNumber));
        }

        public bool GetActionA(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButton(string.Format(ActionAFormat, joystickNumber));
        }

        /// <summary>
        /// Get action button B (face button 2) state.
        /// </summary>
        public bool GetActionBDown(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButtonDown(string.Format(ActionBFormat, joystickNumber));
        }

        public bool GetActionB(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButton(string.Format(ActionBFormat, joystickNumber));
        }

        /// <summary>
        /// Get action button X (face button 3) state.
        /// </summary>
        public bool GetActionXDown(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButtonDown(string.Format(ActionXFormat, joystickNumber));
        }

        public bool GetActionX(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButton(string.Format(ActionXFormat, joystickNumber));
        }

        /// <summary>
        /// Get action button Y (face button 4) state.
        /// </summary>
        public bool GetActionYDown(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButtonDown(string.Format(ActionYFormat, joystickNumber));
        }

        public bool GetActionY(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButton(string.Format(ActionYFormat, joystickNumber));
        }

        /// <summary>
        /// Get left bumper (LB) button state.
        /// </summary>
        public bool GetLBDown(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButtonDown(string.Format(LBFormat, joystickNumber));
        }

        public bool GetLB(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButton(string.Format(LBFormat, joystickNumber));
        }

        /// <summary>
        /// Get right bumper (RB) button state.
        /// </summary>
        public bool GetRBDown(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButtonDown(string.Format(RBFormat, joystickNumber));
        }

        public bool GetRB(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButton(string.Format(RBFormat, joystickNumber));
        }

        /// <summary>
        /// Get back button state.
        /// </summary>
        public bool GetBackDown(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButtonDown(string.Format(BackFormat, joystickNumber));
        }

        /// <summary>
        /// Get start button state.
        /// </summary>
        public bool GetStartDown(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButtonDown(string.Format(StartFormat, joystickNumber));
        }

        /// <summary>
        /// Get left stick click state.
        /// </summary>
        public bool GetLeftStickClickDown(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButtonDown(string.Format(LSFormat, joystickNumber));
        }

        /// <summary>
        /// Get right stick click state.
        /// </summary>
        public bool GetRightStickClickDown(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetButtonDown(string.Format(RSFormat, joystickNumber));
        }

        /// <summary>
        /// Get triggers (analogs).
        /// </summary>
        public float GetTriggers(int playerSlotIndex)
        {
            int joystickNumber = playerSlotIndex + 1;
            return Input.GetAxis(string.Format(TriggersFormat, joystickNumber));
        }

        /// <summary>
        /// Query the full input state for a player slot.
        /// Useful for controller-based games.
        /// </summary>
        public PlayerInputState GetPlayerInputState(int playerSlotIndex)
        {
            return new PlayerInputState
            {
                LeftStick = GetLeftStick(playerSlotIndex),
                RightStick = GetRightStick(playerSlotIndex),
                ActionA = GetActionA(playerSlotIndex),
                ActionB = GetActionB(playerSlotIndex),
                ActionX = GetActionX(playerSlotIndex),
                ActionY = GetActionY(playerSlotIndex),
                LB = GetLB(playerSlotIndex),
                RB = GetRB(playerSlotIndex),
                Back = GetBackDown(playerSlotIndex),
                Start = GetStartDown(playerSlotIndex),
                LeftStickClick = GetLeftStickClickDown(playerSlotIndex),
                RightStickClick = GetRightStickClickDown(playerSlotIndex),
                Triggers = GetTriggers(playerSlotIndex)
            };
        }
    }

    /// <summary>
    /// Snapshot of input state for a player at a given frame.
    /// </summary>
    public struct PlayerInputState
    {
        public Vector2 LeftStick;
        public Vector2 RightStick;
        public bool ActionA;
        public bool ActionB;
        public bool ActionX;
        public bool ActionY;
        public bool LB;
        public bool RB;
        public bool Back;
        public bool Start;
        public bool LeftStickClick;
        public bool RightStickClick;
        public float Triggers;
    }
}
