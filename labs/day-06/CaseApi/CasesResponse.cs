namespace CaseApi;

public record CasesResponse(
    List<CaseResponse> Cases,
    int Page,
    int PageSize
);