using System.Threading.Tasks;

namespace App.Develop.CommonServices.Firebase.Auth.Services
{
    public interface IAuthService
    {
        // Метод для регистрации нового пользователя
        Task<(bool success, string error)> RegisterUser(string email, string password);
        
        // Метод для входа пользователя
        Task<(bool success, string error)> LoginUser(string email, string password);
        
        // Метод для входа через Google
        Task<(bool success, string error)> LoginWithGoogle();
        
        // Метод для повторной отправки письма верификации
        Task<bool> ResendVerificationEmail();
        
        // Метод для проверки верификации email
        Task<bool> IsEmailVerified();
        
        // Дополнительные методы, которые могут потребоваться
        Task<bool> ResetPassword(string email);
        Task SignOut();
    }
}
