using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>Runs only PlayerScene's control regressions without changing build scenes.</summary>
[InitializeOnLoad]
public static class PlayerCombatTestRunner
{
    private static readonly TestRunnerApi Api;
    static PlayerCombatTestRunner()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Api.RegisterCallbacks(new Results());
    }
    [MenuItem("Tools/Player/Run Combat Control Tests")]
    public static void Run()
    {
        Directory.CreateDirectory("Temp/PlayerCombatTests");
        File.WriteAllText("Temp/PlayerCombatTests/status.txt","Running");
        SessionState.SetBool("PlayerCombatTests.Running",true);
        Api.Execute(new ExecutionSettings(new Filter { testMode=TestMode.PlayMode, testNames=new[]{"MainHackathonGame.Tests.PlayerCombatControlsTests"} }));
    }
    private sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor test) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result)
        {
            if(SessionState.GetBool("PlayerCombatTests.Running",false) && !result.Test.IsSuite)
                File.AppendAllText("Temp/PlayerCombatTests/status.txt","\n"+result.Name+": "+result.TestStatus+" "+result.Message);
        }
        public void RunFinished(ITestResultAdaptor result)
        {
            if(!SessionState.GetBool("PlayerCombatTests.Running",false)) return;
            SessionState.SetBool("PlayerCombatTests.Running",false);
            TestRunnerApi.SaveResultToFile(result,"Temp/PlayerCombatTests/results.xml");
            File.AppendAllText("Temp/PlayerCombatTests/status.txt","\nCOMPLETE Passed="+result.PassCount+" Failed="+result.FailCount);
        }
    }
}
