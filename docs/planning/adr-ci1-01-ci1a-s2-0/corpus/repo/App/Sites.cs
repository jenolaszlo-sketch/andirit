using Corpus.Core;

namespace Corpus.App;

/// <summary>
/// Named semantic scenarios. Each <c>/*@Sxx*/</c> marker is immediately followed
/// by the syntax node whose binding the oracle must establish. Markers are data,
/// not instructions, and are never read by production code.
/// </summary>
public sealed class Sites
{
    public int Run()
    {
        var methods = new Methods();
        _ = /*@S01*/ methods.Direct(1);
        _ = /*@S02*/ Methods.Static(2);
        _ = /*@S03*/ methods.Add(1, 2);
        _ = /*@S04*/ methods.Identity<int>(3);
        _ = /*@S05*/ methods.Named(1, second: 2, label: "y");
        _ = /*@S06*/ "hello world".Shout();
        var constructed = /*@S07*/ new Methods();
        Animal animal = new Dog();
        _ = /*@S08*/ animal.BaseOnly();
        _ = /*@S09*/ animal.Speak();
        IGreeter greeter = new Impl();
        _ = /*@S10*/ greeter.Greet("a");
        Animal hidden = new Dog();
        _ = /*@S11*/ hidden.Describe();
        IGreeter explicitGreeter = new ExplicitGreeter();
        _ = /*@S12*/ explicitGreeter.Greet("b");
        _ = /*@S13*/ methods.Prop;
        _ = /*@S14*/ methods.Field;
        /*@S15*/ methods.Changed += OnChanged;
        System.Action action = /*@S16*/ methods.Raise;
        System.Func<int> del = () => 7;
        _ = /*@S17*/ del();
        _ = /*@S18*/ Local(4);
        System.Type kind = /*@S19*/ typeof(Methods);
        _ = /*@S20*/ PartialType.Left();
        _ = PartialType.Right();
        _ = /*@S21*/ methods.Add(1, 2, 3);
        _ = /*@S22*/ methods.Add(1.5, 2.5);
        _ = /*@S23*/ methods.Add("a", "b");
        _ = /*@S24*/ methods.WithOptional(1);
        _ = /*@S25*/ ConditionalApi.Value();
        _ = /*@S26*/ Corpus.Leaf.Helper.Compute(5);
        _ = constructed;
        _ = kind;
        _ = action;
        return 0;

        void OnChanged() { }

        static int Local(int value) => value + 1;
    }
}
