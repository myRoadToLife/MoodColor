using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

namespace App.Develop.CommonServices.CoroutinePerformer
{
    /// <summary>
    /// Реализация интерфейса ICoroutinePerformer для выполнения корутин
    /// </summary>
    [PublicAPI]
    public sealed class CoroutinePerformer : ICoroutinePerformer
    {
        private readonly MonoBehaviour m_MonoBehaviourProxy;
        private bool m_IsDisposed;

        /// <summary>
        /// Создает новый экземпляр CoroutinePerformer
        /// </summary>
        /// <param name="_monoBehaviourProxy">MonoBehaviour, который будет использоваться для запуска корутин</param>
        /// <exception cref="ArgumentNullException">Если _monoBehaviourProxy равен null</exception>
        /// <exception cref="InvalidOperationException">Если создается не в режиме воспроизведения</exception>
        internal CoroutinePerformer(MonoBehaviour _monoBehaviourProxy)
        {
            m_MonoBehaviourProxy = _monoBehaviourProxy ?? throw new ArgumentNullException(nameof(_monoBehaviourProxy));
            
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("CoroutinePerformer can only be created in play mode");
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
            
            return m_MonoBehaviourProxy.StartCoroutine(_routine);
        }

        /// <inheritdoc />
        public void StopCoroutine(Coroutine _coroutine)
        {
            ThrowIfDisposed();
            
            if (_coroutine != null)
            {
                m_MonoBehaviourProxy.StopCoroutine(_coroutine);
            }
        }

        /// <inheritdoc />
        public void StopAllCoroutines()
        {
            ThrowIfDisposed();
            m_MonoBehaviourProxy.StopAllCoroutines();
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
