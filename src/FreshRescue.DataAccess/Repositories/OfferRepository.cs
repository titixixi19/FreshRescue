using FreshRescue.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.DataAccess.Repositories;

public class OfferRepository : GenericRepository<Offer>, IOfferRepository
{
    public OfferRepository(FreshRescueDbContext context)
        : base(context)
    {
    }

    public Task<List<Offer>> GetForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        // Không ORDER BY được trong câu SQL vì EF bọc nó thành truy vấn con để thêm bộ lọc xoá mềm.
        // Không sao: SQL Server lấy khoá theo thứ tự khoá chính khi tìm theo danh sách IN, nên
        // mọi giao dịch khoá cùng một thứ tự — đúng yêu cầu chống deadlock của 02 §6.2.
        // Chỉ ghép chỗ giữ tham số {0}, {1}…; giá trị Id vẫn đi qua tham số, không chèn vào SQL.
        var placeholders = string.Join(", ", ids.Select((_, i) => "{" + i + "}"));
        var sql = "SELECT * FROM [Offers] WITH (UPDLOCK, ROWLOCK) WHERE [Id] IN (" + placeholders + ")";
        return Set
            .FromSqlRaw(sql, ids.Cast<object>().ToArray())
            .ToListAsync(cancellationToken);
    }
}
