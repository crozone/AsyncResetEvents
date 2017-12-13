namespace crozone.AsyncResetEvents
{
    /// <summary>
    /// Async compatible auto-reset event.
    /// </summary>
    public class AsyncAutoResetEvent : AsyncResetEvent
    {
        /// <summary>
        /// Creates an async-compatible auto reset event.
        /// The event is initially unset.
        /// </summary>
        public AsyncAutoResetEvent() : this(false) { }

        /// <summary>
        /// Creates an async-compatible auto reset event.
        /// </summary>
        /// <param name="set">If true, the event starts as set. If false, the event starts as unset.</param>
        public AsyncAutoResetEvent(bool set) : base(set, true) { }
    }
}
