using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Develop.DI
{
    public class DIContainer
    {
        private readonly Dictionary<Type, ServiceDescriptor> _descriptors;
        private readonly Dictionary<Type, object> _instances = new();

        public DIContainer(Dictionary<Type, ServiceDescriptor> descriptors)
        {
            _descriptors = descriptors;
        }

        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }

        private object Resolve(Type serviceType)
        {
            if (!_descriptors.TryGetValue(serviceType, out var descriptor))
            {
                throw new InvalidOperationException($"Service of type {serviceType} is not registered");
            }

            if (descriptor.Implementation != null)
            {
                return descriptor.Implementation;
            }

            if (_instances.TryGetValue(serviceType, out var instance))
            {
                return instance;
            }

            var implementation = descriptor.ImplementationFactory(this);
            
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                _instances[serviceType] = implementation;
            }

            return implementation;
        }

        public async Task InitializeAsync()
        {
            foreach (var descriptor in _descriptors.Values)
            {
                if (descriptor.Implementation is IAsyncInitializable initializable)
                {
                    await initializable.InitializeAsync();
                }
            }
        }
    }
}
