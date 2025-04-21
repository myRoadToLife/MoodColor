using System.Threading.Tasks;

namespace App.Develop.DI
{
    public interface IAsyncInitializable
    {
        Task InitializeAsync();
    }
} 