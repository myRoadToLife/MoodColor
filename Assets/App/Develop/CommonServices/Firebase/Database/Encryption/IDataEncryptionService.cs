namespace App.Develop.CommonServices.Firebase.Database.Encryption
{
    /// <summary>
    /// Интерфейс для сервиса шифрования данных Firebase
    /// </summary>
    public interface IDataEncryptionService
    {
        /// <summary>
        /// Шифрует строку
        /// </summary>
        /// <param name="plainText">Исходная строка</param>
        /// <returns>Зашифрованная строка</returns>
        string Encrypt(string plainText);
        
        /// <summary>
        /// Дешифрует строку
        /// </summary>
        /// <param name="encryptedText">Зашифрованная строка</param>
        /// <returns>Расшифрованная строка</returns>
        string Decrypt(string encryptedText);
        
        /// <summary>
        /// Получает текущий пароль для шифрования
        /// </summary>
        /// <returns>Пароль шифрования</returns>
        string GetEncryptionPassword();
    }
} 