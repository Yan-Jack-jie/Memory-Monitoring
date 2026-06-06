using MemoryMonitoring.Core.Contracts;
using MemoryMonitoring.Infrastructure.Execution;

namespace MemoryMonitoring.Tests.Core;

public sealed class LocalExecutorGatewayTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReportFailureWhenExecutorIsUnavailable()
    {
        var gateway = new LocalExecutorGateway();
        var request = new ExecutorRequest(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            new[]
            {
                new CleanupAction(CleanupActionType.TrimWorkingSet, 42, "sample.exe")
            });

        var response = await gateway.ExecuteAsync(request, CancellationToken.None);

        Assert.Single(response.Results);
        Assert.False(response.Results[0].Success);
        Assert.Contains("执行器", response.Results[0].Message);
    }
}
