using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Develop.Services.Friends
{
    public interface IFriendsService
    {
        Task<List<UserModel>> GetFriendsAsync();
        Task<List<UserModel>> SearchUsersAsync(string searchQuery);
        Task<bool> SendFriendRequestAsync(string userId);
        Task<bool> RemoveFriendAsync(string userId);
        Task<bool> AcceptFriendRequestAsync(string userId);
        Task<bool> RejectFriendRequestAsync(string userId);
    }
}