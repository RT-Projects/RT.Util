using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

// Future ideas:
// - make it possible to change the TimeLimit of an enqueued task (fairly easy, just need to wake up the control thread if the task has started and is due before the sleep ends)
// - ExecuteTask helper method, for a single task, not even using the thread pool. Could return bool to indicate whether the task got completed or aborted.
// - ExecuteTasks helper method. Could use the calling thread as the control thread.

namespace RT.KitchenSink.Threading
{
    /// <summary>
    /// Runs tasks in separate threads, each with a time limit. Tasks exceeding the limit are automatically aborted. See Remarks for important limitations.
    /// </summary>
    /// <remarks>
    /// There are several major limitations that limit the usefulness of this code. First, the task code must be fully guarded against asynchronous exceptions.
    /// This precludes the use of much of the BCL, for example all collection classes. Second, if the task is stuck in a native method, it will not be aborted.
    /// Third, the code may deadlock in debug builds due to how the lock statement is JITted there.
    /// </remarks>
    public sealed class TimeLimitedThreadPool
    {
        /// <summary>Keeps track of how many worker threads the user requested when constructing this pool.</summary>
        private int _workerThreadCount;
        /// <summary>Keeps track of whether the user requested background or foreground threads when constructing this pool.</summary>
        private bool _foreground;
        /// <summary>The queue of tasks. Also acts as the lock object for synchronizing additions/removals from the queue and several other changes.</summary>
        private Queue<TimeLimitedTask> _queue;
        /// <summary>The array of worker descriptors. Each instance is created once and never replaced.</summary>
        private worker[] _workers;
        /// <summary>Set whenever all workers are idle and there are no tasks queued. Reset whenever a task gets queued.</summary>
        private ManualResetEvent _idle = new ManualResetEvent(true);
        /// <summary>Used for controlled shutdown of the control thread and the worker threads.</summary>
        private bool _shutdownControl = false, _shutdownWorkers = false;
        /// <summary>Used for waking up the control thread whenever it needs to check whether anything needs aborting or when the pool may have become idle.</summary>
        private AutoResetEvent _controlWakeup = new AutoResetEvent(false);
        /// <summary>Indicates how long the control thread is going to sleep, to avoid waking it up when there's no need to.</summary>
        private DateTime _controlSleepingUntil = DateTime.UtcNow + _indefinitely;
        /// <summary>Reference to the control thread.</summary>
        private Thread _controlThread;

        /// <summary>An approximation to sleeping indefinitely which doesn't require special-casing in arithmetic/comparisons</summary>
        private static TimeSpan _indefinitely = TimeSpan.FromDays(1);

        private class worker
        {
            /// <summary>The task this thread is currently executing. Is null to indicate that the worker is idle (but is possibly about to retrieve the next queued task).</summary>
            public TimeLimitedTask Task;
            /// <summary>The UTC time at which the current task should be aborted if it doesn't complete by then.</summary>
            public DateTime AbortTime;
            /// <summary>The thread on which the work is performed.</summary>
            public Thread Thread;
        }

        /// <summary>Wait on this handle after enqueueing tasks to find out when all tasks have been completed or aborted.</summary>
        public WaitHandle Idle { get { return _idle; } }

        /// <summary>Constructor.</summary>
        /// <param name="workerThreadCount">The number of worker threads to run. If zero, the number of processor cores will be used.</param>
        /// <param name="foreground">True to use foreground threads (which prevent a program from shutting down while executing).</param>
        public TimeLimitedThreadPool(int workerThreadCount = 0, bool foreground = false)
        {
            _workerThreadCount = workerThreadCount <= 0 ? Environment.ProcessorCount : workerThreadCount;
            _foreground = foreground;
        }

