using System.Collections.Generic;
using System.Linq;

namespace Sandstorm.Core.Logger;

public static class Watermark
{
    public static void Draw(List<string> text)
    {
        List<string> result = new();
        int longestLength = text.Max(TextLength);
        string line = new('-', longestLength);
        result.Add($"┌─{line}─┐");

        foreach (string textItem in text)
        {
            int spacingSize = longestLength - TextLength(textItem);
            string spacingText = textItem + new string(' ', spacingSize);
            result.Add($"│ {spacingText} │");
        }

        result.Add($"└─{line}─┘");
        foreach (string textItem in result)
        {
            LogBase.Info(textItem);
        }
    }

    private static int TextLength(string s)
    {
        return s.Replace("[\u0391-\uFFE5]", "ab").Length;
    }
}
