using System.Security.Cryptography;
using System.Text;

namespace Switchly_2._0.WebApi.Services.Helpers;

public static class HashPasswordService
{
    public static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

}