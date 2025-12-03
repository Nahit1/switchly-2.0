using System.Text.RegularExpressions;

namespace Switchly_2._0.WebApi.Services.Helpers;

public static class SlugHelper
{
    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        input = input.Trim().ToLowerInvariant();

        // Türkçe karakterleri sadeleştir
        input = input
            .Replace("ç", "c")
            .Replace("ğ", "g")
            .Replace("ı", "i")
            .Replace("ö", "o")
            .Replace("ş", "s")
            .Replace("ü", "u");

        // Harf/rakam ve boşluk dışındaki karakterleri kaldır
        input = Regex.Replace(input, @"[^a-z0-9\s-]", "");

        // Birden fazla boşluk veya -'yi tek boşluk yap
        input = Regex.Replace(input, @"[\s-]+", " ").Trim();

        // Boşlukları - yap
        var slug = input.Replace(" ", "-");

        return slug;
    }
}