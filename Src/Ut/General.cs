using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RT.Util
{
    /// <summary>
    /// This class offers some generic static functions which are hard to categorize
    /// under any more specific classes.
    /// </summary>
    public static partial class Ut
    {
        /// <summary>
        /// Converts file size in bytes to a string in bytes, kbytes, Mbytes
        /// or Gbytes accordingly. The suffix appended is kB, MB or GB.
        /// </summary>
        /// <param name="size">Size in bytes</param>
        /// <returns>Converted string</returns>
        public static string SizeToString(long size)
        {
            if (size == 0)
                return "0";
            else if (size < 1024)
                return size.ToString("#,###");
            else if (size < 1024 * 1024)
                return (size / 1024d).ToString("#,###.## kB");
            else if (size < 1024 * 1024 * 1024)
                return (size / (1024d * 1024d)).ToString("#,###.## MB");
            else
                return (size / (1024d * 1024d * 1024d)).ToString("#,###.## GB");
        }

        /// <summary>
        /// Returns the smaller of the two IComparable values. If the values are
        /// equal, returns the first one.
        /// </summary>
        public static T Min<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) <= 0 ? val1 : val2;
        }

        /// <summary>
        /// Returns the smaller of the three IComparable values. If two values are
        /// equal, returns the earlier one.
        /// </summary>
        public static T Min<T>(T val1, T val2, T val3) where T : IComparable<T>
        {
            T c1 = val1.CompareTo(val2) <= 0 ? val1 : val2;
            return c1.CompareTo(val3) <= 0 ? c1 : val3;
        }

        /// <summary>
        /// Returns the smallest of all arguments passed in. Uses the Linq .Min
        /// extension method to do the work.
        /// </summary>
        public static T Min<T>(params T[] args) where T : IComparable<T>
        {
            return args.Min();
        }

        /// <summary>
        /// Returns the larger of the two IComparable values. If the values are
        /// equal, returns the first one.
        /// </summary>
        public static T Max<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) >= 0 ? val1 : val2;
        }

        /// <summary>
        /// Returns the larger of the three IComparable values. If two values are
        /// equal, returns the earlier one.
        /// </summary>
        public static T Max<T>(T val1, T val2, T val3) where T : IComparable<T>
        {
            T c1 = val1.CompareTo(val2) >= 0 ? val1 : val2;
            return c1.CompareTo(val3) >= 0 ? c1 : val3;
        }

        /// <summary>
        /// Returns the largest of all arguments passed in. Uses the Linq .Max
        /// extension method to do the work.
        /// </summary>
        public static T Max<T>(params T[] args) where T : IComparable<T>
        {
            return args.Max();
        }

        /// <summary>
        /// Sends the specified sequence of key strokes to the active application.
        /// </summary>
        /// <param name="keys">A collection of objects of type <see cref="Keys"/> or <see cref="char"/>.</param>
        /// <exception cref="ArgumentException">
        ///     <list type="bullet">
        ///         <item><description><paramref name="keys"/> was null.</description></item>
        ///         <item><description><paramref name="keys"/> contains an object which is of an unexpected type. Only <see cref="Keys"/> and <see cref="char"/> are accepted.</description></item>
        ///     </list>
        /// </exception>
        public static void SendKeystrokes(IEnumerable<object> keys)
        {
            if (keys == null)
                throw new ArgumentException(@"The input collection cannot be null.", "keys");
            var arr = keys.ToArray();
            if (arr.Length < 1)
                return;

            var inputArr = new WinAPI.INPUT[arr.Length * 2];
            for (int i = 0; i < arr.Length; i++)
            {
                if (!(arr[i] is Keys || arr[i] is char))
                    throw new ArgumentException(@"The input collection is expected to contain only objects of type Keys or char.", "keys");
                var keyDown = new WinAPI.INPUT
                {
                    type = WinAPI.INPUT_KEYBOARD,
                    mkhi = new WinAPI.MOUSEKEYBDHARDWAREINPUT
                    {
                        ki = (arr[i] is Keys)
                            ? new WinAPI.KEYBDINPUT { wVk = (ushort) (Keys) arr[i] }
                            : new WinAPI.KEYBDINPUT { wScan = (ushort) (char) arr[i], dwFlags = WinAPI.KEYEVENTF_UNICODE }
                    }
                };
                var keyUp = keyDown;
                keyUp.mkhi.ki.dwFlags = WinAPI.KEYEVENTF_KEYUP;
                inputArr[2 * i] = keyDown;
                inputArr[2 * i + 1] = keyUp;
            }
            WinAPI.SendInput((uint) (2 * arr.Length), inputArr, Marshal.SizeOf(inputArr[0]));
        }

        /// <summary>
        /// Sends the specified key the specified number of times.
        /// </summary>
        /// <param name="key">Key stroke to send.</param>
        /// <param name="times">Number of times to send the <paramref name="key"/>.</param>
        public static void SendKeystrokes(Keys key, int times)
        {
            if (times > 0)
                SendKeystrokes(Enumerable.Repeat((object) key, times));
        }

        /// <summary>
        /// Sends key strokes equivalent to typing the specified text.
        /// </summary>
        public static void SendKeystrokesForText(string text)
        {
            if (!string.IsNullOrEmpty(text))
                SendKeystrokes(text.Cast<object>());
        }
    }
}
