namespace Corpus.Core;

public static class StringExtensions
{
    public static string Shout(this string value) => value.ToUpperInvariant();

    public static int WordCount(this string value) => value.Split(' ').Length;
}
