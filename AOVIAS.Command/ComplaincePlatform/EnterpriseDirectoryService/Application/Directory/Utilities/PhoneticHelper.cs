namespace EnterpriseDirectoryService.Application.Directory.Utilities;

public static class PhoneticHelper
{
    public static string ComputeKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var word = input.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? input;
        return Soundex(word);
    }

    private static string Soundex(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.ToUpperInvariant();
        var result = new char[4];
        result[0] = s[0];
        int Map(char c) => c switch
        {
            'B' or 'F' or 'P' or 'V' => 1,
            'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => 2,
            'D' or 'T' => 3,
            'L' => 4,
            'M' or 'N' => 5,
            'R' => 6,
            _ => 0
        };
        var i = 1;
        var j = 1;
        var prev = Map(s[0]);
        for (; i < s.Length && j < 4; i++)
        {
            var code = Map(s[i]);
            if (code != 0 && code != prev)
            {
                result[j++] = (char)('0' + code);
                prev = code;
            }
            else if (code == 0)
            {
                prev = 0;
            }
        }
        while (j < 4) result[j++] = '0';
        return new string(result);
    }
}
