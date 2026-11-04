using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FreshRescue.DataAccess;

/// <summary>
/// Gắn <see cref="DateTimeKind.Utc"/> cho giá trị đọc từ CSDL. Không đổi kiểu cột, không cần
/// migration.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
