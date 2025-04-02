using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Develop.DI
{
    public class DIContainer
    {
        private readonly Dictionary<Type, Registration> _container = new Dictionary<Type, Registration>();

        private readonly DIContainer _parent;

        private readonly List<Type> _request = new List<Type>();

        public DIContainer() : this(null)
        {
        }

        public DIContainer(DIContainer parent) => _parent = parent;

        public void RegisterAsSingle <T>(Func<DIContainer, T> factory)
        {
            if (_container.ContainsKey(typeof(T)))
                throw new InvalidOperationException($"The type {typeof(T)} is already registered.");

            Registration registration = new Registration(container => factory(container));
            _container[typeof(T)] = registration;
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

            public Registration(object instance) => Instance = instance;
            public Registration(Func<DIContainer, object> factory) => Factory = factory;
        }
    }
}
