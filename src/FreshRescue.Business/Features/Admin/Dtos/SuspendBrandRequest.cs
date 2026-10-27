namespace FreshRescue.Business.Features.Admin.Dtos;

public record SuspendBrandRequest
{
    public string Reason { get; init; } = string.Empty;
}
