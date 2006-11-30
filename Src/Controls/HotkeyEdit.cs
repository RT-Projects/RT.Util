using System.Windows.Forms;

namespace RT.Util.Controls
{
    public class HotkeyEdit: TextBox
    {
        private bool LastNone;
        private bool LastCtrl;
        private bool LastAlt;
        private bool LastShift;
        private Keys LastKey;
        private bool FOneKeyOnly;

        public HotkeyEdit()
        {
            LastNone = true;
            FOneKeyOnly = false;
        }

        public bool OneKeyOnly
        {
            get { return FOneKeyOnly; }
            set { FOneKeyOnly = value; }
        }

        public bool ShortcutNone
        {
            get { return LastNone; }
        }

        public bool ShortcutCtrl
        {
            get { return LastCtrl; }
        }

        public bool ShortcutAlt
        {
            get { return LastAlt; }
        }

        public bool ShortcutShift
        {
            get { return LastShift; }
        }

        public Keys ShortcutKey
        {
            get { return LastKey; }
        }

        private void SetText()
        {
            if (LastNone && !LastShift && !LastCtrl && !LastAlt) {
                Text = "(none)";
                return;
            }

            string s = "";
            if (LastCtrl) s += "Ctrl + ";
            if (LastAlt) s += "Alt + ";
            if (LastShift) s += "Shift + ";


            if (!LastNone) {
                switch (LastKey) {
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
                        s += LastKey.ToString();
                        break;
                }
            }

            Text = s;
            SelectionStart = s.Length;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (((e.KeyCode == Keys.ShiftKey) || (e.KeyCode == Keys.ControlKey) || (e.KeyCode == Keys.Menu)) && (!FOneKeyOnly)) {
                // If it's a modifier then reset the key
                LastNone = true;
            } else {
                // Otherwise set the key
                LastNone = false;
                LastKey = e.KeyCode;
            }

            // Update current modifiers state
            if (FOneKeyOnly) {
                LastCtrl = LastAlt = LastShift = false;
            } else {
                LastCtrl = e.Control;
                LastAlt = e.Alt;
                LastShift = e.Shift;
            }

            // Update display etc
            SetText();
            e.Handled = true;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            // Update current modifiers state
            if (LastNone & !FOneKeyOnly) {
                LastCtrl = e.Control;
                LastAlt = e.Alt;
                LastShift = e.Shift;
            }

            // Update display etc
            SetText();
            e.Handled = true;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            e.Handled = true;
        }

    }
}