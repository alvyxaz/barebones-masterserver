using System;
using System.Collections;
using UnityEngine;

namespace Barebones.Utils
{
    public static class MonoBehaviorExt
    {
        public static SafeCoroutine<string> StartSafeCoroutine(this MonoBehaviour obj, IEnumerator coroutine)
        {
            var coroutineObject = new SafeCoroutine<string>();
            coroutineObject.WaitCoroutine = obj.StartCoroutine(coroutineObject.InternalRoutine(coroutine));
            return coroutineObject;
        }

        public static SafeCoroutine<T> StartSafeCoroutine<T>(this MonoBehaviour obj, IEnumerator coroutine)
        {
            var coroutineObject = new SafeCoroutine<T>();
            coroutineObject.WaitCoroutine = obj.StartCoroutine(coroutineObject.InternalRoutine(coroutine));
            return coroutineObject;
        }

        public static IEnumerator WaitOrException(this MonoBehaviour obj, IEnumerator coroutine)
        {
            var safeCoroutine = obj.StartSafeCoroutine(coroutine);
            yield return safeCoroutine.WaitCoroutine;

            if (safeCoroutine.Exception != null)
                throw safeCoroutine.Exception;
        }
    }

    public class SafeCoroutine : MonoBehaviour
    {
        public delegate void InvokeCallbackHandler(Exception exception);

        private static SafeCoroutine _runner;
        private static SafeCoroutine _permanentRunner;

        /// <summary>
        ///     Runner, that gets destroyed on scene change
        /// </summary>
        public static SafeCoroutine Runner
        {
            get
            {
                if (_runner == null)
                    _runner = new GameObject("Coroutine runner").AddComponent<SafeCoroutine>();

                return _runner;
            }
        }

        /// <summary>
        ///     Runner, that remains when scene changes
        /// </summary>
        public static SafeCoroutine PermanentRunner
        {
            get
            {
                if (_permanentRunner == null)
                {
                    _permanentRunner = new GameObject("Permanent Coroutine runner")
                        .AddComponent<SafeCoroutine>();
                    _permanentRunner.DontDestroy();
                }


                return _permanentRunner;
            }
        }

        private void DontDestroy()
        {
            DontDestroyOnLoad(this);
        }

        /// <summary>
        ///     Waits for coroutine to finish and calls a callback.
        ///     Callback is invoked with "true" if no exception was thrown
        /// </summary>
        /// <param name="coroutine"></param>
        /// <param name="callback"></param>
        /// <param name="usePermanentRunner">
        ///     If true, will run coroutine on an object
        ///     which is not destroyed on scene change
        /// </param>
        public static void WaitAndCatchException(IEnumerator coroutine, InvokeCallbackHandler callback,
            bool usePermanentRunner = false)
        {
            var runner = usePermanentRunner ? PermanentRunner : Runner;
            runner.StartCoroutine(WaitCoroutine(coroutine, callback, runner));
        }

        private static IEnumerator WaitCoroutine(IEnumerator coroutine, InvokeCallbackHandler callback,
            MonoBehaviour runner)
        {
            var safeCoroutine = runner.StartSafeCoroutine(coroutine);
            yield return safeCoroutine.WaitCoroutine;
            callback.Invoke(safeCoroutine.Exception);
        }
    }

    public class SafeCoroutine<T>
    {
        private T _returnVal;
        public Coroutine WaitCoroutine;

        public T Value
        {
            get
            {
                if (Exception != null)
                    throw Exception;
                return _returnVal;
            }
        }

        public Exception Exception { get; private set; }

        public IEnumerator InternalRoutine(IEnumerator coroutine)
        {
            while (true)
            {
                try
                {
                    if (!coroutine.MoveNext())
                        yield break;
                }
                catch (Exception e)
                {
                    Exception = e;
                    yield break;
                }
                var yielded = coroutine.Current;
                if ((yielded != null) && (yielded.GetType() == typeof(T)))
                {
                    _returnVal = (T) yielded;
                    yield break;
                }
                yield return coroutine.Current;
            }
        }
    }
}