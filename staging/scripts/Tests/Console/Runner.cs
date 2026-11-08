// 此檔只供 console；不要複製進 Unity。
using System;
using System.Linq;
using System.IO;
using System.Reflection;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TestCaseAttribute : Attribute
    {
        public object[] Arguments { get; }
        public TestCaseAttribute(params object[] arguments) { Arguments = arguments; }
    }
}

internal static class Runner
{
    private static int Main()
    {
        int passed = 0, failed = 0;
        var types = new[] { typeof(Liuruan.Tests.HungerCoreTests), typeof(Liuruan.Tests.CompanionCoreTests),
            typeof(Liuruan.Tests.MinimapFogCoreTests), typeof(Liuruan.Tests.ClimbCoreTests),
            typeof(Liuruan.Tests.DriftCoreTests), typeof(Liuruan.Tests.SeasonCoreTests),
            typeof(Liuruan.Tests.EpilogueCoreTests), typeof(Liuruan.Tests.CharacterBobCoreTests),
            typeof(Liuruan.Tests.StallHintCoreTests), typeof(Liuruan.Tests.DialogueCoreTests),
            typeof(Liuruan.Tests.ObjectiveCoreTests), typeof(Liuruan.Tests.RescueCoreTests),
            typeof(Liuruan.Tests.OffscreenArrowCoreTests), typeof(Liuruan.Tests.ReadingCoreTests) };
        string content = Path.Combine(Liuruan.Tests.R3Fixture.Root, "docs/content/r3");
        string[] contentFiles = { "hints", "dialogue", "objectives", "sentences", "glossary_cards" };
        if (contentFiles.All(name => File.Exists(Path.Combine(content, name + ".json"))))
            types = types.Concat(new[] { typeof(Liuruan.Tests.R3ContentCoreTests) }).ToArray();
        else Console.WriteLine("SKIP R3ContentCoreTests: C 線五份 JSON 尚未齊備；固定樣本測試照常執行。");
        foreach (Type type in types)
        foreach (MethodInfo method in type.GetMethods().OrderBy(m => m.Name))
        {
            var cases = method.GetCustomAttributes<NUnit.Framework.TestCaseAttribute>().ToArray();
            bool simple = method.IsDefined(typeof(NUnit.Framework.TestAttribute), false);
            if (!simple && cases.Length == 0) continue;
            var arguments = cases.Length > 0 ? cases.Select(c => c.Arguments) : new[] { new object[0] };
            foreach (object[] args in arguments)
            {
                string name = type.Name + "." + method.Name + "(" + string.Join(",", args) + ")";
                try
                {
                    method.Invoke(Activator.CreateInstance(type), args);
                    Console.WriteLine("PASS " + name); passed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("FAIL " + name + ": " + (e.InnerException ?? e)); failed++;
                }
            }
        }
        Console.WriteLine($"RESULT: {passed} passed, {failed} failed");
        return failed == 0 && passed > 0 ? 0 : 1;
    }
}
