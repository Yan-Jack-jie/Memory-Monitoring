using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Executor.Actions;
using MemoryMonitoring.Executor.Interop;

var executor = new CleanupExecutor(new NativeMemoryActions());
var sample = new ExecutorRequest(Guid.NewGuid(), Array.Empty<CleanupAction>());
var response = executor.Execute(sample);
Console.WriteLine($"Executor ready: {response.CorrelationId}");
