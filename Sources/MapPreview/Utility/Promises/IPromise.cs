﻿/*
 
Modified version of: https://github.com/UnlimitedHugs/RimworldMapReroll/blob/master/Source/Promises/IPromise.cs

MIT License

Copyright (c) 2014 Real Serious Games, modifications (c) 2017 UnlimitedHugs and (c) 2022 m00nl1ght <https://github.com/m00nl1ght-dev>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

 */

using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable All

namespace MapPreview.Promises
{
    /// <summary>
    ///     Implements a non-generic C# promise, this is a promise that simply resolves without delivering a value.
    ///     https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public interface IPromise
    {
        /// <summary>
        ///     ID of the promise, useful for debugging.
        /// </summary>
        int Id { get; }

        /// <summary>
        ///     Set the name of the promise, useful for debugging.
        /// </summary>
        IPromise WithName(string name);

        /// <summary>
        ///     Completes the promise.
        ///     onResolved is called on successful completion.
        ///     onRejected is called on error.
        /// </summary>
        void Done(Action onResolved, Action<Exception> onRejected);

        /// <summary>
        ///     Completes the promise.
        ///     onResolved is called on successful completion.
        ///     Adds a default error handler.
        /// </summary>
        void Done(Action onResolved);

        /// <summary>
        ///     Complete the promise. Adds a default error handler.
        /// </summary>
        void Done();

        /// <summary>
        ///     Handle errors for the promise.
        /// </summary>
        IPromise Catch(Action<Exception> onRejected);

        /// <summary>
        ///     Add a resolved callback that chains a value promise (optionally converting to a different value type).
        /// </summary>
        IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved);

        /// <summary>
        ///     Add a resolved callback that chains a non-value promise.
        /// </summary>
        IPromise Then(Func<IPromise> onResolved);

        /// <summary>
        ///     Add a resolved callback.
        /// </summary>
        IPromise Then(Action onResolved);

