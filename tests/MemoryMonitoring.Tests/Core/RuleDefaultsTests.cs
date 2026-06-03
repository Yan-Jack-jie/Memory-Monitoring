using MemoryMonitoring.Core.Models;

namespace MemoryMonitoring.Tests.Core;

public sealed class RuleDefaultsTests
{
    [Fact]
    public void WhiteListRule_ShouldNeverAllowSuspend()
    {
        var rule = ProcessRule.WhiteList("WeChat.exe");

        Assert.Equal("WeChat.exe", rule.ProcessName);
        Assert.Equal(ProcessTreatment.WhiteList, rule.Treatment);
        Assert.False(rule.AllowSuspend);
    }

    [Fact]
    public void DefaultPolicy_ShouldStartInBalancedMode()
    {
        var settings = MemoryPolicySettings.CreateDefault();

        Assert.Equal(85, settings.MemoryLoadPercentThreshold);
        Assert.Equal(2_048, settings.AvailableMemoryThresholdMb);
        Assert.Equal(45, settings.SustainedPressureSeconds);
        Assert.Equal(90, settings.StartupDelaySeconds);
    }
}
