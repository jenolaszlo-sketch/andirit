using Corpus.Core;

namespace Corpus.App;

/// <summary>
/// Volume sites to meet the F12 minima (>= 50 invocations, >= 20 method
/// references). <c>/*@INV-nn*/</c> markers are additional supported occurrences;
/// <c>/*@MR-nn*/</c> markers are method-group references (qualification-only:
/// the resolved symbol is a method, but a method group is not an occurrence
/// fact). Every site binds to a source-defined <see cref="Methods"/>,
/// <see cref="StringExtensions"/>, <see cref="PartialType"/>,
/// <see cref="ConditionalApi"/> or <see cref="Corpus.Leaf.Helper"/> member.
/// </summary>
public sealed class Bulk
{
    public int RunAll()
    {
        var m = new Methods();
        var sum = 0;
        sum += /*@INV-01*/ m.Add(1, 2);
        sum += /*@INV-02*/ m.Add(3, 4);
        sum += /*@INV-03*/ m.Add(5, 6);
        sum += /*@INV-04*/ m.Add(7, 8);
        sum += /*@INV-05*/ m.Add(9, 10);
        sum += /*@INV-06*/ Methods.Static(11);
        sum += /*@INV-07*/ Methods.Static(12);
        sum += /*@INV-08*/ Methods.Static(13);
        sum += /*@INV-09*/ Methods.Static(14);
        sum += /*@INV-10*/ Methods.Static(15);
        sum += /*@INV-11*/ m.Direct(16);
        sum += /*@INV-12*/ m.Direct(17);
        sum += /*@INV-13*/ m.Direct(18);
        sum += /*@INV-14*/ m.Direct(19);
        sum += /*@INV-15*/ m.Direct(20);
        sum += /*@INV-16*/ m.Add(21, 22, 23);
        sum += /*@INV-17*/ m.Add(24, 25, 26);
        sum += /*@INV-18*/ m.Add(27, 28, 29);
        sum += /*@INV-19*/ m.Add(100, 200);
        sum += /*@INV-20*/ m.Add(101, 202);
        sum += /*@INV-21*/ m.Identity(30);
        sum += /*@INV-22*/ m.Identity(31);
        sum += /*@INV-23*/ m.Identity(32);
        sum += /*@INV-24*/ m.Identity(33);
        sum += /*@INV-25*/ m.Identity(34);
        sum += /*@INV-26*/ (int)m.Add(300L, 400L);
        sum += /*@INV-27*/ (int)m.Add(25.0, 26.0);
        sum += /*@INV-28*/ m.WithOptional(35).Length;
        sum += /*@INV-29*/ m.WithOptional(36, 37).Length;
        sum += /*@INV-30*/ m.Named(38).Length;
        sum += /*@INV-31*/ "alpha beta".WordCount();
        sum += /*@INV-32*/ "gamma delta".WordCount();

        System.Action mr01 = /*@MR-01*/ m.Raise;
        System.Func<int, int> mr02 = /*@MR-02*/ m.Direct;
        System.Func<int, int, int> mr03 = /*@MR-03*/ m.Add;
        System.Func<int, int> mr04 = /*@MR-04*/ Methods.Static;
        System.Func<int, int> mr05 = /*@MR-05*/ m.Identity;
        System.Func<int, int, int, int> mr06 = /*@MR-06*/ m.Add;
        System.Func<string, string, string> mr07 = /*@MR-07*/ m.Add;
        System.Func<double, double, double> mr08 = /*@MR-08*/ m.Add;
        System.Func<long, long, long> mr09 = /*@MR-09*/ m.Add;
        System.Func<int, int, string> mr10 = /*@MR-10*/ m.WithOptional;
        System.Func<int, int, string, string> mr11 = /*@MR-11*/ m.Named;
        System.Func<string, string> mr12 = /*@MR-12*/ StringExtensions.Shout;
        System.Func<string, int> mr13 = /*@MR-13*/ StringExtensions.WordCount;
        System.Func<int> mr14 = /*@MR-14*/ PartialType.Left;
        System.Func<int> mr15 = /*@MR-15*/ PartialType.Right;
        System.Func<int, int> mr16 = /*@MR-16*/ m.Direct;
        System.Func<int, int> mr17 = /*@MR-17*/ Corpus.Leaf.Helper.Compute;
        System.Func<int, int> mr18 = /*@MR-18*/ Corpus.Leaf.Helper.Extra;
        System.Func<int, int, int> mr19 = /*@MR-19*/ m.Add;
        System.Func<int, int> mr20 = /*@MR-20*/ m.Direct;

        System.Action all = mr01;
        all += () => { };
        _ = mr02; _ = mr03; _ = mr04; _ = mr05; _ = mr06; _ = mr07; _ = mr08; _ = mr09; _ = mr10;
        _ = mr11; _ = mr12; _ = mr13; _ = mr14; _ = mr15; _ = mr16; _ = mr17; _ = mr18; _ = mr19; _ = mr20;

        return sum;
    }
}
