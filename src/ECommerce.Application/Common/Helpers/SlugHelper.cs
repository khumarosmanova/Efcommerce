using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ECommerce.Application.Common.Helpers;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // 1. Normalize and remove accents/diacritics
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var clean = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // 2. Replace non-alphanumeric characters with hyphens
        clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");

        // 3. Replace multiple spaces/hyphens with a single hyphen
        clean = Regex.Replace(clean, @"[\s-]+", "-").Trim('-');

        return clean;
    }
}
