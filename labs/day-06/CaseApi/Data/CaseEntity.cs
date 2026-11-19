namespace CaseApi.Data;

public class CaseEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string? AssignedTo { get; set; }
}