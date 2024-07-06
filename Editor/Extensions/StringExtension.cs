
using System.Text.RegularExpressions;

static partial class StringExtension
{
    public static bool IsValidVarname(this string input)
    {
        return Regex.IsMatch(input, @"^[_a-zA-Z]+[_a-zA-Z0-9]*$");
    }
}