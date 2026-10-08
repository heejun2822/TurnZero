using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TurnZero.Tests;

int failed = 0;
var fixture = new BattleTests();
var tests = typeof(BattleTests).GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null).ToArray();
foreach (var test in tests)
{
    try
    {
        test.Invoke(fixture, null);
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (TargetInvocationException exception)
    {
        failed++;
        Console.WriteLine($"FAIL {test.Name}: {exception.InnerException}");
    }
}
Console.WriteLine($"{tests.Length - failed}/{tests.Length} checks passed.");
return failed == 0 ? 0 : 1;