        private void startupIfNecessary()
        {
            if (_queue != null)
                return;
            _queue = new Queue<TimeLimitedTask>();
            _workers = new worker[_workerThreadCount];

            for (int i = 0; i < _workerThreadCount; i++)
            {
                _workers[i] = new worker();
                createWorkerThread(_workers[i]);
            }

            _controlThread = new Thread(controlThreadProc);
            _controlThread.IsBackground = !_foreground;
            _controlThread.Name = "TimeLimitedThreadPool Control " + _controlThread.GetHashCode();
            _controlThread.Start();
        }

        private void createWorkerThread(worker worker)
        {
            worker.Thread = new Thread(() => workerThreadProc(worker));
            worker.Thread.Name = "TimeLimitedThreadPool Worker " + worker.Thread.GetHashCode();
            worker.Thread.IsBackground = !_foreground;
            worker.Thread.Start();
        }

        private void controlThreadProc()
        {
            while (true)
            {
                var sleep = _controlSleepingUntil - DateTime.UtcNow + TimeSpan.FromMilliseconds(1); // otherwise the WaitOne call might end up not waiting at all, iterating through the loop far too often instead of waiting 1 ms.
                if (sleep.Ticks > 0)
                    _controlWakeup.WaitOne(sleep);
                lock (_queue) // guarantees that Task and StartTime are both valid w.r.t. each other; no tasks can start (change from null to non-null) while we're in this lock
                {
                    if (_shutdownControl)
                        return;
                    var now = DateTime.UtcNow;
                    _controlSleepingUntil = now + _indefinitely; // if no tasks are found to be in progress (and we know none can start while we're doing this) then wait indefinitely
                    bool allIdle = true;
                    foreach (var worker in _workers)
                    {
                        var task = worker.Task; // the task could complete while we're doing this, making this field null
                        if (task == null)
                            continue; // no task is currently active on this thread

                        if (worker.AbortTime < now)
                        {
                            worker.Thread.Abort();
                            if (worker.Task != null) // the occasional task will complete between the first check and the thread abort; no need to invoke the abort action for these since we know for sure they completed in their entirety
                            {
                                task.State = TimeLimitedTaskState.Aborted;
                                task.Aborted(); // access via the task variable since the Task field could still disappear from under our feet
                            }
                            worker.Task = null; // mark that there's no task on this thread
                            createWorkerThread(worker);
                        }
                        else
                        {
                            allIdle = false; // a newly created worker thread will also be idle, so only this case counts as non-idle
                            if (_controlSleepingUntil > worker.AbortTime)
                                _controlSleepingUntil = worker.AbortTime;
                        }
                    }
                    if (allIdle && _queue.Count == 0 && !_idle.WaitOne(0))
                    {
                        _idle.Set();
                        _controlWakeup.Reset(); // a task may have finished while we were doing stuff; we know for sure that a another iteration is not currently necessary
                    }
                }
            }
        }

        private void workerThreadProc(worker worker)
        {
            while (true)
            {
                lock (_queue)
                {
                    // The control thread might now need to indicate that the pool has become idle. This can only occur if the queue is empty and every worker is idle.
                    if (_queue.Count == 0)
                        _controlWakeup.Set(); // the queue is indeed empty and _this_ worker has just become idle, so a check is needed
                    // Wait for work
                    while (_queue.Count == 0)
                    {
                        if (_shutdownWorkers)
                            return;
                        Monitor.Wait(_queue); // either we've run out of tasks, or PulseAll woke up too many threads for the job
                    }
                    // Initiate the next task
                    worker.Task = _queue.Dequeue();
                    worker.Task.State = TimeLimitedTaskState.Started;
                    worker.AbortTime = DateTime.UtcNow + worker.Task.TimeLimit;
                }
                // ThreadAbortException can occur from here onwards, and never inside the lock (including Monitor.Wait, because Task is then guaranteed null)

                // The control thread now needs to update its sleeping time, but only if the current task is due to be aborted before the current sleep expires
                if (_controlSleepingUntil > worker.AbortTime)
                    _controlWakeup.Set();

                worker.Task.Execute();

                var task = worker.Task;
                worker.Task = null; // First mark the order as idle
                task.State = TimeLimitedTaskState.Completed; // Only then mark the task as completed. This matters in the Shutdown method
            }
        }

