namespace QualifierCaller;
public static class Work1
{
    public static void Run()
    {
        QualifierDependency.Api.Mark();
        QualifierDependency.Api.Mark();
        QualifierDependency.Api.Mark();
        QualifierDependency.Api.Mark();
        QualifierDependency.Api.Mark();
        _ = string.Concat("left", "right");
    }
}
