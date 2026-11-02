namespace Corpus.Core;

public interface IGreeter
{
    string Greet(string name);
}

public class Animal
{
    public virtual string Speak() => "animal";

    public string BaseOnly() => "base";

    public string Describe() => "animal";
}

public sealed class Dog : Animal
{
    public override string Speak() => "woof";

    public new string Describe() => "dog";
}

public sealed class Impl : IGreeter
{
    public string Greet(string name) => "hi " + name;
}

public sealed class ExplicitGreeter : IGreeter
{
    string IGreeter.Greet(string name) => "explicit " + name;
}
