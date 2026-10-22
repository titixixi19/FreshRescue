namespace FreshRescue.Business.Features.Admin.Dtos;

public record RejectBrandRequest
{
    public string Reason { get; init; } = string.Empty;
}
