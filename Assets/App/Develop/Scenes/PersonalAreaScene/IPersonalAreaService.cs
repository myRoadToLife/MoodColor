using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Develop.Scenes.PersonalAreaScene
{
    public interface IPersonalAreaService
    {
        void Initialize(string userId);
        Task<Dictionary<string, object>> GetUserDataAsync();
        Task UpdateUserDataAsync(Dictionary<string, object> updates);
        Task<List<Dictionary<string, object>>> GetEmotionsAsync();
        Task<Dictionary<string, object>> GetStatisticsAsync();
        Task<List<Dictionary<string, object>>> GetFriendsAsync();
        Task<Dictionary<string, object>> GetCustomizationAsync();
    }
} 