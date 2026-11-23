namespace CaseApi.Data;

public class CaseActivityEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CaseId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}