using System.Windows.Forms;

namespace RT.Util.Controls
{
    /// <summary>
    /// This control enables the user to specify a hotkey combination by pressing it on
    /// the keyboard. For exmaple, if the user presses Ctrl + Alt + A while this control
    /// is focussed, the control would display "Ctrl+Alt+A". The combination can be read
    /// out by the program in a convenient format.
    /// </summary>
    public sealed class HotkeyEdit: TextBox
    {
        private bool _lastNone;
        private bool _lastCtrl;
        private bool _lastAlt;
        private bool _lastShift;
        private Keys _lastKey;
        private bool _oneKeyOnly;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public HotkeyEdit()
        {
            _lastNone = true;
            _oneKeyOnly = false;
        }

        /// <summary>
        /// If true, the control does not accept key combinations, such as "Ctrl + A",
        /// however it does accept special keys such as "Ctrl".
        /// </summary>
        public bool OneKeyOnly
        {
            get { return _oneKeyOnly; }
            set { _oneKeyOnly = value; }
        }

        /// <summary>
        /// Returns true if the control does not hold a shortcut combination
        /// (for example, because the user didn't press anything).
        /// </summary>
        public bool ShortcutNone
        {
            get { return _lastNone; }
        }

        /// <summary>
        /// Returns true if the shortcut combination includes Ctrl.
        /// </summary>
        public bool ShortcutCtrl
        {
            get { return _lastCtrl; }
        }

        /// <summary>
        /// Returns true if the shortcut combination includes Alt.
        /// </summary>
        public bool ShortcutAlt
        {
            get { return _lastAlt; }
        }

        /// <summary>
        /// Returns true if the shortcut combination includes Shift.
        /// </summary>
        public bool ShortcutShift
        {
            get { return _lastShift; }
        }

        /// <summary>
        /// Returns the main shortcut key.
        /// </summary>
        public Keys ShortcutKey
        {
            get { return _lastKey; }
        }

        private void SetText()
        {
            if (_lastNone && !_lastShift && !_lastCtrl && !_lastAlt) {
                Text = "(none)";
                return;
            }

            string s = "";
            if (_lastCtrl) s += "Ctrl + ";
            if (_lastAlt) s += "Alt + ";
            if (_lastShift) s += "Shift + ";


            if (!_lastNone) {
                switch (_lastKey) {
                    case Keys.D0: s+="0"; break;
                    case Keys.D1: s+="1"; break;
                    case Keys.D2: s+="2"; break;
                    case Keys.D3: s+="3"; break;
                    case Keys.D4: s+="4"; break;
                    case Keys.D5: s+="5"; break;
                    case Keys.D6: s+="6"; break;
                    case Keys.D7: s+="7"; break;
                    case Keys.D8: s+="8"; break;
                    case Keys.D9: s+="9"; break;
                    case Keys.ShiftKey: s+="Shift"; break;
                    case Keys.ControlKey: s+="Control"; break;
                    case Keys.Menu: s+="Alt"; break;
                    default:
                        s += _lastKey.ToString();
                        break;
                }
            }

            Text = s;
            SelectionStart = s.Length;
        }

        /// <summary>Captures key presses and updates the control's state accordingly.</summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (((e.KeyCode == Keys.ShiftKey) || (e.KeyCode == Keys.ControlKey) || (e.KeyCode == Keys.Menu)) && (!_oneKeyOnly)) {
                // If it's a modifier then reset the key
                _lastNone = true;
            } else {
                // Otherwise set the key
                _lastNone = false;
                _lastKey = e.KeyCode;
            }

            // Update current modifiers state
            if (_oneKeyOnly) {
                _lastCtrl = _lastAlt = _lastShift = false;
            } else {
                _lastCtrl = e.Control;
                _lastAlt = e.Alt;
                _lastShift = e.Shift;
            }

            // Update display etc
            SetText();
            e.Handled = true;
        }

        /// <summary>Captures key presses and updates the control's state accordingly.</summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            // Update current modifiers state
            if (_lastNone & !_oneKeyOnly) {
                _lastCtrl = e.Control;
                _lastAlt = e.Alt;
                _lastShift = e.Shift;
            }

            // Update display etc
            SetText();
            e.Handled = true;
        }

        /// <summary>Captures key presses and updates the control's state accordingly.</summary>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            e.Handled = true;
        }

    }
}