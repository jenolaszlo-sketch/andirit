namespace ProvenanceFixture;
public static partial class Api
{
#if FEATURE
    public static string Value() => "changed";
#else
    public static int Value() => 1;
#endif
}
