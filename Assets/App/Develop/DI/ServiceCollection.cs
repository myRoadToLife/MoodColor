using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace App.Develop.DI
{
    public class ServiceCollection
    {
        private readonly List<ServiceDescriptor> _descriptors = new List<ServiceDescriptor>();

        public void AddSingleton<TService, TImplementation>() where TImplementation : TService
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton));
        }

        public void AddTransient<TService, TImplementation>() where TImplementation : TService
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Transient));
        }

        public void AddScoped<TService, TImplementation>() where TImplementation : TService
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped));
        }

        public void AddSingleton<TService>(TService instance)
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TService), instance));
        }

        public void AddSingleton<TService>(Func<DIContainer, TService> factory)
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TService), factory, ServiceLifetime.Singleton));
        }

        public void AddTransient<TService>(Func<DIContainer, TService> factory)
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TService), factory, ServiceLifetime.Transient));
        }

        public void AddScoped<TService>(Func<DIContainer, TService> factory)
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TService), factory, ServiceLifetime.Scoped));
        }

        public void RegisterAssemblyTypes(Assembly assembly)
        {
            var types = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (var type in types)
            {
                if (type.GetCustomAttribute<RegisterAsSingletonAttribute>() != null)
                {
                    var interfaces = type.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        _descriptors.Add(new ServiceDescriptor(@interface, type, ServiceLifetime.Singleton));
                    }
                }
                else if (type.GetCustomAttribute<RegisterAsTransientAttribute>() != null)
                {
                    var interfaces = type.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        _descriptors.Add(new ServiceDescriptor(@interface, type, ServiceLifetime.Transient));
                    }
                }
                else if (type.GetCustomAttribute<RegisterAsScopedAttribute>() != null)
                {
                    var interfaces = type.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        _descriptors.Add(new ServiceDescriptor(@interface, type, ServiceLifetime.Scoped));
                    }
                }
            }
        }

        public DIContainer Build()
        {
            var container = new DIContainer();
            foreach (var descriptor in _descriptors)
            {
                container.Register(descriptor);
            }
            return container;
        }
    }

    public class ServiceDescriptor
    {
        public Type ServiceType { get; }
        public Type ImplementationType { get; }
        public object ImplementationInstance { get; }
        public Func<DIContainer, object> ImplementationFactory { get; }
        public ServiceLifetime Lifetime { get; }

        public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
        }

        public ServiceDescriptor(Type serviceType, object instance)
        {
            ServiceType = serviceType;
            ImplementationInstance = instance;
            Lifetime = ServiceLifetime.Singleton;
        }

        public ServiceDescriptor(Type serviceType, Func<DIContainer, object> factory, ServiceLifetime lifetime)
        {
            ServiceType = serviceType;
            ImplementationFactory = factory;
            Lifetime = lifetime;
        }
    }
} 