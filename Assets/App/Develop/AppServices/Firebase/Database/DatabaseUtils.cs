// Assets/App/Develop/AppServices/Firebase/Database/Utils/DatabaseUtils.cs

using System.Collections.Generic;

namespace App.Develop.AppServices.Firebase.Database
{
    public static class DatabaseUtils
    {
        public static object CreateIncrement(int value)
        {
            return new Dictionary<string, object>
            {
                [".sv"] = new Dictionary<string, object>
                {
                    ["increment"] = value
                }
            };
        }
    }
}
