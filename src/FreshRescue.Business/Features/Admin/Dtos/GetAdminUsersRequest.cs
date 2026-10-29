namespace FreshRescue.Business.Features.Admin.Dtos;

public sealed class GetAdminUsersRequest
{
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
