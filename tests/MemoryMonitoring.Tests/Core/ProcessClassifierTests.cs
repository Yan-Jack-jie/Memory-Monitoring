using MemoryMonitoring.Core.Models;
using MemoryMonitoring.Core.Policies;

namespace MemoryMonitoring.Tests.Core;

public sealed class ProcessClassifierTests
{
    [Fact]
    public void Classifier_ShouldPreferExplicitWhiteListRule()
    {
        var rules = new RuleSet(
            WhiteList: new[] { ProcessRule.WhiteList("vpn.exe") },
            TrimOnly: Array.Empty<ProcessRule>(),
            SuspendEligible: Array.Empty<ProcessRule>());

        var classifier = new ProcessClassifier(rules);

        var result = classifier.Classify("vpn.exe");

        Assert.Equal(ProcessTreatment.WhiteList, result.Treatment);
        Assert.False(result.AllowSuspend);
    }
}
