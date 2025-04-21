using System;

namespace App.Develop.DI
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RegisterAsSingletonAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RegisterAsTransientAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RegisterAsScopedAttribute : Attribute { }

    public enum ServiceLifetime
    {
        Transient,
        Scoped,
        Singleton
    }
} 