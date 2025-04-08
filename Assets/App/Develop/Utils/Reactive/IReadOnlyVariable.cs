using System;

namespace App.Develop.Utils.Reactive
{
    public interface IReadOnlyVariable <out T>
    {
        event Action<T, T> Changed;
        T Value { get; }
    }
}