        /// <summary>
        /// Shuts down all threads owned by the thread pool. Empties the queue of tasks and forcefully aborts any tasks currently in progress.
        /// Blocks until the shutdown has completed. May be called multiple times in a row. The threads will be recreated automatically the next
        /// time one of the task-enqueueing methods is called. Must be synchronized with any calls to task enqueueing methods.
        /// </summary>
        public void Shutdown()
        {
            if (_queue == null)
                return;

            // First shut down the control thread, to make sure it doesn't kick off new workers in case it happens to abort something while we're doing this
            lock (_queue)
            {
                foreach (var task in _queue)
                    task.State = TimeLimitedTaskState.Cancelled;
                _queue.Clear();
                _shutdownControl = true;
                // Now the control thread is definitely outside the lock, either before or after the wait. Force it to skip/leave the wait.
                _controlWakeup.Set();
            }
            // The thread will now enter the lock and see that a shutdown is requested
            _controlThread.Join();

            // Now shut down the worker threads
            _shutdownWorkers = true;
            lock (_queue)
            {
                // First abort those that are doing work
                foreach (var worker in _workers)
                {
                    var task = worker.Task;
                    if (task != null)
                    {
                        task.State = TimeLimitedTaskState.Aborted;
                        worker.Thread.Abort(); // cannot happen inside the worker thread's lock (_queue), including the Monitor.Wait
                    }
                }
                // Now do an orderly shutdown of any idle workers, to maintain the absence of Thread.Aborts inside the lock (_queue) section there
                Monitor.PulseAll(_queue);
            }
            // And wait until they've all stopped
            foreach (var worker in _workers)
                worker.Thread.Join();

            // Update state to reflect that we're shut down
            _queue = null;
            _workers = null;
            _controlThread = null;
            _shutdownControl = _shutdownWorkers = false;
            _controlSleepingUntil = DateTime.UtcNow + _indefinitely;
            _controlWakeup.Reset();
        }

        /// <summary>Places a time-limited task into the execution queue, which will start executing as soon as a worker is available
        /// and all tasks queued earlier are completed. If the pool was shut down, it will be started up automatically.
        /// Must be synchronized with <see cref="Shutdown"/>.</summary>
        public void EnqueueTask(TimeLimitedTask task)
        {
            startupIfNecessary();
            lock (_queue)
            {
                _queue.Enqueue(task);
                task.State = TimeLimitedTaskState.Enqueued;
                _idle.Reset();
                Monitor.Pulse(_queue);
            }
        }

        /// <summary>Places a number of time-limited tasks into the execution queue, which will start executing as soon as a worker is available
        /// and all tasks queued earlier are completed. If the pool was shut down, it will be started up automatically.
        /// Must be synchronized with <see cref="Shutdown"/>.</summary>
        public void EnqueueTasks(IEnumerable<TimeLimitedTask> tasks)
        {
            startupIfNecessary();
            lock (_queue)
            {
                foreach (var task in tasks)
                {
                    _queue.Enqueue(task);
                    task.State = TimeLimitedTaskState.Enqueued;
                }
                _idle.Reset();
                Monitor.PulseAll(_queue);
            }
        }

        /// <summary>Places a time-limited task into the execution queue, which will start executing as soon as a worker is available
        /// and all tasks queued earlier are completed. If the pool was shut down, it will be started up automatically.
        /// Must be synchronized with <see cref="Shutdown"/>.</summary>
        public void EnqueueTask(TimeSpan timeLimit, Action action, Action abortAction = null)
        {
            EnqueueTask(new TimeLimitedDelegateTask(timeLimit, action, abortAction));
        }

