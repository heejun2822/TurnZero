using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace TurnZero.Tests
{
    [InitializeOnLoad]
    public sealed class LocalTestRun : ICallbacks
    {
        private const string Pending = "TurnZero.Checks.Pending";
        private static TestRunnerApi runner;

        static LocalTestRun()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(Pending, "")))
            {
                Register();
            }
        }

        private static void Register()
        {
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new LocalTestRun());
        }

        public static string Begin(string mode, string outputPath, string[] tests = null)
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(Pending, "")))
            {
                throw new InvalidOperationException("A project test run is already pending.");
            }

            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            SessionState.SetString(Pending, outputPath);
            Register();

            return runner.Execute(new ExecutionSettings(new Filter
            {
                testMode = (TestMode)Enum.Parse(typeof(TestMode), mode),
                testNames = tests
            }));
        }

        public void RunStarted(ITestAdaptor tests)
        {
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            string path = SessionState.GetString(Pending, "");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            TestRunnerApi.SaveResultToFile(result, path);
            SessionState.EraseString(Pending);

            Debug.Log($"TurnZero tests: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped. {path}");
        }
    }
}
