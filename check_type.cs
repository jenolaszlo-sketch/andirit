using System;
using System.Reflection;
using System.IO;

class Program
{
    static void Main()
    {
        var asmPath = @"D:\projects\andirit\src\Andirit.LatticeDb\bin\Release\net10.0\Andirit.LatticeDb.dll";
        var asm = Assembly.LoadFrom(asmPath);
        var type = asm.GetType("Andirit.LatticeDb.LatticeCodeGraphStore");
        Console.WriteLine($"Type found: {type != null}");
        if (type != null)
        {
            Console.WriteLine($"  IsPublic: {type.IsPublic}");
            Console.WriteLine($"  IsSealed: {type.IsSealed}");
            Console.WriteLine($"  FullName: {type.FullName}");
            Console.WriteLine($"  Namespace: {type.Namespace}");
            foreach (var ctor in type.GetConstructors())
            {
                Console.WriteLine($"  Constructor: {ctor}");
            }
        }
        else
        {
            Console.WriteLine("Type NOT FOUND in assembly");
            foreach (var t in asm.GetTypes())
            {
                if (t.Name.Contains("LatticeCode"))
                {
                    Console.WriteLine($"  Found similar: {t.FullName}");
                }
            }
        }
    }
}