        /// <summary>
        ///     Add a resolved callback and a rejected callback.
        ///     The resolved callback chains a value promise (optionally converting to a different value type).
        /// </summary>
        IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved, Action<Exception> onRejected);

        /// <summary>
        ///     Add a resolved callback and a rejected callback.
        ///     The resolved callback chains a non-value promise.
        /// </summary>
        IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected);

        /// <summary>
        ///     Add a resolved callback and a rejected callback.
        /// </summary>
        IPromise Then(Action onResolved, Action<Exception> onRejected);

        /// <summary>
        ///     Chain an enumerable of promises, all of which must resolve.
        ///     The resulting promise is resolved when all of the promises have resolved.
        ///     It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        IPromise ThenAll(Func<IEnumerable<IPromise>> chain);

        /// <summary>
        ///     Chain an enumerable of promises, all of which must resolve.
        ///     Converts to a non-value promise.
        ///     The resulting promise is resolved when all of the promises have resolved.
        ///     It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain);

        /// <summary>
        ///     Chain a sequence of operations using promises.
        ///     Reutrn a collection of functions each of which starts an async operation and yields a promise.
        ///     Each function will be called and each promise resolved in turn.
        ///     The resulting promise is resolved after each promise is resolved in sequence.
        /// </summary>
        IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain);

        /// <summary>
        ///     Takes a function that yields an enumerable of promises.
        ///     Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        IPromise ThenRace(Func<IEnumerable<IPromise>> chain);

        /// <summary>
        ///     Takes a function that yields an enumerable of promises.
        ///     Converts to a value promise.
        ///     Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        IPromise<ConvertedT> ThenRace<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain);

        /// <summary>
        ///     Add a finally callback.
        ///     Finally callbacks will always be called, even if any preceding promise is rejected, or encounters an error.
        /// </summary>
        IPromise Finally(Action onComplete);

        /// <summary>
        ///     Add a finally callback that chains a non-value promise.
        /// </summary>
        IPromise Finally(Func<IPromise> onResolved);

        /// <summary>
        ///     Add a finally callback.
        ///     Finally callbacks will always be called, even if any preceding promise is rejected, or encounters an error.
        /// </summary>
        IPromise<ConvertedT> Finally<ConvertedT>(Func<IPromise<ConvertedT>> onComplete);
    }

    /// <summary>
    ///     Interface for a promise that can be rejected or resolved.
    /// </summary>
    public interface IPendingPromise : IRejectable
    {
        /// <summary>
        ///     ID of the promise, useful for debugging.
        /// </summary>
        int Id { get; }

        /// <summary>
        ///     Resolve the promise with a particular value.
        /// </summary>
        void Resolve();
    }

    /// <summary>
    ///     Used to list information of pending promises.
    /// </summary>
    public interface IPromiseInfo
    {
        /// <summary>
        ///     Id of the promise.
        /// </summary>
        int Id { get; }

        /// <summary>
        ///     Human-readable name for the promise.
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    ///     Arguments to the UnhandledError event.
    /// </summary>
    public class ExceptionEventArgs : EventArgs
    {
        internal ExceptionEventArgs(Exception exception)
        {
            //            Argument.NotNull(() => exception);

            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }

    /// <summary>
    ///     Represents a handler invoked when the promise is rejected.
    /// </summary>
    public struct RejectHandler
    {
        /// <summary>
        ///     Callback fn.
        /// </summary>
        public Action<Exception> callback;

        /// <summary>
        ///     The promise that is rejected when there is an error while invoking the handler.
        /// </summary>
        public IRejectable rejectable;
    }

    /// <summary>
    ///     Implements a non-generic C# promise, this is a promise that simply resolves without delivering a value.
    ///     https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public class Promise : IPromise, IPendingPromise, IPromiseInfo
    {
        /// <summary>
        ///     Set to true to enable tracking of promises.
        /// </summary>
        public static bool EnablePromiseTracking = false;

        private static EventHandler<ExceptionEventArgs> unhandlerException;

        /// <summary>
        ///     Id for the next promise that is created.
        /// </summary>
        private static int nextPromiseId;

        /// <summary>
        ///     Information about pending promises.
        /// </summary>
        internal static HashSet<IPromiseInfo> pendingPromises = new HashSet<IPromiseInfo>();

        /// <summary>
        ///     Error handlers.
        /// </summary>
        private List<RejectHandler> rejectHandlers;

        /// <summary>
        ///     The exception when the promise is rejected.
        /// </summary>
        private Exception rejectionException;

        /// <summary>
        ///     Completed handlers that accept no value.
        /// </summary>
        private List<ResolveHandler> resolveHandlers;

        public Promise()
        {
            CurState = PromiseState.Pending;
            Id = NextId();
            if (EnablePromiseTracking) pendingPromises.Add(this);
        }

        public Promise(Action<Action, Action<Exception>> resolver)
        {
            CurState = PromiseState.Pending;
            Id = NextId();
            if (EnablePromiseTracking) pendingPromises.Add(this);

            try
            {
                resolver(
                    // Resolve
                    () => Resolve(),

                    // Reject
                    ex => Reject(ex)
                );
            }
            catch (Exception ex)
            {
                Reject(ex);
            }
        }

        /// <summary>
        ///     Tracks the current state of the promise.
        /// </summary>
        public PromiseState CurState { get; private set; }

        /// <summary>
        ///     Reject the promise with an exception.
        /// </summary>
        public void Reject(Exception ex)
        {
            //            Argument.NotNull(() => ex);

            if (CurState != PromiseState.Pending)
                throw new ApplicationException("Attempt to reject a promise that is already in state: " + CurState +
                                               ", a promise can only be rejected when it is still in state: " +
                                               PromiseState.Pending);

            rejectionException = ex;
            CurState = PromiseState.Rejected;

            if (EnablePromiseTracking) pendingPromises.Remove(this);

            InvokeRejectHandlers(ex);
        }

        /// <summary>
        ///     Resolve the promise with a particular value.
        /// </summary>
        public void Resolve()
        {
            if (CurState != PromiseState.Pending)
                throw new ApplicationException("Attempt to resolve a promise that is already in state: " + CurState +
                                               ", a promise can only be resolved when it is still in state: " +
                                               PromiseState.Pending);

            CurState = PromiseState.Resolved;

            if (EnablePromiseTracking) pendingPromises.Remove(this);

            InvokeResolveHandlers();
        }

        /// <summary>
        ///     ID of the promise, useful for debugging.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        ///     Completes the promise.
        ///     onResolved is called on successful completion.
        ///     onRejected is called on error.
        /// </summary>
        public void Done(Action onResolved, Action<Exception> onRejected)
        {
            Then(onResolved, onRejected)
                .Catch(ex =>
                    PropagateUnhandledException(this, ex)
                );
        }

        /// <summary>
        ///     Completes the promise.
        ///     onResolved is called on successful completion.
        ///     Adds a default error handler.
        /// </summary>
        public void Done(Action onResolved)
        {
            Then(onResolved)
                .Catch(ex =>
                    PropagateUnhandledException(this, ex)
                );
        }

        /// <summary>
        ///     Complete the promise. Adds a defualt error handler.
        /// </summary>
        public void Done()
        {
            Catch(ex =>
                PropagateUnhandledException(this, ex)
            );
        }

        /// <summary>
        ///     Set the name of the promise, useful for debugging.
        /// </summary>
        public IPromise WithName(string name)
        {
            Name = name;
            return this;
        }

        /// <summary>
        ///     Handle errors for the promise.
        /// </summary>
        public IPromise Catch(Action<Exception> onRejected)
        {
            //            Argument.NotNull(() => onRejected);

            var resultPromise = new Promise();
            resultPromise.WithName(Name);

            Action resolveHandler = () => { resultPromise.Resolve(); };

            Action<Exception> rejectHandler = ex =>
            {
                onRejected(ex);

                resultPromise.Reject(ex);
            };

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);

            return resultPromise;
        }

        /// <summary>
        ///     Add a resolved callback that chains a value promise (optionally converting to a different value type).
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved)
        {
            return Then(onResolved, null);
        }

        /// <summary>
        ///     Add a resolved callback that chains a non-value promise.
        /// </summary>
        public IPromise Then(Func<IPromise> onResolved)
        {
            return Then(onResolved, null);
        }

        /// <summary>
        ///     Add a resolved callback.
        /// </summary>
        public IPromise Then(Action onResolved)
        {
            return Then(onResolved, null);
        }

        /// <summary>
        ///     Add a resolved callback and a rejected callback.
        ///     The resolved callback chains a value promise (optionally converting to a different value type).
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved, Action<Exception> onRejected)
        {
            // This version of the function must supply an onResolved.
            // Otherwise there is now way to get the converted value to pass to the resulting promise.
            //            Argument.NotNull(() => onResolved);

            var resultPromise = new Promise<ConvertedT>();
            resultPromise.WithName(Name);

            Action resolveHandler = () =>
            {
                onResolved()
                    .Then(
                        // Should not be necessary to specify the arg type on the next line, but Unity (mono) has an internal compiler error otherwise.
                        chainedValue => resultPromise.Resolve(chainedValue),
                        ex => resultPromise.Reject(ex)
                    );
            };

            Action<Exception> rejectHandler = ex =>
            {
                if (onRejected != null) onRejected(ex);

                resultPromise.Reject(ex);
            };

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);

            return resultPromise;
        }

        /// <summary>
        ///     Add a resolved callback and a rejected callback.
        ///     The resolved callback chains a non-value promise.
        /// </summary>
        public IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected)
        {
            var resultPromise = new Promise();
            resultPromise.WithName(Name);

            Action resolveHandler = () =>
            {
                if (onResolved != null)
                    onResolved()
                        .Then(
                            () => resultPromise.Resolve(),
                            ex => resultPromise.Reject(ex)
                        );
                else resultPromise.Resolve();
            };

            Action<Exception> rejectHandler = ex =>
            {
                if (onRejected != null) onRejected(ex);

                resultPromise.Reject(ex);
            };

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);

            return resultPromise;
        }

        /// <summary>
        ///     Add a resolved callback and a rejected callback.
        /// </summary>
        public IPromise Then(Action onResolved, Action<Exception> onRejected)
        {
            var resultPromise = new Promise();
            resultPromise.WithName(Name);

            Action resolveHandler = () =>
            {
                if (onResolved != null) onResolved();

                resultPromise.Resolve();
            };

            Action<Exception> rejectHandler = ex =>
            {
                if (onRejected != null) onRejected(ex);

                resultPromise.Reject(ex);
            };

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);

            return resultPromise;
        }

        /// <summary>
        ///     Chain an enumerable of promises, all of which must resolve.
        ///     The resulting promise is resolved when all of the promises have resolved.
        ///     It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        public IPromise ThenAll(Func<IEnumerable<IPromise>> chain)
        {
            return Then(() => All(chain()));
        }

        /// <summary>
        ///     Chain an enumerable of promises, all of which must resolve.
        ///     Converts to a non-value promise.
        ///     The resulting promise is resolved when all of the promises have resolved.
        ///     It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        public IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain)
        {
            return Then(() => Promise<ConvertedT>.All(chain()));
        }

        /// <summary>
        ///     Chain a sequence of operations using promises.
        ///     Reutrn a collection of functions each of which starts an async operation and yields a promise.
        ///     Each function will be called and each promise resolved in turn.
        ///     The resulting promise is resolved after each promise is resolved in sequence.
        /// </summary>
        public IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain)
        {
            return Then(() => Sequence(chain()));
        }

        /// <summary>
        ///     Takes a function that yields an enumerable of promises.
        ///     Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        public IPromise ThenRace(Func<IEnumerable<IPromise>> chain)
        {
            return Then(() => Race(chain()));
        }

        /// <summary>
        ///     Takes a function that yields an enumerable of promises.
        ///     Converts to a value promise.
        ///     Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        public IPromise<ConvertedT> ThenRace<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain)
        {
            return Then(() => Promise<ConvertedT>.Race(chain()));
        }

        public IPromise Finally(Action onComplete)
        {
            var promise = new Promise();
            promise.WithName(Name);

            Then(() => { promise.Resolve(); });
            Catch(e => { promise.Resolve(); });

            return promise.Then(onComplete);
        }

        public IPromise Finally(Func<IPromise> onComplete)
        {
            var promise = new Promise();
            promise.WithName(Name);

            Then(() => { promise.Resolve(); });
            Catch(e => { promise.Resolve(); });

            return promise.Then(onComplete);
        }

        public IPromise<ConvertedT> Finally<ConvertedT>(Func<IPromise<ConvertedT>> onComplete)
        {
            var promise = new Promise();
            promise.WithName(Name);

            Then(() => { promise.Resolve(); });
            Catch(e => { promise.Resolve(); });

            return promise.Then(() => { return onComplete(); });
        }

        /// <summary>
        ///     Name of the promise, when set, useful for debugging.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Event raised for unhandled errors.
        ///     For this to work you have to complete your promises with a call to Done().
        /// </summary>
        public static event EventHandler<ExceptionEventArgs> UnhandledException
        {
            add { unhandlerException += value; }
            remove { unhandlerException -= value; }
        }

        /// <summary>
        ///     Information about pending promises, useful for debugging.
        ///     This is only populated when 'EnablePromiseTracking' is set to true.
        /// </summary>
        public static IEnumerable<IPromiseInfo> GetPendingPromises()
        {
            return pendingPromises;
        }

        /// <summary>
        ///     Increments the ID counter and gives us the ID for the next promise.
        /// </summary>
        internal static int NextId()
        {
            return ++nextPromiseId;
        }

        /// <summary>
        ///     Add a rejection handler for this promise.
        /// </summary>
        private void AddRejectHandler(Action<Exception> onRejected, IRejectable rejectable)
        {
            if (rejectHandlers == null) rejectHandlers = new List<RejectHandler>();

            rejectHandlers.Add(new RejectHandler
            {
                callback = onRejected,
                rejectable = rejectable
            });
        }

        /// <summary>
        ///     Add a resolve handler for this promise.
        /// </summary>
        private void AddResolveHandler(Action onResolved, IRejectable rejectable)
        {
            if (resolveHandlers == null) resolveHandlers = new List<ResolveHandler>();

            resolveHandlers.Add(new ResolveHandler
            {
                callback = onResolved,
                rejectable = rejectable
            });
        }

        /// <summary>
        ///     Invoke a single error handler.
        /// </summary>
        private void InvokeRejectHandler(Action<Exception> callback, IRejectable rejectable, Exception value)
        {
            //            Argument.NotNull(() => callback);
            //            Argument.NotNull(() => rejectable);

            try
            {
                callback(value);
            }
            catch (Exception ex)
            {
                rejectable.Reject(ex);
            }
        }

        /// <summary>
        ///     Invoke a single resolve handler.
        /// </summary>
        private void InvokeResolveHandler(Action callback, IRejectable rejectable)
        {
            //            Argument.NotNull(() => callback);
            //            Argument.NotNull(() => rejectable);

            try
            {
                callback();
            }
            catch (Exception ex)
            {
                rejectable.Reject(ex);
            }
        }

        /// <summary>
        ///     Helper function clear out all handlers after resolution or rejection.
        /// </summary>
        private void ClearHandlers()
        {
            rejectHandlers = null;
            resolveHandlers = null;
        }

        /// <summary>
        ///     Invoke all reject handlers.
        /// </summary>
        private void InvokeRejectHandlers(Exception ex)
        {
            //            Argument.NotNull(() => ex);

            if (rejectHandlers != null)
                rejectHandlers.Each(handler => InvokeRejectHandler(handler.callback, handler.rejectable, ex));

            ClearHandlers();
        }

        /// <summary>
        ///     Invoke all resolve handlers.
        /// </summary>
        private void InvokeResolveHandlers()
        {
            if (resolveHandlers != null)
                resolveHandlers.Each(handler => InvokeResolveHandler(handler.callback, handler.rejectable));

            ClearHandlers();
        }

        /// <summary>
        ///     Helper function to invoke or register resolve/reject handlers.
        /// </summary>
        private void ActionHandlers(IRejectable resultPromise, Action resolveHandler, Action<Exception> rejectHandler)
        {
            if (CurState == PromiseState.Resolved)
            {
                InvokeResolveHandler(resolveHandler, resultPromise);
            }
            else if (CurState == PromiseState.Rejected)
            {
                InvokeRejectHandler(rejectHandler, resultPromise, rejectionException);
            }
            else
            {
                AddResolveHandler(resolveHandler, resultPromise);
                AddRejectHandler(rejectHandler, resultPromise);
            }
        }

        /// <summary>
        ///     Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        ///     Returns a promise of a collection of the resolved results.
        /// </summary>
        public static IPromise All(params IPromise[] promises)
        {
            return All((IEnumerable<IPromise>) promises); // Cast is required to force use of the other All function.
        }

        /// <summary>
        ///     Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        ///     Returns a promise of a collection of the resolved results.
        /// </summary>
        public static IPromise All(IEnumerable<IPromise> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0) return Resolved();

            var remainingCount = promisesArray.Length;
            var resultPromise = new Promise();
            resultPromise.WithName("All");

            promisesArray.Each((promise, index) =>
            {
                promise
                    .Catch(ex =>
                    {
                        if (resultPromise.CurState == PromiseState.Pending) resultPromise.Reject(ex);
                    })
                    .Then(() =>
                    {
                        --remainingCount;
                        if (remainingCount <= 0) resultPromise.Resolve();
                    })
                    .Done();
            });

            return resultPromise;
        }

        /// <summary>
        ///     Chain a number of operations using promises.
        ///     Takes a number of functions each of which starts an async operation and yields a promise.
        /// </summary>
        public static IPromise Sequence(params Func<IPromise>[] fns)
        {
            return Sequence((IEnumerable<Func<IPromise>>) fns);
        }

        /// <summary>
        ///     Chain a sequence of operations using promises.
        ///     Takes a collection of functions each of which starts an async operation and yields a promise.
        /// </summary>
        public static IPromise Sequence(IEnumerable<Func<IPromise>> fns)
        {
            return fns.Aggregate(
                Resolved(),
                (prevPromise, fn) => { return prevPromise.Then(() => fn()); }
            );
        }

        /// <summary>
        ///     Returns a promise that resolves when the first of the promises in the enumerable argument have resolved.
        ///     Returns the value from the first promise that has resolved.
        /// </summary>
        public static IPromise Race(params IPromise[] promises)
        {
            return Race((IEnumerable<IPromise>) promises); // Cast is required to force use of the other function.
        }

        /// <summary>
        ///     Returns a promise that resolves when the first of the promises in the enumerable argument have resolved.
        ///     Returns the value from the first promise that has resolved.
        /// </summary>
        public static IPromise Race(IEnumerable<IPromise> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
                throw new ApplicationException("At least 1 input promise must be provided for Race");

            var resultPromise = new Promise();
            resultPromise.WithName("Race");

            promisesArray.Each((promise, index) =>
            {
                promise
                    .Catch(ex =>
                    {
                        if (resultPromise.CurState == PromiseState.Pending) resultPromise.Reject(ex);
                    })
                    .Then(() =>
                    {
                        if (resultPromise.CurState == PromiseState.Pending) resultPromise.Resolve();
                    })
                    .Done();
            });

            return resultPromise;
        }

        /// <summary>
        ///     Convert a simple value directly into a resolved promise.
        /// </summary>
        public static IPromise Resolved()
        {
            var promise = new Promise();
            promise.Resolve();
            return promise;
        }

        /// <summary>
        ///     Convert an exception directly into a rejected promise.
        /// </summary>
        public static IPromise Rejected(Exception ex)
        {
            //            Argument.NotNull(() => ex);

            var promise = new Promise();
            promise.Reject(ex);
            return promise;
        }

        /// <summary>
        ///     Raises the UnhandledException event.
        /// </summary>
        internal static void PropagateUnhandledException(object sender, Exception ex)
        {
            if (unhandlerException != null) unhandlerException(sender, new ExceptionEventArgs(ex));
        }

        /// <summary>
        ///     Represents a handler invoked when the promise is resolved.
        /// </summary>
        public struct ResolveHandler
        {
            /// <summary>
            ///     Callback fn.
            /// </summary>
            public Action callback;

            /// <summary>
            ///     The promise that is rejected when there is an error while invoking the handler.
            /// </summary>
            public IRejectable rejectable;
        }
    }
}