        /// <summary>Places a number of time-limited tasks into the execution queue, which will start executing as soon as a worker is available
        /// and all tasks queued earlier are completed. If the pool was shut down, it will be started up automatically.
        /// Must be synchronized with <see cref="Shutdown"/>.</summary>
        public void EnqueueTasks(TimeSpan timeLimit, IEnumerable<Action> action, Action abortAction = null)
        {
            EnqueueTasks(action.Select(a => new TimeLimitedDelegateTask(timeLimit, a, abortAction)));
        }
    }

    /// <summary>Represents the state of a time-limited task.</summary>
    public enum TimeLimitedTaskState
    {
        /// <summary>The task has been created, but not yet enqueued.</summary>
        Created,
        /// <summary>The task has been enqueued, but hasn't started yet.</summary>
        Enqueued,
        /// <summary>The task has started executing, but is not yet completed, aborted or cancelled.</summary>
        Started,
        /// <summary>The task has completed.</summary>
        Completed,
        /// <summary>The task has been aborted due to time-out.</summary>
        Aborted,
        /// <summary>The task has been queued for execution, but was cancelled (e.g. by shutting down the pool forcefully) before it begun.</summary>
        Cancelled,
    }

    /// <summary>Encapsulates a time-limited task used with the <see cref="TimeLimitedThreadPool"/>.</summary>
    public abstract class TimeLimitedTask
    {
        /// <summary>The maximum amount of time this task may take from the point when it begins executing. Tasks exceeding this limit are automatically aborted.</summary>
        public TimeSpan TimeLimit { get; private set; }
        /// <summary>Invoked to execute this task. This code must be hardened against the asynchronous <see cref="ThreadAbortException"/>.</summary>
        public abstract void Execute();
        /// <summary>Invoked if the task has been aborted due to time-out (but not cancelled or aborted due to a Shutdown).</summary>
        public abstract void Aborted();
        /// <summary>Invoked whenever the state of this task changes.</summary>
        /// <param name="old">The state of the task before this change.</param>
        public virtual void StateChanged(TimeLimitedTaskState old) { }
        /// <summary>Gets the current state of this task.</summary>
        public TimeLimitedTaskState State
        {
            get { return _state; }
            internal set
            {
                if (_state == value) return;
                var old = _state;
                _state = value;
                StateChanged(old);
            }
        }
        private TimeLimitedTaskState _state = TimeLimitedTaskState.Created;

        /// <summary>Constructor.</summary>
        /// <param name="timeLimit">The maximum amount of time this task may take from the point when it begins executing. Tasks exceeding this limit are automatically aborted.</param>
        public TimeLimitedTask(TimeSpan timeLimit)
        {
            if (timeLimit < TimeSpan.Zero)
                throw new ArgumentException("Time limit must not be negative", "timeLimit");
            TimeLimit = timeLimit;
        }
    }

    /// <summary>Implements an <see cref="Action"/>-based time-limited task for use with the <see cref="TimeLimitedThreadPool"/>.</summary>
    public class TimeLimitedDelegateTask : TimeLimitedTask
    {
        private Action _action, _abortAction;

        /// <summary>Constructor.</summary>
        /// <param name="timeLimit">The maximum amount of time this task may take from the point when it begins executing. Tasks exceeding this limit are automatically aborted.</param>
        /// <param name="action">The action invoked to perform the task in question. This code must be hardened against the asynchronous <see cref="ThreadAbortException"/>.</param>
        /// <param name="abortAction">The action invoked if the task has been aborted due to time-out (but not cancelled or aborted due to a Shutdown).</param>
        public TimeLimitedDelegateTask(TimeSpan timeLimit, Action action, Action abortAction = null)
            : base(timeLimit)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            _action = action;
            _abortAction = abortAction;
        }

        /// <summary>Override; see base.</summary>
        public override void Execute() { _action(); }
        /// <summary>Override; see base.</summary>
        public override void Aborted() { if (_abortAction != null) _abortAction(); }
    }
}
