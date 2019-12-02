using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace crozone.AsyncResetEvents
{
    /// <summary>
    /// Async compatible reset event.
    /// Can function as a Manual Reset Event or an Auto Reset Event.
    /// </summary>
    public class AsyncResetEvent
    {
        /// <summary>
        /// If this is true, this is an auto-reset event, and will reset after each wait.
        /// </summary>
        private readonly bool autoReset;

        /// <summary>
        /// The object used for all locking.
        /// </summary>
        private readonly object mainLock = new object();

        /// <summary>
        /// The queue of TaskCompletionSources that other tasks are awaiting.
        /// </summary>
        private readonly Queue<TaskCompletionSource<bool>> waitQueue = new Queue<TaskCompletionSource<bool>>();

        /// <summary>
        /// The current state of the event.
        /// </summary>
        private volatile bool eventSet;

        /// <summary>
        /// A cached completed task.
        /// </summary>   
        private readonly static Task completeTask = Task.FromResult(true);

        /// <summary>
        /// Creates an async-compatible reset event.
        /// </summary>
        /// <param name="set">If true, the event starts as set. If false, the event starts as unset.</param>
        /// <param name="autoReset">If true, functions as an auto-reset event. If false, functions as a manual reset event.</param>
        public AsyncResetEvent(bool set, bool autoReset)
        {
            this.eventSet = set;
            this.autoReset = autoReset;
        }

        /// <summary>
        /// Asynchronously waits for this event to be set. If the event is set, this method will auto-reset it and return immediately.
        /// </summary>
        public Task WaitAsync()
        {
            return WaitAsync(CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously waits for this event to be set.
        /// If the wait is canceled, then it will not auto-reset this event.
        /// </summary>
        /// <param name="timeout">The amount of time to wait before cancelling the wait.</param>
        public async Task<bool> WaitAsync(TimeSpan timeout)
        {
            try
            {
                using (CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout))
                {
                    await WaitAsync(timeoutSource.Token);
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        /// <summary>
        /// Asynchronously waits for this event to be set.
        /// If the wait is canceled, then it will not auto-reset this event.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel this wait.</param>
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            // Fast path if token is already cancelled.
            //
            if (cancellationToken.IsCancellationRequested)
            {
#if NET45
                var tcs = new TaskCompletionSource<bool>();
                tcs.TrySetCanceled();
                return tcs.Task;
#else
                return Task.FromCanceled(cancellationToken);
#endif
            }

            lock (mainLock)
            {
                if (eventSet)
                {
                    // If the event is already set, act immediately,
                    // and return a completed task. No queueing is necessary here.
                    //
                    // If we're an auto-reset event, set the event to false.
                    //
                    if (autoReset)
                    {
                        eventSet = false;
                    }

                    return completeTask;
                }
                else
                {
                    // If the event is unset, queue a completion source on the queue,
                    // and return a task from it.
                    //
                    // When set is called, it will dequeue the completion task, and
                    // transition it to completed, thereby completing the task.
                    //
#if NET45
                    TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
#else
                    // Run the continuation asynchronously if the current API level supports it.
                    // Otherwise, the continuation will be run synchronously by default, which can cause
                    // TaskCompletionSource.SetResult() to block while it executes the remainder of the completion (!).
                    //
                    TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif

                    // Cancel the task if the cancellation token is triggered.
                    //
                    // If the token is already cancelled before this point, the
                    // method will run immediately and synchronously on this thread.
                    //
                    CancellationTokenRegistration registration = cancellationToken.Register(() =>
                    {
                        lock (mainLock)
                        {
                            // Use TrySetCanceled so that even if the task is already completed,
                            // we don't throw an exception.
                            //
#if NET45
                            completionSource.TrySetCanceled();
#else
                            completionSource.TrySetCanceled(cancellationToken);
#endif
                        }
                    }, useSynchronizationContext: false);

                    // Enqueue the task completion source so that Set()
                    // can later dequeue it and transition it to completed.
                    //
                    waitQueue.Enqueue(completionSource);

                    // After the task has finished, dispose of the cancellation token
                    // registration that we created.
                    //
                    completionSource.Task.ContinueWith(
                            (_) => registration.Dispose(),
                            CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default
                            );

                    // Return a task that will complete when the completion source is triggered.
                    //
                    return completionSource.Task;
                }
            }
        }

        /// <summary>
        /// Resets the event.
        /// </summary>
        public void Reset()
        {
            lock (mainLock)
            {
                eventSet = false;
            }
        }

        /// <summary>
        /// Sets the event, atomically completing a task returned by <see cref="o:WaitAsync"/>.
        /// If the event is already set, this method does nothing.
        /// </summary>
        public void Set()
        {
            lock (mainLock)
            {
                while (waitQueue.Count > 0)
                {
                    // Dequeue waiters from the queue.
                    //
                    TaskCompletionSource<bool> toRelease = waitQueue.Dequeue();

                    // If the task was already completed, it means that it reached the RanToCompletion, Faulted, or Canceled state.
                    //
                    // This is probably because the WaitAsync cancellation token was triggered.
                    //
                    // In any case, nobody is waiting on this task anymore.
                    //
                    // Ignore this task and move on to cancelling the next one.
                    //
                    if (toRelease.Task.IsCompleted)
                    {
                        continue;
                    }

                    // Try and set the task to completed, with a result of true.
                    //
                    // We use TrySetResult because we don't really care if this is successful or not.
                    //
                    toRelease.TrySetResult(true);

                    // In an auto-reset event, we either want to release just one waiter from the queue,
                    // complete it, and then return without changing the value of eventSet.
                    //
                    // Therefore, return after the first loop in an autoreset event.
                    //
                    if (autoReset)
                    {
                        return;
                    }
                }

                // We're out of things to reset in the queue.
                //
                // If we reached this point, we're either setting a manual-reset event and have finished
                // clearing the wait queue, or we're setting an auto-reset event and the queue was empty.
                //
                // Set the event to true.
                //
                eventSet = true;
            }
        }
    }
}
