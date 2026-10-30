namespace CI1Corpus;
public static class Overloads
{
    public static void Hit(int value) { }
    public static void Hit(string value) { }
    public static void Caller() { Hit(1); Hit("x"); }
}
public static class GenericExtensions
{
    public static T Echo<T>(T value) => value;
    public static string Mark<T>(this T value) => "";
    public static void Caller() { Echo<int>(1); "x".Mark<string>(); }
}
public class ConstructorBase
{
    public ConstructorBase(int value) { }
}
public class Constructors : ConstructorBase
{
    public Constructors() : this(1) { }
    public Constructors(int value) : base(value) { }
    public static void Caller() { Constructors a = new(); var b = new Constructors(1); }
}
public interface IWorker { void Work(); }
public class Worker : IWorker
{
    public virtual void Work() { }
}
public class OverrideWorker : Worker
{
    public override void Work() { }
    public void BaseCall() { base.Work(); }
}
public static class Dispatch
{
    public static void InterfaceCall(IWorker value) { value.Work(); }
    public static void VirtualCall(Worker value) { value.Work(); }
}
public static class Target
{
    public static void Work() { }
}
public static class MethodGroups
{
    public static void Caller() { System.Action action = Target.Work; action(); }
}
public static class References
{
    public static void Caller() { var t = typeof(Target); var n = nameof(Target.Work); Target.Work(); }
}
public static class External
{
    public static void Caller() { System.Console.WriteLine("outside"); }
}
public static class Ambiguous
{
    public static void Pick(string value) { }
    public static void Pick(System.Exception value) { }
    public static void Caller() { Pick(null); Missing(); }
}
public static class Conditional
{
    public static void Caller()
    {
#if CI1_EXTRA
        Target.Work();
#endif
    }
}
