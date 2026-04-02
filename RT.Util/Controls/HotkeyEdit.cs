namespace RT.Util.Controls;

/// <summary>
///     This control enables the user to specify a hotkey combination by pressing it on the keyboard. For exmaple, if the user
///     presses Ctrl + Alt + A while this control is focussed, the control would display "Ctrl+Alt+A". The combination can be
///     read out by the program in a convenient format.</summary>
public sealed class HotkeyEdit : TextBox
{
    private bool _lastNone;
    private bool _lastCtrl;
    private bool _lastAlt;
    private bool _lastShift;
    private Keys _lastKey;
    private bool _oneKeyOnly;

    /// <summary>Constructs a new instance.</summary>
    public HotkeyEdit()
    {
        _lastNone = true;
        _oneKeyOnly = false;
    }

    /// <summary>
    ///     If true, the control does not accept key combinations, such as "Ctrl + A", however it does accept special keys
    ///     such as "Ctrl".</summary>
    public bool OneKeyOnly
    {
        get { return _oneKeyOnly; }
        set { _oneKeyOnly = value; }
    }

    /// <summary>
    ///     Returns true if the control does not hold a shortcut combination (for example, because the user didn't press
    ///     anything).</summary>
    public bool ShortcutNone
    {
        get { return _lastNone; }
    }

    /// <summary>Returns true if the shortcut combination includes Ctrl.</summary>
    public bool ShortcutCtrl
    {
        get { return _lastCtrl; }
    }

    /// <summary>Returns true if the shortcut combination includes Alt.</summary>
    public bool ShortcutAlt
    {
        get { return _lastAlt; }
    }

    /// <summary>Returns true if the shortcut combination includes Shift.</summary>
    public bool ShortcutShift
    {
        get { return _lastShift; }
    }

    /// <summary>Returns the main shortcut key.</summary>
    public Keys ShortcutKey
    {
        get { return _lastKey; }
    }

    private void setText()
    {
        if (_lastNone && !_lastShift && !_lastCtrl && !_lastAlt)
        {
            Text = "(none)";
            return;
        }

        string s = "";
        if (_lastCtrl) s += "Ctrl + ";
        if (_lastAlt) s += "Alt + ";
        if (_lastShift) s += "Shift + ";


        if (!_lastNone)
        {
            s += _lastKey switch
            {
                Keys.D0 => "0",
                Keys.D1 => "1",
                Keys.D2 => "2",
                Keys.D3 => "3",
                Keys.D4 => "4",
                Keys.D5 => "5",
                Keys.D6 => "6",
                Keys.D7 => "7",
                Keys.D8 => "8",
                Keys.D9 => "9",
                Keys.ShiftKey => "Shift",
                Keys.ControlKey => "Control",
                Keys.Menu => "Alt",
                _ => _lastKey.ToString(),
            };
        }

        Text = s;
        SelectionStart = s.Length;
    }

    /// <summary>Captures key presses and updates the control's state accordingly.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (((e.KeyCode == Keys.ShiftKey) || (e.KeyCode == Keys.ControlKey) || (e.KeyCode == Keys.Menu)) && (!_oneKeyOnly))
        {
            // If it's a modifier then reset the key
            _lastNone = true;
        }
        else
        {
            // Otherwise set the key
            _lastNone = false;
            _lastKey = e.KeyCode;
        }

        // Update current modifiers state
        if (_oneKeyOnly)
        {
            _lastCtrl = _lastAlt = _lastShift = false;
        }
        else
        {
            _lastCtrl = e.Control;
            _lastAlt = e.Alt;
            _lastShift = e.Shift;
        }

        // Update display etc
        setText();
        e.Handled = true;
    }

    /// <summary>Captures key presses and updates the control's state accordingly.</summary>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        // Update current modifiers state
        if (_lastNone & !_oneKeyOnly)
        {
            _lastCtrl = e.Control;
            _lastAlt = e.Alt;
            _lastShift = e.Shift;
        }

        // Update display etc
        setText();
        e.Handled = true;
    }

    /// <summary>Captures key presses and updates the control's state accordingly.</summary>
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        e.Handled = true;
    }

}
