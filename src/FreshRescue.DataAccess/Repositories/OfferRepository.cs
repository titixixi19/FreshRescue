using System.Data.SqlTypes;
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

    public async Task RestockAsync(IReadOnlyDictionary<Guid, int> quantities, CancellationToken cancellationToken)
    {
        // 02 §6.2: khoá các dòng offer cùng thứ tự với lúc đặt hàng — SQL Server xếp uniqueidentifier
        // khác Guid.CompareTo của .NET, nên sắp theo SqlGuid cho khớp thứ tự khoá của GetForUpdateAsync.
        foreach (var (offerId, quantity) in quantities.OrderBy(q => new SqlGuid(q.Key)))
        {
            // Offer có thể đã bị xoá mềm sau khi khách đặt; vẫn phải trả về cho đúng sổ sách.
            await Set
                .IgnoreQueryFilters()
                .Where(o => o.Id == offerId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(
                        o => o.RemainingQuantity,
                        o => o.RemainingQuantity + quantity > o.TotalQuantity
                            ? o.TotalQuantity
                            : o.RemainingQuantity + quantity),
                    cancellationToken);
        }
    }

    public async Task<bool> TryAdjustTotalAsync(Guid offerId, int newTotalQuantity, CancellationToken cancellationToken)
    {
        // Điều kiện nằm trong WHERE để đua với đơn đang đặt vẫn không bao giờ làm kho âm (02 §6.4):
        // RemainingQuantity + (mới − cũ) >= 0  ⇔  mới >= số đã giữ chỗ hoặc đã bán.
        var affected = await Set
            .Where(o => o.Id == offerId && o.RemainingQuantity + (newTotalQuantity - o.TotalQuantity) >= 0)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(o => o.RemainingQuantity, o => o.RemainingQuantity + (newTotalQuantity - o.TotalQuantity))
                    .SetProperty(o => o.TotalQuantity, newTotalQuantity),
                cancellationToken);
        return affected == 1;
    }
}
