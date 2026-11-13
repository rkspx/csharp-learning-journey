using CaseLifecycle;

Case incident = new();
Console.WriteLine($"initial status: {incident.Status}");

incident.StartInvestigation();
Console.WriteLine($"status after investigation has been started: {incident.Status}");


incident.RequestClosure();
Console.WriteLine($"status after closure request: {incident.Status}");


incident.Close();
Console.WriteLine($"status after closure: {incident.Status}");

List<Case> cases = [new Case(), new Case(), new Case()];

try
{ 
    cases[1].StartInvestigation();
    cases[2].StartInvestigation();
    cases[2].RequestClosure();
    cases[2].Close();
} catch (InvalidOperationException e)
{
    Console.Error.WriteLine($"Invalid operation: {e.Message}");
    return 1;
}

int openCount = 0;
foreach(Case currentCase in cases)
{
    Console.WriteLine(currentCase.Status);
    if (currentCase.Status is not CaseStatus.Closed)
    {
        openCount++;
    }
}

Console.WriteLine($"Open cases count: {openCount}");

Case first = new();
Case second = new();
Console.WriteLine(first == second);

CaseSnapshot firstSnapshot = new("case-001", CaseStatus.New);
CaseSnapshot secondSnapshot = new("case-001", CaseStatus.New);
Console.WriteLine(firstSnapshot == secondSnapshot);

CaseSnapshot thirdSnapshot = secondSnapshot with { Id = "case-002" };
Console.WriteLine(thirdSnapshot == secondSnapshot);
return 0;