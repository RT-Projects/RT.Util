using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RT.Util
{
    public static partial class Ut
    {
        /// <summary>
        ///     Sends the specified sequence of key strokes to the active application. See remarks for details.</summary>
        /// <param name="keys">
        ///     A collection of objects of type <see cref="Keys"/>, <see cref="char"/>, or <c>System.Tuple&lt;Keys,
        ///     bool&gt;</c>.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="keys"/> was null.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="keys"/> contains an object which is of an unexpected type. Only <see cref="Keys"/>, <see
        ///     cref="char"/> and <c>System.Tuple&lt;System.Windows.Forms.Keys, bool&gt;</c> are accepted.</exception>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><description>
        ///             For objects of type <see cref="Keys"/>, the relevant key is pressed and released.</description></item>
        ///         <item><description>
        ///             For objects of type <see cref="char"/>, the specified Unicode character is simulated as a keypress and
        ///             release.</description></item>
        ///         <item><description>
        ///             For objects of type <c>Tuple&lt;Keys, bool&gt;</c> or <c>ValueType&lt;Keys, bool&gt;</c>, the bool
        ///             specifies whether to simulate only a key-down (false) or only a key-up (true).</description></item></list></remarks>
        /// <example>
        ///     <para>
        ///         The following example demonstrates how to use this method to send the key combination Win+R:</para>
        ///     <code>
        ///         Ut.SendKeystrokes(Ut.NewArray&lt;object&gt;(
        ///             (Keys.LWin, false),
        ///             Keys.R,
        ///             (Keys.LWin, true)
        ///         ));</code></example>
        public static void SendKeystrokes(IEnumerable<object> keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            var input = new List<WinAPI.INPUT>();
            foreach (var elem in keys)
            {
                void sendTuple(Keys key, bool isUp)
                {
                    var keyEvent = new WinAPI.INPUT
                    {
                        Type = WinAPI.INPUT_KEYBOARD,
                        SpecificInput = new WinAPI.MOUSEKEYBDHARDWAREINPUT
                        {
                            Keyboard = new WinAPI.KEYBDINPUT { wVk = (ushort) key }
                        }
                    };
                    if (isUp)
                        keyEvent.SpecificInput.Keyboard.dwFlags |= WinAPI.KEYEVENTF_KEYUP;
                    input.Add(keyEvent);
                }

                if (elem is Tuple<Keys, bool> t)
                    sendTuple(t.Item1, t.Item2);
                else if (elem is ValueTuple<Keys, bool> vt)
                    sendTuple(vt.Item1, vt.Item2);
                else
                {
                    if (!(elem is Keys || elem is char))
                        throw new ArgumentException(@"The input collection is expected to contain only objects of type Keys, char, Tuple<Keys, bool> or ValueTuple<Keys, bool>.", nameof(keys));
                    var keyDown = new WinAPI.INPUT
                    {
                        Type = WinAPI.INPUT_KEYBOARD,
                        SpecificInput = new WinAPI.MOUSEKEYBDHARDWAREINPUT
                        {
                            Keyboard = (elem is Keys)
                                ? new WinAPI.KEYBDINPUT { wVk = (ushort) (Keys) elem }
                                : new WinAPI.KEYBDINPUT { wScan = (char) elem, dwFlags = WinAPI.KEYEVENTF_UNICODE }
                        }
                    };
                    var keyUp = keyDown;
                    keyUp.SpecificInput.Keyboard.dwFlags |= WinAPI.KEYEVENTF_KEYUP;
                    input.Add(keyDown);
                    input.Add(keyUp);
                }
            }
            var inputArr = input.ToArray();
            WinAPI.SendInput((uint) inputArr.Length, inputArr, Marshal.SizeOf(input[0]));
        }

        /// <summary>
        ///     Sends the specified key the specified number of times.</summary>
        /// <param name="key">
        ///     Key stroke to send.</param>
        /// <param name="times">
        ///     Number of times to send the <paramref name="key"/>.</param>
        public static void SendKeystrokes(Keys key, int times)
        {
            if (times > 0)
                SendKeystrokes(Enumerable.Repeat((object) key, times));
        }

        /// <summary>Sends key strokes equivalent to typing the specified text.</summary>
        public static void SendKeystrokesForText(string text)
        {
            if (!string.IsNullOrEmpty(text))
                SendKeystrokes(text.Cast<object>());
        }

        /// <summary>Determines whether the Ctrl key is pressed.</summary>
        public static bool Ctrl { get { return Control.ModifierKeys.HasFlag(Keys.Control); } }
        /// <summary>Determines whether the Alt key is pressed.</summary>
        public static bool Alt { get { return Control.ModifierKeys.HasFlag(Keys.Alt); } }
        /// <summary>Determines whether the Shift key is pressed.</summary>
        public static bool Shift { get { return Control.ModifierKeys.HasFlag(Keys.Shift); } }
    }
}
