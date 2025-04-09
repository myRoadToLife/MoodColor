using System;
using System.Collections.Generic;

namespace App.Develop.DI
{
    public class DIContainer : IDisposable
    {
        private readonly Dictionary<Type, Registration> _container = new Dictionary<Type, Registration>();

        private readonly DIContainer _parent;

        private readonly List<Type> _request = new List<Type>();

        public DIContainer() : this(null)
        {
        }

        public DIContainer(DIContainer parent) => _parent = parent;

        public Registration RegisterAsSingle <T>(Func<DIContainer, T> factory)
        {
            if (IsAlreadyRegistered<T>())
                throw new InvalidOperationException($"The type {typeof(T)} is already registered.");

            Registration registration = new Registration(container => factory(container));
            _container[typeof(T)] = registration;
            return registration;
        }

        private bool IsAlreadyRegistered <T>()
        {
            if (_container.ContainsKey(typeof(T)))
                return true;

            if (_parent != null)
                return _parent.IsAlreadyRegistered<T>();

            return false;
        }

        public T Resolve <T>()
        {
            if (_request.Contains(typeof(T)))
                throw new InvalidOperationException($"The type {typeof(T)} is cycle resolving.");

            _request.Add(typeof(T));

            try
            {
                if (_container.TryGetValue(typeof(T), out Registration registration))
                    return CreateFrom<T>(registration);

                if (_parent != null)
                    return _parent.Resolve<T>();
            }
            finally
            {
                _request.Remove(typeof(T));
            }

            throw new InvalidOperationException($"The type {typeof(T)} is not registered.");
        }

        public void Initialize()
        {
            foreach (Registration registration in _container.Values)
            {
                if (registration.Instance == null && registration.IsNonLazy)
                    registration.Instance = registration.Factory(this);

                if (registration.Instance != null)
                    if (registration.Instance is IInitializable initializable)
                        initializable.Initialize();
            }
        }

        public void Dispose()
        {
            foreach (Registration registration in _container.Values)
            {
                if (registration.Instance != null)
                    if (registration.Instance is IDisposable disposable)
                        disposable.Dispose();
            }
        }

        private T CreateFrom <T>(Registration registration)
        {
            if (registration.Instance == null && registration.Factory != null)
                registration.Instance = registration.Factory(this);

            return (T)registration.Instance;
        }

        public class Registration
        {
            public Func<DIContainer, object> Factory { get; }
            public object Instance { get; set; }

            public bool IsNonLazy { get; private set; }

            public Registration(object instance) => Instance = instance;
            public Registration(Func<DIContainer, object> factory) => Factory = factory;

            public void NonLazy() => IsNonLazy = true;
        }
    }
}
