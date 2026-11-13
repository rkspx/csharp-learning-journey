using CaseLifecycle;
using Xunit;

namespace CaseLifecycle.Tests;

public class Casetests
{
    [Theory]
    [InlineData(0, CaseStatus.New)]
    [InlineData(1, CaseStatus.Investigating)]
    [InlineData(2, CaseStatus.ClosureRequested)]
    [InlineData(3, CaseStatus.Closed)]
    public void ValidSequence_ReachesExpectedStatus(
        int transitionCount, CaseStatus expectedStatus
    )
    {
        Case cs = new();
        if (transitionCount > 0)
        {   
            cs.StartInvestigation();
        }

        if (transitionCount > 1)
        {   
            cs.RequestClosure();
        }

        if (transitionCount > 2)
        {   
            cs.Close();
        }

        Assert.Equal(expectedStatus, cs.Status);
    }

    [Fact]
    public void StartInvestigation_FromInvestigating_ThrowsAndPreservesStatus()
    {
        Case cs = new();
        cs.StartInvestigation();

        Assert.Throws<InvalidOperationException>(() => cs.StartInvestigation());
        Assert.Equal(CaseStatus.Investigating, cs.Status);
    }

    [Fact]
    public void StartInvestigation_FromClosureRequested_ThrowsAndPreservesStatus()
    {
        Case cs = new();
        cs.StartInvestigation();
        cs.RequestClosure();

        Assert.Throws<InvalidOperationException>(() => cs.StartInvestigation());
        Assert.Equal(CaseStatus.ClosureRequested, cs.Status);
    }

    [Fact]
    public void StartInvestigation_FromClosed_ThrowsAndPreservesStatus()
    {
        Case cs = new();
        cs.StartInvestigation();
        cs.RequestClosure();
        cs.Close();

        Assert.Throws<InvalidOperationException>(() => cs.StartInvestigation());
        Assert.Equal(CaseStatus.Closed, cs.Status);
    }

    [Fact]
    public void RequestClosure_FromNew_ThrowsAndPreservesStatus()
    {
        Case cs = new();

        Assert.Throws<InvalidOperationException>(() => cs.RequestClosure());
        Assert.Equal(CaseStatus.New, cs.Status);
    }

    [Fact]
    public void RequestClosure_FromClosureRequested_ThrowsAndPreservesStatus()
    {
        Case cs = new();
        cs.StartInvestigation();
        cs.RequestClosure();

        Assert.Throws<InvalidOperationException>(() => cs.RequestClosure());
        Assert.Equal(CaseStatus.ClosureRequested, cs.Status);
    }

    [Fact]
    public void RequestClosure_FromClosed_ThrowsAndPreservesStatus()
    {
        Case cs = new();
        cs.StartInvestigation();
        cs.RequestClosure();
        cs.Close();

        Assert.Throws<InvalidOperationException>(() => cs.RequestClosure());
        Assert.Equal(CaseStatus.Closed, cs.Status);
    }

    [Fact]
    public void Close_FromNew_ThrowsAndPreservesStatus()
    {
        Case cs = new();

        Assert.Throws<InvalidOperationException>(() => cs.Close());
        Assert.Equal(CaseStatus.New, cs.Status);
    }

    [Fact]
    public void Close_FromInvestigating_ThrowsAndPreservesStatus()
    {
        Case cs = new();
        cs.StartInvestigation();

        Assert.Throws<InvalidOperationException>(() => cs.Close());
        Assert.Equal(CaseStatus.Investigating, cs.Status);
    }

    [Fact]
    public void Close_FromClosed_ThrowsAndPreservesStatus()
    {
        Case cs = new();
        cs.StartInvestigation();
        cs.RequestClosure();
        cs.Close();

        Assert.Throws<InvalidOperationException>(() => cs.Close());
        Assert.Equal(CaseStatus.Closed, cs.Status);
    }
}
