using System;
using System.Threading;

namespace RT.Util
{
    public static partial class Ut
    {
        /// <summary>
        /// Evaluates the specified code and returns its result.
        /// If the code throws an exception of the specified type, runs <paramref name="onException"/> instead.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="onException">The code to be executed in case of failure.</param>
        public static TResult OnException<TException, TResult>(Func<TResult> method, Func<TException, TResult> onException) where TException : Exception
        {
            try { return method(); }
            catch (TException e) { return onException(e); }
        }

        /// <summary>
        /// Evaluates the specified code.
        /// If the code throws any exceptions, catches and suppresses them.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <returns>True if the method returned without exceptions, false otherwise.</returns>
        public static bool OnExceptionIgnore(Action method)
        {
            try { method(); return true; }
            catch { return false; }
        }

        /// <summary>
        /// Evaluates the specified code and returns its result.
        /// If the code throws <typeparamref name="TException"/>, catches and suppresses it.
        /// Doesn't catch any other exceptions.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <returns>True if the method returned without exceptions, false if <typeparamref name="TException"/> was caught and suppressed.</returns>
        public static bool OnExceptionIgnore<TException>(Action method) where TException : Exception
        {
            try { method(); return true; }
            catch (TException) { return false; }
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
        /// Evaluates the specified code.
        /// If the code throws any exceptions, retries the specified number of times.
        /// The final attempt is executed without any exception handlers.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="attempts">The maximum number of times to retry the method before giving up.</param>
        /// <param name="delayMs">Delay, in milliseconds, before retrying the method.</param>
        public static void OnExceptionRetry(Action method, int attempts = 3, int delayMs = 333)
        {
            while (attempts > 1)
            {
                attempts--;
                try { method(); return; }
                catch { }
                Thread.Sleep(delayMs);
            }
            method();
        }

        /// <summary>
        /// Evaluates the specified code.
        /// If the code throws <typeparamref name="TException"/>, retries the specified number of times.
        /// The final attempt is executed without any exception handlers.
        /// </summary>
        /// <param name="method">The code to be executed.</param>
        /// <param name="attempts">The maximum number of times to retry the method before giving up.</param>
        /// <param name="delayMs">Delay, in milliseconds, before retrying the method.</param>
        public static void OnExceptionRetry<TException>(Action method, int attempts = 3, int delayMs = 333) where TException : Exception
        {
            while (attempts > 1)
            {
                attempts--;
                try { method(); return; }
                catch (TException) { }
                Thread.Sleep(delayMs);
            }
            method();
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
        /// <returns>True if the method returned without exceptions, false if an exception was caught and suppressed on every attempt.</returns>
        public static bool OnExceptionRetryThenIgnore(Action method, int attempts = 3, int delayMs = 333)
        {
            while (attempts > 1)
            {
                attempts--;
                try { method(); return true; }
                catch { }
                Thread.Sleep(delayMs);
            }
            try { method(); return true; }
            catch { return false; }
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
        /// <returns>True if the method returned without exceptions, false if <typeparamref name="TException"/> was caught and suppressed on every attempt.</returns>
        public static bool OnExceptionRetryThenIgnore<TException>(Action method, int attempts = 3, int delayMs = 333) where TException : Exception
        {
            while (attempts > 1)
            {
                attempts--;
                try { method(); return true; }
                catch (TException) { }
                Thread.Sleep(delayMs);
            }
            try { method(); return true; }
            catch (TException) { return false; }
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
            while (attempts > 1)
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
