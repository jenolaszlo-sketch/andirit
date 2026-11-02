namespace Corpus.Core;

public sealed class Methods
{
    public int Direct(int value) => value;

    public static int Static(int value) => value;

    // Five overloads (plus string) to cover overload selection.
    public int Add(int a, int b) => a + b;

    public int Add(int a, int b, int c) => a + b + c;

    public string Add(string a, string b) => a + b;

    public double Add(double a, double b) => a + b;

    public long Add(long a, long b) => a + b;

    // Deliberately ambiguous pair: Add(1, 1) cannot resolve.
    public int Amb(int a, long b) => a + (int)b;

    public int Amb(long a, int b) => (int)a + b;

    public T Identity<T>(T value) => value;

    public string Named(int first, int second = 5, string label = "x") => label + (first + second);

    public string WithOptional(int a, int b = 10) => (a + b).ToString();

    public int Prop { get; set; }

    public int Field;

    public event System.Action? Changed;

    public void Raise() => Changed?.Invoke();

    public string Describe(int a) => "int:" + a;
}

public static partial class PartialType
{
    public static int Left() => 1;
}

public static partial class PartialType
{
    public static int Right() => 2;
}

public static class ConditionalApi
{
#if CORE_FEATURE
    public static string Value() => "feature";
#else
    public static int Value() => 1;
#endif
}
