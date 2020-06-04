﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Cysharp.Threading.Tasks.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cysharp.Threading.Tasks
{
    public partial struct UniTask
    {
        static readonly UniTask CanceledUniTask = new Func<UniTask>(() =>
        {
            var promise = new UniTaskCompletionSource();
            promise.TrySetCanceled(CancellationToken.None);
            promise.MarkHandled();
            return promise.Task;
        })();

        static class CanceledUniTaskCache<T>
        {
            public static readonly UniTask<T> Task;

            static CanceledUniTaskCache()
            {
                var promise = new UniTaskCompletionSource<T>();
                promise.TrySetCanceled(CancellationToken.None);
                promise.MarkHandled();
                Task = promise.Task;
            }
        }

        public static readonly UniTask CompletedTask = new UniTask();

        public static UniTask FromException(Exception ex)
        {
            var promise = new UniTaskCompletionSource();
            promise.TrySetException(ex);
            promise.MarkHandled();
            return promise.Task;
        }

        public static UniTask<T> FromException<T>(Exception ex)
        {
            var promise = new UniTaskCompletionSource<T>();
            promise.TrySetException(ex);
            promise.MarkHandled();
            return promise.Task;
        }

        public static UniTask<T> FromResult<T>(T value)
        {
            return new UniTask<T>(value);
        }

        public static UniTask FromCanceled(CancellationToken cancellationToken = default)
        {
            if (cancellationToken == CancellationToken.None)
            {
                return CanceledUniTask;
            }
            else
            {
                var promise = new UniTaskCompletionSource();
                promise.TrySetCanceled(cancellationToken);
                promise.MarkHandled();
                return promise.Task;
            }
        }

        public static UniTask<T> FromCanceled<T>(CancellationToken cancellationToken = default)
        {
            if (cancellationToken == CancellationToken.None)
            {
                return CanceledUniTaskCache<T>.Task;
            }
            else
            {
                var promise = new UniTaskCompletionSource<T>();
                promise.TrySetCanceled(cancellationToken);
                promise.MarkHandled();
                return promise.Task;
            }
        }

        public static UniTask Create(Func<UniTask> factory)
        {
            return factory();
        }

        public static UniTask<T> Create<T>(Func<UniTask<T>> factory)
        {
            return factory();
        }

        public static AsyncLazy Lazy(Func<UniTask> factory)
        {
            return new AsyncLazy(factory);
        }

        public static AsyncLazy<T> Lazy<T>(Func<UniTask<T>> factory)
        {
            return new AsyncLazy<T>(factory);
        }

        /// <summary>
        /// helper of fire and forget void action.
        /// </summary>
        public static void Void(Func<UniTaskVoid> asyncAction)
        {
            asyncAction().Forget();
        }

        /// <summary>
        /// helper of fire and forget void action.
        /// </summary>
        public static void Void(Func<CancellationToken, UniTaskVoid> asyncAction, CancellationToken cancellationToken)
        {
            asyncAction(cancellationToken).Forget();
        }

        /// <summary>
        /// helper of fire and forget void action.
        /// </summary>
        public static void Void<T>(Func<T, UniTaskVoid> asyncAction, T state)
        {
            asyncAction(state).Forget();
        }

        /// <summary>
        /// helper of create add UniTaskVoid to delegate.
        /// For example: FooAction = UniTask.Action(async () => { /* */ })
        /// </summary>
        public static Action Action(Func<UniTaskVoid> asyncAction)
        {
            return AsyncAction.Create(asyncAction);
        }

        /// <summary>
        /// helper of create add UniTaskVoid to delegate.
        /// </summary>
        public static Action Action(Func<CancellationToken, UniTaskVoid> asyncAction, CancellationToken cancellationToken)
        {
            return AsyncActionWithCancellation.Create(asyncAction, cancellationToken);
        }

#if UNITY_2018_3_OR_NEWER

        /// <summary>
        /// Create async void(UniTaskVoid) UnityAction.
        /// For exampe: onClick.AddListener(UniTask.UnityAction(async () => { /* */ } ))
        /// </summary>
        public static UnityEngine.Events.UnityAction UnityAction(Func<UniTaskVoid> asyncAction)
        {
            return AsyncUnityAction.Create(asyncAction);
        }

        /// <summary>
        /// Create async void(UniTaskVoid) UnityAction.
        /// For exampe: onClick.AddListener(UniTask.UnityAction(FooAsync, this.GetCancellationTokenOnDestroy()))
        /// </summary>
        public static UnityEngine.Events.UnityAction UnityAction(Func<CancellationToken, UniTaskVoid> asyncAction, CancellationToken cancellationToken)
        {
            return AsyncUnityActionWithCancellation.Create(asyncAction, cancellationToken);
        }

#endif

        /// <summary>
        /// Defer the task creation just before call await.
        /// </summary>
        public static UniTask Defer(Func<UniTask> factory)
        {
            return new UniTask(new DeferPromise(factory), 0);
        }

        /// <summary>
        /// Defer the task creation just before call await.
        /// </summary>
        public static UniTask<T> Defer<T>(Func<UniTask<T>> factory)
        {
            return new UniTask<T>(new DeferPromise<T>(factory), 0);
        }

        sealed class DeferPromise : IUniTaskSource
        {
            Func<UniTask> factory;
            UniTask task;
            UniTask.Awaiter awaiter;

            public DeferPromise(Func<UniTask> factory)
            {
                this.factory = factory;
            }

            public void GetResult(short token)
            {
                awaiter.GetResult();
            }

            public UniTaskStatus GetStatus(short token)
            {
                var f = Interlocked.Exchange(ref factory, null);
                if (f == null) throw new InvalidOperationException("Can't call twice.");

                task = f();
                awaiter = f().GetAwaiter();
                return task.Status;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                awaiter.SourceOnCompleted(continuation, state);
            }

            public UniTaskStatus UnsafeGetStatus()
            {
                return task.Status;
            }
        }

        sealed class DeferPromise<T> : IUniTaskSource<T>
        {
            Func<UniTask<T>> factory;
            UniTask<T> task;
            UniTask<T>.Awaiter awaiter;

            public DeferPromise(Func<UniTask<T>> factory)
            {
                this.factory = factory;
            }

            public T GetResult(short token)
            {
                return awaiter.GetResult();
            }

            void IUniTaskSource.GetResult(short token)
            {
                awaiter.GetResult();
            }

            public UniTaskStatus GetStatus(short token)
            {
                var f = Interlocked.Exchange(ref factory, null);
                if (f == null) throw new InvalidOperationException("Can't call twice.");

                task = f();
                awaiter = f().GetAwaiter();
                return task.Status;
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                awaiter.SourceOnCompleted(continuation, state);
            }

            public UniTaskStatus UnsafeGetStatus()
            {
                return task.Status;
            }
        }

        sealed class AsyncAction : ITaskPoolNode<AsyncAction>
        {
            static TaskPool<AsyncAction> pool;

            public AsyncAction NextNode { get; set; }

            static AsyncAction()
            {
                TaskPool.RegisterSizeGetter(typeof(AsyncAction), () => pool.Size);
            }

            readonly Action runDelegate;
            Func<UniTaskVoid> voidAction;

            AsyncAction()
            {
                runDelegate = Run;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Action Create(Func<UniTaskVoid> voidAction)
            {
                if (!pool.TryPop(out var item))
                {
                    item = new AsyncAction();
                }

                item.voidAction = voidAction;
                return item.runDelegate;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Run()
            {
                var call = voidAction;
                voidAction = null;
                if (call != null)
                {
                    pool.TryPush(this);
                    call.Invoke();
                }
            }
        }

        sealed class AsyncActionWithCancellation : ITaskPoolNode<AsyncActionWithCancellation>
        {
            static TaskPool<AsyncActionWithCancellation> pool;

            public AsyncActionWithCancellation NextNode { get; set; }

            static AsyncActionWithCancellation()
            {
                TaskPool.RegisterSizeGetter(typeof(AsyncActionWithCancellation), () => pool.Size);
            }

            readonly Action runDelegate;
            CancellationToken cancellationToken;
            Func<CancellationToken, UniTaskVoid> voidAction;

            AsyncActionWithCancellation()
            {
                runDelegate = Run;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Action Create(Func<CancellationToken, UniTaskVoid> voidAction, CancellationToken cancellationToken)
            {
                if (!pool.TryPop(out var item))
                {
                    item = new AsyncActionWithCancellation();
                }

                item.voidAction = voidAction;
                item.cancellationToken = cancellationToken;
                return item.runDelegate;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Run()
            {
                var call = voidAction;
                var ct = cancellationToken;
                voidAction = null;
                cancellationToken = default;
                if (call != null)
                {
                    pool.TryPush(this);
                    call.Invoke(ct);
                }
            }
        }

#if UNITY_2018_3_OR_NEWER

        sealed class AsyncUnityAction : ITaskPoolNode<AsyncUnityAction>
        {
            static TaskPool<AsyncUnityAction> pool;

            public AsyncUnityAction NextNode { get; set; }

            static AsyncUnityAction()
            {
                TaskPool.RegisterSizeGetter(typeof(AsyncUnityAction), () => pool.Size);
            }

            readonly UnityEngine.Events.UnityAction runDelegate;
            Func<UniTaskVoid> voidAction;

            AsyncUnityAction()
            {
                runDelegate = Run;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static UnityEngine.Events.UnityAction Create(Func<UniTaskVoid> voidAction)
            {
                if (!pool.TryPop(out var item))
                {
                    item = new AsyncUnityAction();
                }

                item.voidAction = voidAction;
                return item.runDelegate;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Run()
            {
                var call = voidAction;
                voidAction = null;
                if (call != null)
                {
                    pool.TryPush(this);
                    call.Invoke();
                }
            }
        }

        sealed class AsyncUnityActionWithCancellation : ITaskPoolNode<AsyncUnityActionWithCancellation>
        {
            static TaskPool<AsyncUnityActionWithCancellation> pool;

            public AsyncUnityActionWithCancellation NextNode { get; set; }

            static AsyncUnityActionWithCancellation()
            {
                TaskPool.RegisterSizeGetter(typeof(AsyncUnityActionWithCancellation), () => pool.Size);
            }

            readonly UnityEngine.Events.UnityAction runDelegate;
            CancellationToken cancellationToken;
            Func<CancellationToken, UniTaskVoid> voidAction;

            AsyncUnityActionWithCancellation()
            {
                runDelegate = Run;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static UnityEngine.Events.UnityAction Create(Func<CancellationToken, UniTaskVoid> voidAction, CancellationToken cancellationToken)
            {
                if (!pool.TryPop(out var item))
                {
                    item = new AsyncUnityActionWithCancellation();
                }

                item.voidAction = voidAction;
                item.cancellationToken = cancellationToken;
                return item.runDelegate;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Run()
            {
                var call = voidAction;
                var ct = cancellationToken;
                voidAction = null;
                cancellationToken = default;
                if (call != null)
                {
                    pool.TryPush(this);
                    call.Invoke(ct);
                }
            }
        }

#endif
    }

    internal static class CompletedTasks
    {
        public static readonly UniTask<AsyncUnit> AsyncUnit = UniTask.FromResult(Cysharp.Threading.Tasks.AsyncUnit.Default);
        public static readonly UniTask<bool> True = UniTask.FromResult(true);
        public static readonly UniTask<bool> False = UniTask.FromResult(false);
        public static readonly UniTask<int> Zero = UniTask.FromResult(0);
        public static readonly UniTask<int> MinusOne = UniTask.FromResult(-1);
        public static readonly UniTask<int> One = UniTask.FromResult(1);
    }
}
