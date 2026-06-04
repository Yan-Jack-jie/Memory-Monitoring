using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Executor.Actions;
using MemoryMonitoring.Executor.Interop;
using MemoryMonitoring.Executor.Services;

var executor = new CleanupExecutor(new NativeMemoryActions());

if (args.Contains("--serve", StringComparer.OrdinalIgnoreCase))
{
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cts.Cancel();
    };

    var server = new ExecutorServer(executor);
    await server.RunAsync(cts.Token);
    return;
}

var sample = new ExecutorRequest(Guid.NewGuid(), Array.Empty<CleanupAction>());
var response = executor.Execute(sample);
Console.WriteLine($"Executor ready: {response.CorrelationId}");
