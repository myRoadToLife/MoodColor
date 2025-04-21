using System;
using System.Collections.Generic;

namespace App.Develop.DI
{
    public class ServiceCollection
    {
        private readonly Dictionary<Type, ServiceDescriptor> _descriptors = new();

        public void AddSingleton<TService>(TService implementation) where TService : class
        {
            var descriptor = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                Implementation = implementation,
                Lifetime = ServiceLifetime.Singleton
            };
            _descriptors[typeof(TService)] = descriptor;
        }

        public void AddSingleton<TService>() where TService : class, new()
        {
            var descriptor = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationFactory = _ => new TService(),
                Lifetime = ServiceLifetime.Singleton
            };
            _descriptors[typeof(TService)] = descriptor;
        }

        public void AddSingleton<TService, TImplementation>() 
            where TService : class 
            where TImplementation : class, TService, new()
        {
            var descriptor = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationFactory = _ => new TImplementation(),
                Lifetime = ServiceLifetime.Singleton
            };
            _descriptors[typeof(TService)] = descriptor;
        }

        public void AddSingleton<TService>(Func<DIContainer, TService> implementationFactory) where TService : class
        {
            var descriptor = new ServiceDescriptor
            {
                ServiceType = typeof(TService),
                ImplementationFactory = di => implementationFactory(di),
                Lifetime = ServiceLifetime.Singleton
            };
            _descriptors[typeof(TService)] = descriptor;
        }

        public DIContainer Build()
        {
            return new DIContainer(_descriptors);
        }
    }

    public class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public object Implementation { get; set; }
        public Func<DIContainer, object> ImplementationFactory { get; set; }
        public ServiceLifetime Lifetime { get; set; }
    }

    public enum ServiceLifetime
    {
        Singleton,
        Transient
    }
} 