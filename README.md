# AsyncResetEvents
Async compatible auto and manual reset events.

## Async compatible reset events

These reset events aim to serve the same purpose as the .NET `AutoResetEvent` and `ManualResetEvent`, but with async methods.

This allows events to be awaited in a manner that doesn't block the thread, allowing easy, efficient, and threadsafe signalling and synchronization.

## Requirements

The library is written for NetStandard 2.0, and is entirely self-contained.

## Behaviour

TODO: accurately describe the implementation's behaviour in detail.

## Derivative code

The code is based on Stephen Toub's [async primatives implementation](https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-2-asyncautoresetevent/)

It is also based on Stephen Cleary's [AsyncEx library](https://github.com/StephenCleary/AsyncEx).

## Implementation

`AsyncAutoResetEvent` and `AsyncManualResetEvent` both share a common underlying implementation, contained within `AsyncResetEvent`. The `AsyncAutoResetEvent` and `AsyncManualResetEvent` classes themselves are really just thin convenience wrappers, and all they do is set a single boolean value within `AsyncResetEvent` to tweak its behaviour between manual and auto modes.

The motivation for this was to provide a straightforward, maintainable, and easy to test implementation. This leads to a slightly less efficient manual reset event, but (IMHO) easier to understand and maintain code.
