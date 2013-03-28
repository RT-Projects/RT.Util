using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>This class offers some generic static functions which are hard to categorize under any more specific classes.</summary>
    public static partial class Ut
    {
        /// <summary>
        ///     Converts file size in bytes to a string that uses KB, MB, GB or TB.</summary>
        /// <param name="size">
        ///     The file size in bytes.</param>
        /// <returns>
        ///     The converted string.</returns>
        public static string SizeToString(long size)
        {
            if (size == 0)
                return "0";
            else if (size < 1024)
                return size.ToString("#,###");
            else if (size < 1024 * 1024)
                return (size / 1024d).ToString("#,###.## KB");
            else if (size < 1024 * 1024 * 1024)
                return (size / 1024d / 1024d).ToString("#,###.## MB");
            else if (size < 1024L * 1024 * 1024 * 1024)
                return (size / 1024d / 1024d / 1024d).ToString("#,###.## GB");
            else
                return (size / 1024d / 1024d / 1024d / 1024d).ToString("#,###.## TB");
        }

        /// <summary>Returns the smaller of the two IComparable values. If the values are equal, returns the first one.</summary>
        public static T Min<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) <= 0 ? val1 : val2;
        }

        /// <summary>Returns the smaller of the three IComparable values. If two values are equal, returns the earlier one.</summary>
        public static T Min<T>(T val1, T val2, T val3) where T : IComparable<T>
        {
            T c1 = val1.CompareTo(val2) <= 0 ? val1 : val2;
            return c1.CompareTo(val3) <= 0 ? c1 : val3;
        }

        /// <summary>Returns the smallest of all arguments passed in. Uses the Linq .Min extension method to do the work.</summary>
        public static T Min<T>(params T[] args) where T : IComparable<T>
        {
            return args.Min();
        }

        /// <summary>Returns the larger of the two IComparable values. If the values are equal, returns the first one.</summary>
        public static T Max<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) >= 0 ? val1 : val2;
        }

        /// <summary>Returns the larger of the three IComparable values. If two values are equal, returns the earlier one.</summary>
        public static T Max<T>(T val1, T val2, T val3) where T : IComparable<T>
        {
            T c1 = val1.CompareTo(val2) >= 0 ? val1 : val2;
            return c1.CompareTo(val3) >= 0 ? c1 : val3;
        }

        /// <summary>Returns the largest of all arguments passed in. Uses the Linq .Max extension method to do the work.</summary>
        public static T Max<T>(params T[] args) where T : IComparable<T>
        {
            return args.Max();
        }

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
        ///             For objects of type <c>Tuple&lt;Keys, bool&gt;</c>, the bool specifies whether to simulate only a key-down
        ///             (false) or only a key-up (true).</description></item></list></remarks>
        /// <example>
        ///     <para>
        ///         The following example demonstrates how to use this method to send the key combination Win+R:</para>
        ///     <code>
        ///         Ut.SendKeystrokes(Ut.NewArray&lt;object&gt;(
        ///             Tuple.Create(Keys.LWin, true),
        ///             Keys.R,
        ///             Tuple.Create(Keys.LWin, false)
        ///         ));</code></example>
        public static void SendKeystrokes(IEnumerable<object> keys)
        {
            if (keys == null)
                throw new ArgumentNullException("keys");

            var input = new List<WinAPI.INPUT>();
            foreach (var elem in keys)
            {
                Tuple<Keys, bool> t;
                if ((t = elem as Tuple<Keys, bool>) != null)
                {
                    var keyEvent = new WinAPI.INPUT
                    {
                        Type = WinAPI.INPUT_KEYBOARD,
                        SpecificInput = new WinAPI.MOUSEKEYBDHARDWAREINPUT
                        {
                            Keyboard = new WinAPI.KEYBDINPUT { wVk = (ushort) t.Item1 }
                        }
                    };
                    if (t.Item2)
                        keyEvent.SpecificInput.Keyboard.dwFlags |= WinAPI.KEYEVENTF_KEYUP;
                    input.Add(keyEvent);
                }
                else
                {
                    if (!(elem is Keys || elem is char))
                        throw new ArgumentException(@"The input collection is expected to contain only objects of type Keys, char, or Tuple<Keys, bool>.", "keys");
                    var keyDown = new WinAPI.INPUT
                    {
                        Type = WinAPI.INPUT_KEYBOARD,
                        SpecificInput = new WinAPI.MOUSEKEYBDHARDWAREINPUT
                        {
                            Keyboard = (elem is Keys)
                                ? new WinAPI.KEYBDINPUT { wVk = (ushort) (Keys) elem }
                                : new WinAPI.KEYBDINPUT { wScan = (ushort) (char) elem, dwFlags = WinAPI.KEYEVENTF_UNICODE }
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

        /// <summary>
        ///     Reads the specified file and computes the SHA1 hash function from its contents.</summary>
        /// <param name="path">
        ///     Path to the file to compute SHA1 hash function from.</param>
        /// <returns>
        ///     Result of the SHA1 hash function as a string of hexadecimal digits.</returns>
        public static string Sha1(string path)
        {
            using (var f = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return SHA1.Create().ComputeHash(f).ToHex();
        }

        /// <summary>
        ///     Reads the specified file and computes the MD5 hash function from its contents.</summary>
        /// <param name="path">
        ///     Path to the file to compute MD5 hash function from.</param>
        /// <returns>
        ///     Result of the MD5 hash function as a string of hexadecimal digits.</returns>
        public static string Md5(string path)
        {
            using (var f = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return MD5.Create().ComputeHash(f).ToHex();
        }

        /// <summary>Returns the version of the entry assembly (the .exe file) in a standard format.</summary>
        public static string VersionOfExe()
        {
            var v = Assembly.GetEntryAssembly().GetName().Version;
            return "{0}.{1}.{2} ({3})".Fmt(v.Major, v.Minor, v.Build, v.Revision); // in our use: v.Build is build#, v.Revision is p4 changelist
        }

        /// <summary>
        ///     Checks the specified condition and causes the debugger to break if it is false. Throws an <see
        ///     cref="InternalErrorException"/> afterwards.</summary>
        public static void Assert(bool assertion, string message = null)
        {
            if (!assertion)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                throw new InternalErrorException(message ?? "Assertion failure");
            }
        }

        /// <summary>
        ///     Throws the specified exception.</summary>
        /// <typeparam name="TResult">
        ///     The type to return.</typeparam>
        /// <param name="exception">
        ///     The exception to throw.</param>
        /// <returns>
        ///     This method never returns a value. It always throws.</returns>
        public static TResult Throw<TResult>(Exception exception)
        {
            throw exception;
        }

        /// <summary>Determines whether the Ctrl key is pressed.</summary>
        public static bool Ctrl { get { return Control.ModifierKeys.HasFlag(Keys.Control); } }
        /// <summary>Determines whether the Alt key is pressed.</summary>
        public static bool Alt { get { return Control.ModifierKeys.HasFlag(Keys.Alt); } }
        /// <summary>Determines whether the Shift key is pressed.</summary>
        public static bool Shift { get { return Control.ModifierKeys.HasFlag(Keys.Shift); } }

        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Action Lambda(Action method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Action<T> Lambda<T>(Action<T> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Action<T1, T2> Lambda<T1, T2>(Action<T1, T2> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Action<T1, T2, T3> Lambda<T1, T2, T3>(Action<T1, T2, T3> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Action<T1, T2, T3, T4> Lambda<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Action<T1, T2, T3, T4, T5> Lambda<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Func<TResult> Lambda<TResult>(Func<TResult> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Func<T, TResult> Lambda<T, TResult>(Func<T, TResult> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Func<T1, T2, TResult> Lambda<T1, T2, TResult>(Func<T1, T2, TResult> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Func<T1, T2, T3, TResult> Lambda<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Func<T1, T2, T3, T4, TResult> Lambda<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> method) { return method; }
        /// <summary>
        ///     Allows the use of C#’s powerful type inference when declaring local lambdas whose delegate type doesn't make any
        ///     difference.</summary>
        public static Func<T1, T2, T3, T4, T5, TResult> Lambda<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> method) { return method; }

        /// <summary>Allows the use of type inference when creating .NET’s KeyValuePair&lt;TK,TV&gt;.</summary>
        public static KeyValuePair<TKey, TValue> KeyValuePair<TKey, TValue>(TKey key, TValue value) { return new KeyValuePair<TKey, TValue>(key, value); }

        /// <summary>
        ///     Returns the parameters as a new array.</summary>
        /// <remarks>
        ///     Useful to circumvent Visual Studio’s bug where multi-line literal arrays are not auto-formatted.</remarks>
        public static T[] NewArray<T>(params T[] parameters) { return parameters; }

        /// <summary>
        ///     Instantiates a fully-initialized array with the specified dimensions.</summary>
        /// <param name="size">
        ///     Size of the first dimension.</param>
        /// <param name="initialiser">
        ///     Function to initialise the value of every element.</param>
        /// <typeparam name="T">
        ///     Type of the array element.</typeparam>
        public static T[] NewArray<T>(int size, Func<int, T> initialiser)
        {
            if (initialiser == null)
                throw new ArgumentNullException("initialiser");
            var result = new T[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = initialiser(i);
            }
            return result;
        }

        /// <summary>
        ///     Instantiates a fully-initialized rectangular jagged array with the specified dimensions.</summary>
        /// <param name="size1">
        ///     Size of the first dimension.</param>
        /// <param name="size2">
        ///     Size of the second dimension.</param>
        /// <param name="initialiser">
        ///     Optional function to initialise the value of every element.</param>
        /// <typeparam name="T">
        ///     Type of the array element.</typeparam>
        public static T[][] NewArray<T>(int size1, int size2, Func<int, int, T> initialiser = null)
        {
            var result = new T[size1][];
            for (int i = 0; i < size1; i++)
            {
                var arr = new T[size2];
                if (initialiser != null)
                    for (int j = 0; j < size2; j++)
                        arr[j] = initialiser(i, j);
                result[i] = arr;
            }
            return result;
        }

        /// <summary>
        ///     Instantiates a fully-initialized "rectangular" jagged array with the specified dimensions.</summary>
        /// <param name="size1">
        ///     Size of the first dimension.</param>
        /// <param name="size2">
        ///     Size of the second dimension.</param>
        /// <param name="size3">
        ///     Size of the third dimension.</param>
        /// <param name="initialiser">
        ///     Optional function to initialise the value of every element.</param>
        /// <typeparam name="T">
        ///     Type of the array element.</typeparam>
        public static T[][][] NewArray<T>(int size1, int size2, int size3, Func<int, int, int, T> initialiser = null)
        {
            var result = new T[size1][][];
            for (int i = 0; i < size1; i++)
            {
                var arr = new T[size2][];
                for (int j = 0; j < size2; j++)
                {
                    var arr2 = new T[size3];
                    if (initialiser != null)
                        for (int k = 0; k < size2; k++)
                            arr2[k] = initialiser(i, j, k);
                    arr[j] = arr2;
                }
                result[i] = arr;
            }
            return result;
        }

        /// <summary>
        ///     Returns the integer represented by the specified string, or null if the string does not represent a valid 32-bit
        ///     integer.</summary>
        public static int? ParseInt32(string value) { int result; return int.TryParse(value, out result) ? (int?) result : null; }
        /// <summary>
        ///     Returns the integer represented by the specified string, or null if the string does not represent a valid 64-bit
        ///     integer.</summary>
        public static long? ParseInt64(string value) { long result; return long.TryParse(value, out result) ? (long?) result : null; }
        /// <summary>
        ///     Returns the floating-point number represented by the specified string, or null if the string does not represent a
        ///     valid double-precision floating-point number.</summary>
        public static double? ParseDouble(string value) { double result; return double.TryParse(value, out result) ? (double?) result : null; }
        /// <summary>
        ///     Returns the date/time stamp represented by the specified string, or null if the string does not represent a valid
        ///     date/time stamp.</summary>
        public static DateTime? ParseDateTime(string value) { DateTime result; return DateTime.TryParse(value, out result) ? (DateTime?) result : null; }
        /// <summary>
        ///     Returns the enum value represented by the specified string, or null if the string does not represent a valid enum
        ///     value.</summary>
        public static T? ParseEnum<T>(string value, bool ignoreCase = false) where T : struct { T result; return Enum.TryParse<T>(value, ignoreCase, out result) ? (T?) result : null; }

        /// <summary>
        ///     Creates a delegate using Action&lt;,*&gt; or Func&lt;,*&gt; depending on the number of parameters of the specified
        ///     method.</summary>
        /// <param name="firstArgument">
        ///     Object to call the method on, or null for static methods.</param>
        /// <param name="method">
        ///     The method to call.</param>
        public static Delegate CreateDelegate(object firstArgument, MethodInfo method)
        {
            var param = method.GetParameters();
            return Delegate.CreateDelegate(
                method.ReturnType == typeof(void)
                    ? param.Length == 0 ? typeof(Action) : actionType(param.Length).MakeGenericType(param.Select(p => p.ParameterType).ToArray())
                    : funcType(param.Length).MakeGenericType(param.Select(p => p.ParameterType).Concat(method.ReturnType).ToArray()),
                firstArgument,
                method
            );
        }

        private static Type funcType(int numParameters)
        {
            switch (numParameters)
            {
                case 0: return typeof(Func<>);
                case 1: return typeof(Func<,>);
                case 2: return typeof(Func<,,>);
                case 3: return typeof(Func<,,,>);
                case 4: return typeof(Func<,,,,>);
                case 5: return typeof(Func<,,,,,>);
                case 6: return typeof(Func<,,,,,,>);
                case 7: return typeof(Func<,,,,,,,>);
                case 8: return typeof(Func<,,,,,,,,>);
                case 9: return typeof(Func<,,,,,,,,,>);
                case 10: return typeof(Func<,,,,,,,,,,>);
                case 11: return typeof(Func<,,,,,,,,,,,>);
                case 12: return typeof(Func<,,,,,,,,,,,,>);
                case 13: return typeof(Func<,,,,,,,,,,,,,>);
                case 14: return typeof(Func<,,,,,,,,,,,,,,>);
                case 15: return typeof(Func<,,,,,,,,,,,,,,,>);
                case 16: return typeof(Func<,,,,,,,,,,,,,,,,>);
            }
            throw new ArgumentException("numParameters must be between 0 and 16.", "numParameters");
        }

        private static Type actionType(int numParameters)
        {
            switch (numParameters)
            {
                case 0: return typeof(Action);
                case 1: return typeof(Action<>);
                case 2: return typeof(Action<,>);
                case 3: return typeof(Action<,,>);
                case 4: return typeof(Action<,,,>);
                case 5: return typeof(Action<,,,,>);
                case 6: return typeof(Action<,,,,,>);
                case 7: return typeof(Action<,,,,,,>);
                case 8: return typeof(Action<,,,,,,,>);
                case 9: return typeof(Action<,,,,,,,,>);
                case 10: return typeof(Action<,,,,,,,,,>);
                case 11: return typeof(Action<,,,,,,,,,,>);
                case 12: return typeof(Action<,,,,,,,,,,,>);
                case 13: return typeof(Action<,,,,,,,,,,,,>);
                case 14: return typeof(Action<,,,,,,,,,,,,,>);
                case 15: return typeof(Action<,,,,,,,,,,,,,,>);
                case 16: return typeof(Action<,,,,,,,,,,,,,,,>);
            }
            throw new ArgumentException("numParameters must be between 0 and 16.", "numParameters");
        }

        /// <summary>
        ///     Executes the specified action if the current object is of the specified type, and the alternative action otherwise.</summary>
        /// <typeparam name="T">
        ///     Type the object must be to have the action executed.</typeparam>
        /// <param name="obj">
        ///     Object whose type is to be examined.</param>
        /// <param name="action">
        ///     Action to execute if <paramref name="obj"/> has the type <typeparamref name="T"/>.</param>
        /// <param name="elseAction">
        ///     Action to execute otherwise. If it's null, this action is not performed.</param>
        public static void IfType<T>(this object obj, Action<T> action, Action<object> elseAction = null)
        {
            if (obj is T)
            {
                action((T) obj);
            }
            else if (elseAction != null)
            {
                elseAction(obj);
            }
        }

        /// <summary>
        ///     Executes the specified function if the current object is of the specified type, and the alternative function otherwise.</summary>
        /// <typeparam name="T">
        ///     Type the object must be to have the action executed.</typeparam>
        /// <typeparam name="TResult">
        ///     Type of the result returned by the functions.</typeparam>
        /// <param name="obj">
        ///     Object whose type is to be examined.</param>
        /// <param name="function">
        ///     Action to execute if <paramref name="obj"/> has the type <typeparamref name="T"/>.</param>
        /// <param name="elseFunction">
        ///     Action to execute otherwise. If it's null, default(TResult) is returned.</param>
        /// <returns>
        ///     The result of the function called.</returns>
        public static TResult IfType<T, TResult>(this object obj, Func<T, TResult> function, Func<object, TResult> elseFunction = null)
        {
            if (obj is T)
            {
                return function((T) obj);
            }
            else if (elseFunction != null)
            {
                return elseFunction(obj);
            }
            else
            {
                return default(TResult);
            }
        }

        /// <summary>
        ///     Executes the specified action. If the action results in a file sharing violation exception, the action will be
        ///     repeatedly retried after a short delay (which increases after every failed attempt).</summary>
        /// <param name="action">
        ///     The action to be attempted and possibly retried.</param>
        /// <param name="maximum">
        ///     Maximum amount of time to keep retrying for. When expired, any sharing violation exception will propagate to the
        ///     caller of this method. Use null to retry indefinitely.</param>
        /// <param name="onSharingVio">
        ///     Action to execute when a sharing violation does occur (is called before the waiting).</param>
        public static void WaitSharingVio(Action action, TimeSpan? maximum = null, Action onSharingVio = null)
        {
            WaitSharingVio<bool>(() => { action(); return true; }, maximum, onSharingVio);
        }

        /// <summary>
        ///     Executes the specified function. If the function results in a file sharing violation exception, the function will
        ///     be repeatedly retried after a short delay (which increases after every failed attempt).</summary>
        /// <param name="func">
        ///     The function to be attempted and possibly retried.</param>
        /// <param name="maximum">
        ///     Maximum amount of time to keep retrying for. When expired, any sharing violation exception will propagate to the
        ///     caller of this method. Use null to retry indefinitely.</param>
        /// <param name="onSharingVio">
        ///     Action to execute when a sharing violation does occur (is called before the waiting).</param>
        public static T WaitSharingVio<T>(Func<T> func, TimeSpan? maximum = null, Action onSharingVio = null)
        {
            var started = DateTime.UtcNow;
            int sleep = 279;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (IOException ex)
                {
                    int hResult = 0;
                    try { hResult = (int) ex.GetType().GetProperty("HResult", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ex, null); }
                    catch { }
                    if (hResult != -2147024864) // 0x80070020 ERROR_SHARING_VIOLATION
                        throw;
                    if (onSharingVio != null)
                        onSharingVio();
                }

                if (maximum != null)
                {
                    int leftMs = (int) (maximum.Value - (DateTime.UtcNow - started)).TotalMilliseconds;
                    if (sleep > leftMs)
                    {
                        Thread.Sleep(leftMs);
                        return func(); // or throw the sharing vio exception
                    }
                }

                Thread.Sleep(sleep);
                sleep = Math.Min((sleep * 3) >> 1, 10000);
            }
        }

        /// <summary>
        ///     Given a set of values and a function that returns true when given this set, will efficiently remove items from
        ///     this set which are not essential for making the function return true. The relative order of items is preserved.
        ///     This method cannot generally guarantee that the result is optimal, but for some types of functions the result will
        ///     be guaranteed optimal.</summary>
        /// <typeparam name="T">
        ///     Type of the values in the set.</typeparam>
        /// <param name="items">
        ///     The set of items to reduce.</param>
        /// <param name="test">
        ///     The function that examines the set. Must always return the same value for the same set.</param>
        /// <param name="breadthFirst">
        ///     A value selecting a breadth-first or a depth-first approach. Depth-first is best at quickly locating a single
        ///     value which will be present in the final required set. Breadth-first is best at quickly placing a lower bound on
        ///     the total number of individual items in the required set.</param>
        /// <param name="skipConsistencyTest">
        ///     When the function is particularly slow, you might want to set this to true to disable calls which are not required
        ///     to reduce the set and are only there to ensure that the function behaves consistently.</param>
        /// <returns>
        ///     A hopefully smaller set of values that still causes the function to return true.</returns>
        public static IEnumerable<T> ReduceRequiredSet<T>(IEnumerable<T> items, Func<ReduceRequiredSetState<T>, bool> test, bool breadthFirst = false, bool skipConsistencyTest = false)
        {
            var state = new ReduceRequiredSetStateInternal<T>(items);

            if (!skipConsistencyTest)
                if (!test(state))
                    throw new Exception("The function does not return true for the original set.");

            while (state.AnyPartitions)
            {
                if (!skipConsistencyTest)
                    if (!test(state))
                        throw new Exception("The function is not consistently returning the same value for the same set, or there is an internal error in this algorithm.");

                var rangeToSplit = breadthFirst ? state.LargestRange : state.SmallestRange;
                int mid = (rangeToSplit.Item1 + rangeToSplit.Item2) / 2;
                var split1 = Tuple.Create(rangeToSplit.Item1, mid);
                var split2 = Tuple.Create(mid + 1, rangeToSplit.Item2);

                state.ApplyTemporarySplit(rangeToSplit, split1);
                if (test(state))
                {
                    state.RemoveRange(rangeToSplit);
                    state.AddRange(split1);
                    continue;
                }
                state.ApplyTemporarySplit(rangeToSplit, split2);
                if (test(state))
                {
                    state.RemoveRange(rangeToSplit);
                    state.AddRange(split2);
                    continue;
                }
                state.ResetTemporarySplit();
                state.RemoveRange(rangeToSplit);
                state.AddRange(split1);
                state.AddRange(split2);
            }

            state.ResetTemporarySplit();
            return state.SetToTest;
        }

        /// <summary>Encapsulates the state of the <see cref="Ut.ReduceRequiredSet"/> algorithm and exposes statistics about it.</summary>
        public abstract class ReduceRequiredSetState<T>
        {
            /// <summary>Internal; do not use.</summary>
            protected List<Tuple<int, int>> Ranges;
            /// <summary>Internal; do not use.</summary>
            protected List<T> Items;
            /// <summary>Internal; do not use.</summary>
            protected Tuple<int, int> ExcludedRange, IncludedRange;

            /// <summary>
            ///     Enumerates every item that is known to be in the final required set. "Definitely" doesn't mean that there
            ///     exists no subset resulting in "true" without these members. Rather, it means that the algorithm will
            ///     definitely return these values, and maybe some others too.</summary>
            public IEnumerable<T> DefinitelyRequired { get { return Ranges.Where(r => r.Item1 == r.Item2).Select(r => Items[r.Item1]); } }
            /// <summary>
            ///     Gets the current number of partitions containing uncertain items. The more of these, the slower the algorithm
            ///     will converge from here onwards.</summary>
            public int PartitionsCount { get { return Ranges.Count - Ranges.Count(r => r.Item1 == r.Item2); } }
            /// <summary>
            ///     Gets the number of items in the smallest partition. This is the value that is halved upon a successful
            ///     depth-first iteration.</summary>
            public int SmallestPartitionSize { get { return Ranges.Where(r => r.Item1 != r.Item2).Min(r => r.Item2 - r.Item1 + 1); } }
            /// <summary>
            ///     Gets the number of items in the largest partition. This is the value that is halved upon a successful
            ///     breadth-first iteration.</summary>
            public int LargestPartitionSize { get { return Ranges.Max(r => r.Item2 - r.Item1 + 1); } }
            /// <summary>Gets the total number of items about which the algorithm is currently undecided.</summary>
            public int ItemsRemaining { get { return Ranges.Where(r => r.Item1 != r.Item2).Sum(r => r.Item2 - r.Item1 + 1); } }

            /// <summary>Gets the set of items for which the function should be evaluated in the current step.</summary>
            public IEnumerable<T> SetToTest
            {
                get
                {
                    var ranges = Ranges.AsEnumerable();
                    if (ExcludedRange != null)
                        ranges = ranges.Where(r => r != ExcludedRange);
                    if (IncludedRange != null)
                        ranges = ranges.Concat(IncludedRange);
                    return ranges
                        .SelectMany(range => Enumerable.Range(range.Item1, range.Item2 - range.Item1 + 1))
                        .Order()
                        .Select(i => Items[i]);
                }
            }
        }

        internal sealed class ReduceRequiredSetStateInternal<T> : ReduceRequiredSetState<T>
        {
            public ReduceRequiredSetStateInternal(IEnumerable<T> items)
            {
                Items = items.ToList();
                Ranges = new List<Tuple<int, int>>();
                Ranges.Add(Tuple.Create(0, Items.Count - 1));
            }

            public bool AnyPartitions { get { return Ranges.Any(r => r.Item1 != r.Item2); } }
            public Tuple<int, int> LargestRange { get { return Ranges.MaxElement(t => t.Item2 - t.Item1); } }
            public Tuple<int, int> SmallestRange { get { return Ranges.Where(r => r.Item1 != r.Item2).MinElement(t => t.Item2 - t.Item1); } }

            public void AddRange(Tuple<int, int> range) { Ranges.Add(range); }
            public void RemoveRange(Tuple<int, int> range) { if (!Ranges.Remove(range)) throw new InvalidOperationException("Ut.ReduceRequiredSet has a bug. Code: 826432"); }

            public void ResetTemporarySplit()
            {
                ExcludedRange = IncludedRange = null;
            }
            public void ApplyTemporarySplit(Tuple<int, int> rangeToSplit, Tuple<int, int> splitRange)
            {
                ExcludedRange = rangeToSplit;
                IncludedRange = splitRange;
            }
        }
    }
}
