using System;
using System.Collections.Generic;
using System.Text;

namespace crozone.AsyncResetEvents
{
    /// <summary>
    /// Async compatible manual-reset event.
    /// </summary>
    public class AsyncManualResetEvent : AsyncResetEvent
    {
        /// <summary>
        /// Creates an async-compatible manual reset event.
        /// The event is initially unset.
        /// </summary>
        public AsyncManualResetEvent() : this(false) { }

        /// <summary>
        /// Creates an async-compatible manual reset event.
        /// </summary>
        /// <param name="set">If true, the event starts as set. If false, the event starts as unset.</param>
        public AsyncManualResetEvent(bool set) : base(set, false) { }
    }
}
