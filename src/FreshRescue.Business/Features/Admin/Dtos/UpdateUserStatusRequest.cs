namespace FreshRescue.Business.Features.Admin.Dtos;

public sealed class UpdateUserStatusRequest
{
    public required bool IsActive { get; init; }
}
