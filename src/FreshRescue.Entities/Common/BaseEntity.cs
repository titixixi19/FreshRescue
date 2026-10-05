namespace FreshRescue.Entities.Common;

/// <summary>
/// Khoá chính <see cref="Guid"/> sinh ở tầng ứng dụng, không để CSDL sinh
/// (02-DOMAIN-MODEL.md §1, nguyên tắc 1).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
