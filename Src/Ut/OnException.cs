using System;
using System.Threading;

namespace RT.Util
{
    public static partial class Ut
    {
        /// <summary>
        ///     Evaluates the specified code and returns its result. If the code throws an exception of the specified type, runs
        ///     <paramref name="onException"/> instead.</summary>
        /// <param name="func">
        ///     The code to be executed.</param>
        /// <param name="onException">
        ///     The code to be executed in case of failure.</param>
        public static TResult OnException<TException, TResult>(Func<TResult> func, Func<TException, TResult> onException) where TException : Exception
        {
            try { return func(); }
            catch (TException e) { return onException(e); }
        }

        /// <summary>
        ///     Evaluates the specified code. If the code throws any exceptions, catches and suppresses them.</summary>
        /// <param name="action">
        ///     The code to be executed.</param>
        /// <returns>
        ///     True if <paramref name="action"/> returned without exceptions, false otherwise.</returns>
        public static bool OnExceptionIgnore(Action action)
        {
            try { action(); return true; }
            catch { return false; }
        }

        /// <summary>
        ///     Evaluates the specified code and returns its result. If the code throws <typeparamref name="TException"/>, catches
        ///     and suppresses it. Doesn't catch any other exceptions.</summary>
        /// <param name="action">
        ///     The code to be executed.</param>
        /// <returns>
        ///     True if <paramref name="action"/> returned without exceptions, false if <typeparamref name="TException"/> was caught and
        ///     suppressed.</returns>
        public static bool OnExceptionIgnore<TException>(Action action) where TException : Exception
        {
            try { action(); return true; }
            catch (TException) { return false; }
        }

        /// <summary>
        ///     Evaluates the specified code and returns its result. If the code throws any exceptions, returns <paramref
        ///     name="default"/> instead.</summary>
        /// <param name="func">
        ///     The code to be executed.</param>
        /// <param name="default">
        ///     Value to return in case of failure.</param>
        public static TResult OnExceptionDefault<TResult>(Func<TResult> func, TResult @default)
        {
            try { return func(); }
            catch { return @default; }
        }

        /// <summary>
        ///     Evaluates the specified code and returns its result. If the code throws <typeparamref name="TException"/>, returns
        ///     <paramref name="default"/> instead. Doesn't catch any other exceptions.</summary>
        /// <param name="func">
        ///     The code to be executed.</param>
        /// <param name="default">
        ///     Value to return in case of failure.</param>
        public static TResult OnExceptionDefault<TResult, TException>(Func<TResult> func, TResult @default) where TException : Exception
        {
            try { return func(); }
            catch (TException) { return @default; }
        }

        /// <summary>
        ///     Evaluates the specified code. If the code throws any exceptions, retries the specified number of times. The final
        ///     attempt is executed without any exception handlers.</summary>
        /// <param name="action">
        ///     The code to be executed.</param>
        /// <param name="attempts">
        ///     The maximum number of times to retry <paramref name="action"/> before giving up.</param>
        /// <param name="delayMs">
        ///     Delay, in milliseconds, before retrying <paramref name="action"/>.</param>
        /// <param name="onException">
        ///     Optional action to execute when an exception occurs and the waiting period starts.</param>
        public static void OnExceptionRetry(Action action, int attempts = 3, int delayMs = 333, Action onException = null)
        {
            while (attempts > 1)
            {
                attempts--;
                try { action(); return; }
                catch { }
                if (onException != null)
                    onException();
                Thread.Sleep(delayMs);
            }
            action();
        }

        /// <summary>
        ///     Evaluates the specified code. If the code throws <typeparamref name="TException"/>, retries the specified number
        ///     of times. The final attempt is executed without any exception handlers.</summary>
        /// <param name="action">
        ///     The code to be executed.</param>
        /// <param name="attempts">
        ///     The maximum number of times to retry <paramref name="action"/> before giving up.</param>
        /// <param name="delayMs">
        ///     Delay, in milliseconds, before retrying <paramref name="action"/>.</param>
        public static void OnExceptionRetry<TException>(Action action, int attempts = 3, int delayMs = 333) where TException : Exception
        {
            while (attempts > 1)
            {
                attempts--;
                try { action(); return; }
                catch (TException) { }
                Thread.Sleep(delayMs);
            }
            action();
        }

        /// <summary>
        ///     Evaluates the specified code and returns its result. If the code throws any exceptions, retries the specified
        ///     number of times. The final attempt is executed without any exception handlers.</summary>
        /// <param name="func">
        ///     The code to be executed.</param>
        /// <param name="attempts">
        ///     The maximum number of times to retry <paramref name="func"/> before giving up.</param>
        /// <param name="delayMs">
        ///     Delay, in milliseconds, before retrying <paramref name="func"/>.</param>
        /// <param name="onException">
        ///     Optional action to execute when an exception occurs and the waiting period starts.</param>
        public static TResult OnExceptionRetry<TResult>(Func<TResult> func, int attempts = 3, int delayMs = 333, Action onException = null)
        {
            while (attempts > 1)
            {
                attempts--;
                try { return func(); }
                catch { }
                if (onException != null)
                    onException();
                Thread.Sleep(delayMs);
            }
            return func();
        }

