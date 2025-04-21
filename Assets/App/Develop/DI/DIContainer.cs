using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Develop.DI
{
    public class DIContainer : IDisposable
    {
        private readonly Dictionary<Type, ServiceDescriptor> _descriptors = new Dictionary<Type, ServiceDescriptor>();
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private readonly DIContainer _parent;
        private readonly List<Type> _request = new List<Type>();

        public DIContainer() : this(null)
        {
        }

        public DIContainer(DIContainer parent) => _parent = parent;

        public void Register(ServiceDescriptor descriptor)
        {
            if (_descriptors.ContainsKey(descriptor.ServiceType))
                throw new InvalidOperationException($"Service {descriptor.ServiceType} is already registered.");

            _descriptors[descriptor.ServiceType] = descriptor;
        }

        public T Resolve<T>()
        {
            if (_request.Contains(typeof(T)))
                throw new InvalidOperationException($"Cycle detected while resolving {typeof(T)}");

            _request.Add(typeof(T));

            try
            {
                if (_instances.TryGetValue(typeof(T), out var instance))
                    return (T)instance;

                if (_descriptors.TryGetValue(typeof(T), out var descriptor))
                {
                    var resolved = CreateInstance(descriptor);
                    if (descriptor.Lifetime == ServiceLifetime.Singleton)
                        _instances[typeof(T)] = resolved;
                    return (T)resolved;
                }

                if (_parent != null)
                    return _parent.Resolve<T>();

                throw new InvalidOperationException($"Service {typeof(T)} is not registered.");
            }
            finally
            {
                _request.Remove(typeof(T));
            }
        }

        private object CreateInstance(ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;

            if (descriptor.ImplementationFactory != null)
                return descriptor.ImplementationFactory(this);

            var type = descriptor.ImplementationType;
            var constructors = type.GetConstructors();

            if (constructors.Length == 0)
                throw new InvalidOperationException($"No public constructor found for {type}");

            var constructor = constructors[0];
            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                args[i] = Resolve(parameterType);
            }

            return constructor.Invoke(args);
        }

        private object Resolve(Type type)
        {
            var method = typeof(DIContainer).GetMethod(nameof(Resolve)).MakeGenericMethod(type);
            return method.Invoke(this, null);
        }

        public async Task InitializeAsync()
        {
            foreach (var descriptor in _descriptors.Values)
            {
                if (descriptor.Lifetime == ServiceLifetime.Singleton)
                {
                    var instance = Resolve(descriptor.ServiceType);
                    if (instance is IAsyncInitializable asyncInitializable)
                        await asyncInitializable.InitializeAsync();
                    else if (instance is IInitializable initializable)
                        initializable.Initialize();
                }
            }
        }

        public void Initialize()
        {
            foreach (var descriptor in _descriptors.Values)
            {
                if (descriptor.Lifetime == ServiceLifetime.Singleton)
                {
                    var instance = Resolve(descriptor.ServiceType);
                    if (instance is IInitializable initializable)
                        initializable.Initialize();
                }
            }
        }

        public void Dispose()
        {
            foreach (var instance in _instances.Values)
            {
                if (instance is IDisposable disposable)
                    disposable.Dispose();
            }
            _instances.Clear();
            _descriptors.Clear();
        }
    }
}
