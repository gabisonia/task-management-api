using System.Security.Cryptography;
using System.Text;

namespace TaskService.Shared;

public static class ETagGenerator
{
    public static string From(string id, DateTime updatedAt)
    {
        var input = Encoding.UTF8.GetBytes($"{id}:{updatedAt.Ticks}");
        var hash = SHA256.HashData(input);
        var base64 = Convert.ToBase64String(hash);
        return $"\"{base64}\"";
    }
}

