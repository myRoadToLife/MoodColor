using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;

namespace App.Develop.CommonServices.CoroutinePerformer
{
    /// <summary>
    /// Реализация интерфейса ICoroutinePerformer для выполнения корутин
    /// </summary>
    [PublicAPI]
    [Preserve]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    [DefaultExecutionOrder(-10000)]
    public sealed class CoroutinePerformer : MonoBehaviour, ICoroutinePerformer
    {
        private bool m_IsDisposed;

        private void Awake()
        {
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.DontSave;
                name = "[CoroutinePerformer]";
            }
        }

        /// <inheritdoc />
        public Coroutine StartCoroutine(IEnumerator _routine)
        {
            ThrowIfDisposed();
            
            if (_routine == null)
            {
                throw new ArgumentNullException(nameof(_routine));
            }
            
            return base.StartCoroutine(_routine);
        }

        /// <inheritdoc />
        public void StopCoroutine(Coroutine _coroutine)
        {
            ThrowIfDisposed();
            
            if (_coroutine != null)
            {
                base.StopCoroutine(_coroutine);
            }
        }

        /// <inheritdoc />
        public void StopAllCoroutines()
        {
            ThrowIfDisposed();
            base.StopAllCoroutines();
        }

        /// <inheritdoc />
        public Coroutine ExecuteWithDelay(float _delay, Action _action)
        {
            ThrowIfDisposed();
            
            if (_delay < 0)
            {
                throw new ArgumentException("Delay cannot be negative", nameof(_delay));
            }
            
            return StartCoroutine(ExecuteWithDelayRoutine(_delay, _action));
        }

        /// <inheritdoc />
        public Coroutine ExecuteNextFrame(Action _action)
        {
            ThrowIfDisposed();
            return StartCoroutine(ExecuteNextFrameRoutine(_action));
        }

        /// <inheritdoc />
        public Coroutine ExecuteWhile(Func<bool> _condition, Action _action)
        {
            ThrowIfDisposed();
            
            if (_condition == null)
            {
                throw new ArgumentNullException(nameof(_condition));
            }
            
            return StartCoroutine(ExecuteWhileRoutine(_condition, _action));
        }

        /// <inheritdoc />
        public Coroutine ExecuteUntil(Func<bool> _predicate, Action _onComplete = null, float _timeout = 0, Action _onTimeout = null)
        {
            ThrowIfDisposed();
            
            if (_predicate == null)
            {
                throw new ArgumentNullException(nameof(_predicate));
            }
            
            if (_timeout < 0)
            {
                throw new ArgumentException("Timeout cannot be negative", nameof(_timeout));
            }
            
            return StartCoroutine(ExecuteUntilRoutine(_predicate, _onComplete, _timeout, _onTimeout));
        }

        #region Private Methods

        private void ThrowIfDisposed()
        {
            if (m_IsDisposed)
            {
                throw new ObjectDisposedException(nameof(CoroutinePerformer));
            }
        }

        private void OnDestroy()
        {
            m_IsDisposed = true;
            StopAllCoroutines();
        }

        #endregion

        #region Private Coroutines

        private IEnumerator ExecuteWithDelayRoutine(float _delay, Action _action)
        {
            yield return new WaitForSeconds(_delay);
            _action?.Invoke();
        }

        private IEnumerator ExecuteNextFrameRoutine(Action _action)
        {
            yield return null;
            _action?.Invoke();
        }

        private IEnumerator ExecuteWhileRoutine(Func<bool> _condition, Action _action)
        {
            while (_condition())
            {
                _action?.Invoke();
                yield return null;
            }
        }

        private IEnumerator ExecuteUntilRoutine(Func<bool> _predicate, Action _onComplete, float _timeout, Action _onTimeout)
        {
            float startTime = Time.time;
            bool hasTimeout = _timeout > 0;

            while (!_predicate())
            {
                if (hasTimeout && (Time.time - startTime >= _timeout))
                {
                    _onTimeout?.Invoke();
                    yield break;
                }
                yield return null;
            }

            _onComplete?.Invoke();
        }

        #endregion
    }
}
