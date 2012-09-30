using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using RT.Util.ExtensionMethods;

namespace RT.Util.Threading
{
    /// <summary>Runs an arbitrary number of actions on a limited number of threads and aborts the ones that exceed a specified time limit.</summary>
    public sealed class TimeLimitedThreadPool
    {
        private Queue<TimeLimitedThread> _queue = new Queue<TimeLimitedThread>();
        private int _concurrentThreads = 2;
        private bool _threadsInitialised = false;
        private bool _background = false;
        private bool[] _isIdle = null;

        /// <summary>Fires when a thread is aborted due to exceeding its time limit.</summary>
        public event Action<TimeLimitedThread> Aborted;
        /// <summary>Fires when a thread completes within its time limit.</summary>
        public event Action<TimeLimitedThread> Completed;

        /// <summary>Constructor.</summary>
        /// <param name="concurrentThreads">Number of concurrent threads to run.</param>
        /// <param name="background">True if the threads should be designated background threads.</param>
        public TimeLimitedThreadPool(int concurrentThreads = 2, bool background = false)
        {
            _concurrentThreads = concurrentThreads;
            _background = background;
        }

        /// <summary>Waits for all currently running threads to finish.</summary>
        public void Wait()
        {
            if (_isIdle == null)
                return;
            lock (_isIdle)
            {
                while (true)
                {
                    while (!_isIdle.All(b => b) || _queue.Count > 0)
                        Monitor.Wait(_isIdle);
                }
            }
        }

        private void startThread(int i)
        {
            Action action = null;
            var startExecuting = new AutoResetEvent(false);
            var executionDone = new AutoResetEvent(false);

            var executionThread = new Thread(() =>
            {
                while (true)
                {
                    lock (_isIdle)
                    {
                        _isIdle[i] = true;
                        Monitor.PulseAll(_isIdle);
                    }
                    startExecuting.WaitOne();
                    lock (_isIdle)
                        _isIdle[i] = false;
                    action();
                    executionDone.Set();
                }
            });
            if (_background)
                executionThread.IsBackground = true;
            executionThread.Start();

            var controlThread = new Thread(() =>
            {
                while (true)
                {
                    TimeLimitedThread threadInfo;
                    lock (_queue)
                    {
                        while (_queue.Count == 0)
                            Monitor.Wait(_queue);
                        threadInfo = _queue.Dequeue();
                    }
                    action = threadInfo.ThreadAction;
                    startExecuting.Set();
                    if (!executionDone.WaitOne(threadInfo.TimeLimit))
                    {
                        // Time limit exceeded — abort the thread
                        executionThread.Abort();
                        if (Aborted != null)
                            Aborted(threadInfo);

                        // Start a new set of threads
                        startThread(i);
                        return;
                    }
                    if (Completed != null)
                        Completed(threadInfo);
                }
            });
            if (_background)
                controlThread.IsBackground = true;
            controlThread.Start();
        }

        private void startThreadsIfNecessary()
        {
            if (!_threadsInitialised)
                lock (_queue)
                    if (!_threadsInitialised)
                    {
                        _threadsInitialised = true;
                        _isIdle = new bool[_concurrentThreads];
                        for (int i = 0; i < _concurrentThreads; i++)
                        {
                            _isIdle[i] = true;
                            startThread(i);
                        }
                    }
        }

        private void enqueue(TimeLimitedThread thread)
        {
            startThreadsIfNecessary();
            lock (_queue)
            {
                _queue.Enqueue(thread);
                Monitor.PulseAll(_queue);
            }
        }

        private void enqueue(IEnumerable<TimeLimitedThread> threads)
        {
            startThreadsIfNecessary();
            lock (_queue)
            {
                foreach (var thread in threads)
                    _queue.Enqueue(thread);
                Monitor.PulseAll(_queue);
            }
        }

        /// <summary>Enqueues the specified thread, to be executed as soon as any thread is idle.</summary>
        /// <param name="thread">Specifies the thread and time limit.</param>
        public void EnqueueThread(TimeLimitedThread thread) { enqueue(thread); }
        /// <summary>Enqueues the specified thread with the specified time limit, to be executed as soon as any thread is idle.</summary>
        /// <param name="timeLimit">Specifies the time limit within which the action must finish or it will be aborted.
        /// The time limit counts from when the action starts executing.</param>
        /// <param name="action">The action to execute in a thread.</param>
        public void EnqueueThread(TimeSpan timeLimit, Action action) { enqueue(new TimeLimitedThread(timeLimit, action)); }

        /// <summary>Enqueues the specified threads, to be executed as soon as any thread is idle.</summary>
        /// <param name="threads">Specifies the threads and their time limits.</param>
        public void EnqueueThreads(IEnumerable<TimeLimitedThread> threads) { enqueue(threads); }
        /// <summary>Enqueues the specified threads, to be executed as soon as any thread is idle.</summary>
        /// <param name="threads">Specifies the threads and their time limits.</param>
        public void EnqueueThreads(params TimeLimitedThread[] threads) { enqueue(threads); }
        /// <summary>Enqueues the specified threads, to be executed as soon as any thread is idle.</summary>
        /// <param name="timeLimit">Specifies the time limit within which each action must finish or it will be aborted.
        /// The time limit counts from when the action starts executing.</param>
        /// <param name="actions">The actions to execute in a thread.</param>
        public void EnqueueThreads(TimeSpan timeLimit, IEnumerable<Action> actions) { enqueue(actions.Select(a => new TimeLimitedThread(timeLimit, a))); }
        /// <summary>Enqueues the specified threads, to be executed as soon as any thread is idle.</summary>
        /// <param name="timeLimit">Specifies the time limit within which each action must finish or it will be aborted.
        /// The time limit counts from when the action starts executing.</param>
        /// <param name="actions">The actions to execute in a thread.</param>
        public void EnqueueThreads(TimeSpan timeLimit, params Action[] actions) { enqueue(actions.Select(a => new TimeLimitedThread(timeLimit, a))); }
    }

    /// <summary>Encapsulates information about an action and a time limit.</summary>
    public sealed class TimeLimitedThread
    {
        /// <summary>Specifies the time limit within which the action must finish or it will be aborted.
        /// The time limit counts from when the action starts executing.</summary>
        public TimeSpan TimeLimit { get; private set; }
        /// <summary>The action to execute within the time limit.</summary>
        public Action ThreadAction { get; private set; }
        /// <summary>Constructor.</summary>
        /// <param name="timeLimit">Specifies the time limit within which the action must finish or it will be aborted.
        /// The time limit counts from when the action starts executing.</param>
        /// <param name="threadAction">The action to execute within the time limit.</param>
        public TimeLimitedThread(TimeSpan timeLimit, Action threadAction)
        {
            ThreadAction = threadAction;
            TimeLimit = timeLimit;
        }
    }
}
