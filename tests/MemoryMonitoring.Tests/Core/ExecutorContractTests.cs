using System.Text.Json;
using MemoryMonitoring.Core.Contracts;

namespace MemoryMonitoring.Tests.Core;

public sealed class ExecutorContractTests
{
    [Fact]
    public void Request_ShouldRoundTripThroughJson()
    {
        var request = new ExecutorRequest(
            CorrelationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Actions: new[]
            {
                new CleanupAction(CleanupActionType.TrimWorkingSet, 1234, "cherry-studio.exe")
            });

        var json = JsonSerializer.Serialize(request);
        var clone = JsonSerializer.Deserialize<ExecutorRequest>(json);

        Assert.NotNull(clone);
        Assert.Single(clone!.Actions);
        Assert.Equal(CleanupActionType.TrimWorkingSet, clone.Actions[0].Type);
    }
}