        /// <summary>
        ///     Evaluates the specified code and returns its result. If the code throws <typeparamref name="TException"/>, retries
        ///     the specified number of times. The final attempt is executed without any exception handlers.</summary>
        /// <param name="func">
        ///     The code to be executed.</param>
        /// <param name="attempts">
        ///     The maximum number of times to retry <paramref name="func"/> before giving up.</param>
        /// <param name="delayMs">
        ///     Delay, in milliseconds, before retrying <paramref name="func"/>.</param>
        public static TResult OnExceptionRetry<TResult, TException>(Func<TResult> func, int attempts = 3, int delayMs = 333) where TException : Exception
        {
            while (attempts > 1)
            {
                attempts--;
                try { return func(); }
                catch (TException) { }
                Thread.Sleep(delayMs);
            }
            return func();
        }

        /// <summary>
        ///     Evaluates the specified code. If the code throws any exceptions, retries the specified number of times. If the
        ///     code still throws on the final attempt, suppresses the exception.</summary>
        /// <param name="action">
        ///     The code to be executed.</param>
        /// <param name="attempts">
        ///     The maximum number of times to retry <paramref name="action"/> before giving up.</param>
        /// <param name="delayMs">
        ///     Delay, in milliseconds, before retrying <paramref name="action"/>.</param>
        /// <returns>
        ///     True if <paramref name="action"/> returned without exceptions, false if an exception was caught and suppressed on every
        ///     attempt.</returns>
        public static bool OnExceptionRetryThenIgnore(Action action, int attempts = 3, int delayMs = 333)
        {
            while (attempts > 1)
            {
                attempts--;
                try { action(); return true; }
                catch { }
                Thread.Sleep(delayMs);
            }
            try { action(); return true; }
            catch { return false; }
        }

        /// <summary>
        ///     Evaluates the specified code. If the code throws <typeparamref name="TException"/>, retries the specified number
        ///     of times. If the code still throws <typeparamref name="TException"/> on the final attempt, suppresses the
        ///     exception. Doesn't catch any other exceptions.</summary>
        /// <param name="action">
        ///     The code to be executed.</param>
        /// <param name="attempts">
        ///     The maximum number of times to retry <paramref name="action"/> before giving up.</param>
        /// <param name="delayMs">
        ///     Delay, in milliseconds, before retrying <paramref name="action"/>.</param>
        /// <returns>
        ///     True if <paramref name="action"/> returned without exceptions, false if <typeparamref name="TException"/> was caught and
        ///     suppressed on every attempt.</returns>
        public static bool OnExceptionRetryThenIgnore<TException>(Action action, int attempts = 3, int delayMs = 333) where TException : Exception
        {
            while (attempts > 1)
            {
                attempts--;
                try { action(); return true; }
                catch (TException) { }
                Thread.Sleep(delayMs);
            }
            try { action(); return true; }
            catch (TException) { return false; }
        }

        /// <summary>
        ///     Evaluates the specified code and returns its result. If the code throws any exceptions, retries the specified
        ///     number of times. If the code still throws on the final attempt, returns <paramref name="default"/>
        ///     instead.</summary>
        /// <param name="func">
        ///     The code to be executed.</param>
        /// <param name="default">
        ///     Value to return in case of failure.</param>
        /// <param name="attempts">
        ///     The maximum number of times to retry <paramref name="func"/> before giving up.</param>
        /// <param name="delayMs">
        ///     Delay, in milliseconds, before retrying <paramref name="func"/>.</param>
        public static TResult OnExceptionRetryThenDefault<TResult>(Func<TResult> func, TResult @default, int attempts = 3, int delayMs = 333)
        {
            while (attempts > 1)
            {
                attempts--;
                try { return func(); }
                catch { }
                Thread.Sleep(delayMs);
            }
            try { return func(); }
            catch { return @default; }
        }

        /// <summary>
        ///     Evaluates the specified code and returns its result. If the code throws <typeparamref name="TException"/>, retries
        ///     the specified number of times. If the code still throws <typeparamref name="TException"/> on the final attempt,
        ///     returns <paramref name="default"/> instead. Doesn't catch any other exceptions.</summary>
        /// <param name="func">
        ///     The code to be executed.</param>
        /// <param name="default">
        ///     Value to return in case of failure.</param>
        /// <param name="attempts">
        ///     The maximum number of times to retry <paramref name="func"/> before giving up.</param>
        /// <param name="delayMs">
        ///     Delay, in milliseconds, before retrying <paramref name="func"/>.</param>
        public static TResult OnExceptionRetryThenDefault<TResult, TException>(Func<TResult> func, TResult @default, int attempts = 3, int delayMs = 333) where TException : Exception
        {
            while (attempts > 1)
            {
                attempts--;
                try { return func(); }
                catch (TException) { }
                Thread.Sleep(delayMs);
            }
            try { return func(); }
            catch (TException) { return @default; }
        }
    }
}
