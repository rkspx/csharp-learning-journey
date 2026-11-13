namespace CaseLifecycle;

public class Case
{
    public CaseStatus Status { get; private set; }

    public Case()
    {
        Status = CaseStatus.New;
    }

    public void StartInvestigation()
    {
        if (Status is not CaseStatus.New)
        {
            throw new InvalidOperationException($"Can not start investigation on case with status '{Status}'");
        }

        Status = CaseStatus.Investigating;
    }

    public void RequestClosure()
    {
        if (Status is not CaseStatus.Investigating)
        {
            throw new InvalidOperationException($"Can not request closure on case with status '{Status}'");
        }

        Status = CaseStatus.ClosureRequested;
    }

    public void Close()
    {
        if (Status is not CaseStatus.ClosureRequested)
        {
            throw new InvalidOperationException($"Can not close case with status '{Status}'");
        }

        Status = CaseStatus.Closed;
    }
}