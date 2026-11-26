using System.Globalization;
using System.Text;

namespace SoliloClock.Services;

public static class TextTruncation
{
    /// <summary>
    /// Truncates by text elements, not UTF-16 code units, so surrogate pairs
    /// (emoji, rare CJK) and combining marks are never split in half.
    /// </summary>
    public static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || maxLength <= 0)
        {
            return text ?? string.Empty;
        }

        // Fast path: if the code units fit, the graphemes fit too.
        if (text.Length <= maxLength)
        {
            return text;
        }

        var info = new StringInfo(text);
        if (info.LengthInTextElements <= maxLength)
        {
            return text;
        }

        var budget = maxLength - 1;
        var builder = new StringBuilder(maxLength);
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        var written = 0;

        while (written < budget && enumerator.MoveNext())
        {
            builder.Append(enumerator.GetTextElement());
            written++;
        }

        return builder.Append('…').ToString();
    }
}
