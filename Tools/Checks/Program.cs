using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TurnZero.Tests;

int failed = 0;
int total = 0;
foreach (var fixture in new object[] { new BattleTests(), new TurnZero.Tests.LocalBattleTests(), new TurnZero.Tests.SpecificationTests() })
foreach (var test in fixture.GetType().GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null))
{
    total++;
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
Console.WriteLine($"{total - failed}/{total} checks passed.");
return failed == 0 ? 0 : 1;
