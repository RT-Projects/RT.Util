using System;
using System.Threading;

namespace RT.Util
{
    public static partial class Ut
    {
        /// <summary>
        /// Evaluates the specified code.
        /// If the code throws any exceptions, catches and suppresses them.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        public static void OnExceptionIgnore(Action method)
        {
            try { method(); }
            catch { }
        }

        /// <summary>
        /// Evaluates the specified code and returns its result.
        /// If the code throws <typeparamref name="TException"/>, catches and suppresses it.
        /// Doesn't catch any other exceptions.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        public static void OnExceptionIgnore<TException>(Action method) where TException : Exception
        {
            try { method(); }
            catch (TException) { }
        }

        /// <summary>
        /// Evaluates the specified code and returns its result.
        /// If the code throws any exceptions, returns <paramref name="default"/> instead.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="default">Value to return in case of failure.</param>
        public static TResult OnExceptionDefault<TResult>(Func<TResult> method, TResult @default)
        {
            try { return method(); }
            catch { return @default; }
        }

        /// <summary>
        /// Evaluates the specified code and returns its result.
        /// If the code throws <typeparamref name="TException"/>, returns <paramref name="default"/> instead.
        /// Doesn't catch any other exceptions.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="default">Value to return in case of failure.</param>
        public static TResult OnExceptionDefault<TResult, TException>(Func<TResult> method, TResult @default) where TException : Exception
        {
            try { return method(); }
            catch (TException) { return @default; }
        }

        /// <summary>
        /// Evaluates the specified code and returns its result.
        /// If the code throws any exceptions, retries the specified number of times.
        /// The final attempt is executed without any exception handlers.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="attempts">The maximum number of times to retry the method before giving up.</param>
        /// <param name="delayMs">Delay, in milliseconds, before retrying the method.</param>
        public static TResult OnExceptionRetry<TResult>(Func<TResult> method, int attempts = 3, int delayMs = 333)
        {
            while (attempts > 1)
            {
                attempts--;
                try { return method(); }
                catch { }
                Thread.Sleep(delayMs);
            }
            return method();
        }

        /// <summary>
        /// Evaluates the specified code and returns its result.
        /// If the code throws <typeparamref name="TException"/>, retries the specified number of times.
        /// The final attempt is executed without any exception handlers.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="attempts">The maximum number of times to retry the method before giving up.</param>
        /// <param name="delayMs">Delay, in milliseconds, before retrying the method.</param>
        public static TResult OnExceptionRetry<TResult, TException>(Func<TResult> method, int attempts = 3, int delayMs = 333) where TException : Exception
        {
            while (attempts > 1)
            {
                attempts--;
                try { return method(); }
                catch (TException) { }
                Thread.Sleep(delayMs);
            }
            return method();
        }

        /// <summary>
        /// Evaluates the specified code.
        /// If the code throws any exceptions, retries the specified number of times.
        /// If the code still throws on the final attempt, suppresses the exception.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="attempts">The maximum number of times to retry the method before giving up.</param>
        /// <param name="delayMs">Delay, in milliseconds, before retrying the method.</param>
        public static void OnExceptionRetryThenIgnore(Action method, int attempts = 3, int delayMs = 333)
        {
            while (attempts > 1)
            {
                attempts--;
                try { method(); }
                catch { }
                Thread.Sleep(delayMs);
            }
            try { method(); }
            catch { }
        }

        /// <summary>
        /// Evaluates the specified code.
        /// If the code throws <typeparamref name="TException"/>, retries the specified number of times.
        /// If the code still throws <typeparamref name="TException"/> on the final attempt, suppresses the exception.
        /// Doesn't catch any other exceptions.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="attempts">The maximum number of times to retry the method before giving up.</param>
        /// <param name="delayMs">Delay, in milliseconds, before retrying the method.</param>
        public static void OnExceptionRetryThenIgnore<TException>(Action method, int attempts = 3, int delayMs = 333) where TException : Exception
        {
            while (attempts > 1)
            {
                attempts--;
                try { method(); }
                catch (TException) { }
                Thread.Sleep(delayMs);
            }
            try { method(); }
            catch (TException) { }
        }

        /// <summary>
        /// Evaluates the specified code and returns its result.
        /// If the code throws any exceptions, retries the specified number of times.
        /// If the code still throws on the final attempt, returns <paramref name="default"/> instead.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="default">Value to return in case of failure.</param>
        /// <param name="attempts">The maximum number of times to retry the method before giving up.</param>
        /// <param name="delayMs">Delay, in milliseconds, before retrying the method.</param>
        public static TResult OnExceptionRetryThenDefault<TResult>(Func<TResult> method, TResult @default, int attempts = 3, int delayMs = 333)
        {
            while (attempts > 1)
            {
                attempts--;
                try { return method(); }
                catch { }
                Thread.Sleep(delayMs);
            }
            try { return method(); }
            catch { return @default; }
        }

        /// <summary>
        /// Evaluates the specified code and returns its result.
        /// If the code throws <typeparamref name="TException"/>, retries the specified number of times.
        /// If the code still throws <typeparamref name="TException"/> on the final attempt, returns <paramref name="default"/> instead.
        /// Doesn't catch any other exceptions.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="default">Value to return in case of failure.</param>
        /// <param name="attempts">The maximum number of times to retry the method before giving up.</param>
        /// <param name="delayMs">Delay, in milliseconds, before retrying the method.</param>
        public static TResult OnExceptionRetryThenDefault<TResult, TException>(Func<TResult> method, TResult @default, int attempts = 3, int delayMs = 333) where TException : Exception
        {
            while (attempts >= 1)
            {
                attempts--;
                try { return method(); }
                catch (TException) { }
                Thread.Sleep(delayMs);
            }
            try { return method(); }
            catch (TException) { return @default; }
        }
    }
}
