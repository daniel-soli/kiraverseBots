// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Extensions.Logging;
//
// namespace KiraVerse.Bots.Functions;
//
// public class TestFunction
// {
//     public TestFunction()
//     {
//         
//     }
//
//     [Function("TestFunction")]
//     public async Task ImxBotGenesis(
//         [TimerTrigger("0 */10 * * * *", RunOnStartup = false)]
//         TimerInfo myTimer,
//         ILogger log)
//     {
//         log.LogInformation("Running the function");
//     }
// }