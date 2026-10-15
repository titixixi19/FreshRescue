using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FreshRescue.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace FreshRescue.LoadTest;

/// <summary>
/// Load test NFR-01: N khách bấm mua cùng lúc một offer chỉ còn Q suất, kiểm tra đúng Q đơn
/// thành công và tồn kho không âm. Chạy hai lần với <c>Ordering:LockingStrategy</c> khác nhau
/// để lấy số liệu so sánh cho báo cáo (02-DOMAIN-MODEL.md §6).
/// </summary>
internal static class Program
{
    private const string Password = "LoadTest123";

    // ID cố định để chạy lại bao nhiêu lần cũng không sinh rác trong DB.
    private static readonly Guid _brandId = new("10AD7E57-0000-0000-0000-000000000001");
    private static readonly Guid _storeId = new("10AD7E57-0000-0000-0000-000000000002");
    private static readonly Guid _categoryId = new("10AD7E57-0000-0000-0000-000000000003");
    private static readonly Guid _templateId = new("10AD7E57-0000-0000-0000-000000000004");
    private static readonly Guid _batchId = new("10AD7E57-0000-0000-0000-000000000005");
    private static readonly Guid _offerId = new("10AD7E57-0000-0000-0000-000000000006");

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public static async Task<int> Main(string[] args)
    {
        var options = LoadTestOptions.Parse(args);
        if (options is null)
        {
            Console.WriteLine(LoadTestOptions.Usage);
            return 1;
        }

        // API chuyển hướng HTTP sang HTTPS và HttpClient bỏ header Authorization khi theo redirect,
        // nên gọi thẳng HTTPS và chấp nhận chứng chỉ dev của máy.
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = options.Users * 2,
            AllowAutoRedirect = false,
            SslOptions = { RemoteCertificateValidationCallback = (_, _, _, _) => true },
        };
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.Api.TrimEnd('/') + "/api/v1/"),
            Timeout = TimeSpan.FromSeconds(60),
        };

        var dbOptions = new DbContextOptionsBuilder<FreshRescueDbContext>()
            .UseSqlServer(options.Connection)
            .Options;
        await using var db = new FreshRescueDbContext(dbOptions, TimeProvider.System);

        Console.WriteLine($"API: {options.Api}  |  chế độ: {options.Label}  |  {options.Users} khách tranh {options.Stock} suất  |  {options.Rounds} lượt đo (+{options.Warmup} lượt làm nóng)");
        Console.WriteLine();

        Console.Write($"Tạo {options.Users} tài khoản khách... ");
        var tokens = await PrepareCustomersAsync(http, options.Users);
        Console.WriteLine("xong.");

        await SeedAsync(db);

        var results = new List<RoundResult>();
        var totalRounds = options.Warmup + options.Rounds;
        for (var round = 1; round <= totalRounds; round++)
        {
            await ResetAsync(db, options.Stock);
            var result = await RunRoundAsync(http, db, tokens, options.Stock);
            var isWarmup = round <= options.Warmup;
            Console.WriteLine($"{(isWarmup ? "làm nóng" : $"lượt {round - options.Warmup}"),-9} {result}");
            if (!isWarmup)
            {
                results.Add(result);
            }
        }

        Console.WriteLine();
        PrintSummary(options, results);
        return results.Any(r => r.Oversold) ? 2 : 0;
    }

    /// <summary>Đăng ký (hoặc đăng nhập nếu đã có) N khách, trả về access token của từng người.</summary>
    private static async Task<string[]> PrepareCustomersAsync(HttpClient http, int users)
    {
        var tokens = new string[users];
        // Identity băm mật khẩu khá nặng — chạy 10 người một lúc để không làm API nghẽn trước khi đo.
        await Parallel.ForAsync(0, users, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (i, ct) =>
        {
            var email = $"load-{i:D3}@freshrescue.test";
            var register = await http.PostAsJsonAsync("auth/register", new { fullName = $"Khách tải {i:D3}", email, password = Password }, ct);
            if (register.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.Created or HttpStatusCode.Conflict or HttpStatusCode.BadRequest))
            {
                throw new InvalidOperationException($"Đăng ký {email} thất bại: {(int)register.StatusCode} {await register.Content.ReadAsStringAsync(ct)}");
            }

            var login = await http.PostAsJsonAsync("auth/login", new { email, password = Password }, ct);
            login.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync(ct));
            tokens[i] = doc.RootElement.GetProperty("accessToken").GetString()
                ?? throw new InvalidOperationException("Phản hồi đăng nhập không có accessToken.");
        });
        return tokens;
    }

    /// <summary>Dựng thương hiệu, chi nhánh, đợt bán và offer cho bài đo. Chạy lại không tạo trùng.</summary>
    private static async Task SeedAsync(FreshRescueDbContext db)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($@"
SET XACT_ABORT ON;
DECLARE @now datetime2 = SYSUTCDATETIME();
DECLARE @owner uniqueidentifier = (SELECT TOP 1 Id FROM AspNetUsers WHERE Email LIKE 'load-%@freshrescue.test' ORDER BY Email);
IF NOT EXISTS (SELECT 1 FROM Brands WHERE Id = {_brandId})
BEGIN
  INSERT Brands (Id, Name, Slug, OwnerUserId, Status, IsDeleted, CreatedAt)
    VALUES ({_brandId}, N'LOAD Brand', 'load-brand', @owner, 1, 0, @now);
  INSERT Stores (Id, BrandId, Name, AddressLine, Ward, District, City, Latitude, Longitude, Phone, OpenTime, CloseTime, Status, RatingAverage, RatingCount, IsDeleted, CreatedAt)
    VALUES ({_storeId}, {_brandId}, N'LOAD Chi nhánh', N'1 Đường Đo', N'Phường Đo', N'Quận 1', N'TP.HCM', 10.776, 106.700, '0900000001', '07:00', '22:00', 1, 0, 0, 0, @now);
  INSERT Categories (Id, Name, Slug, DisplayOrder, IsDeleted)
    VALUES ({_categoryId}, N'LOAD Bánh', 'load-banh', 98, 0);
  INSERT OfferTemplates (Id, BrandId, CategoryId, Name, OfferType, OriginalPrice, SuggestedPrice, BrandMinDiscountPercent, BrandMaxDiscountPercent, IsDeleted, CreatedAt)
    VALUES ({_templateId}, {_brandId}, {_categoryId}, N'LOAD Túi mù', 1, 100000, 30000, 60, 75, 0, @now);
  INSERT SaleBatches (Id, StoreId, Status, SaleStartTime, SaleEndTime, PickupStartTime, PickupEndTime, CreatedByUserId, CreatedAt)
    VALUES ({_batchId}, {_storeId}, 2, DATEADD(HOUR, -1, @now), DATEADD(HOUR, 2, @now), DATEADD(MINUTE, -30, @now), DATEADD(HOUR, 3, @now), @owner, @now);
  INSERT Offers (Id, SaleBatchId, OfferTemplateId, Name, CategoryId, OfferType, OriginalPrice, ActualPrice, TotalQuantity, RemainingQuantity, MaxPerCustomer, IsDeleted, CreatedAt)
    VALUES ({_offerId}, {_batchId}, {_templateId}, N'LOAD Túi mù', {_categoryId}, 1, 100000, 30000, 10, 10, 1, 0, @now);
END
UPDATE SaleBatches SET Status = 2,
  SaleStartTime = DATEADD(HOUR, -1, @now), SaleEndTime = DATEADD(HOUR, 2, @now),
  PickupStartTime = DATEADD(MINUTE, -30, @now), PickupEndTime = DATEADD(HOUR, 3, @now)
WHERE Id = {_batchId};");
    }

    /// <summary>Xoá đơn của lượt trước và nạp lại đúng Q suất. BR-06 không cản vì mỗi khách chỉ mua 1.</summary>
    private static async Task ResetAsync(FreshRescueDbContext db, int stock)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($@"
SET XACT_ABORT ON;
DELETE QrTickets WHERE OrderId IN (SELECT Id FROM Orders WHERE SaleBatchId = {_batchId});
DELETE OrderItems WHERE OrderId IN (SELECT Id FROM Orders WHERE SaleBatchId = {_batchId});
DELETE Orders WHERE SaleBatchId = {_batchId};
UPDATE Offers SET TotalQuantity = {stock}, RemainingQuantity = {stock}, MaxPerCustomer = 1, IsDeleted = 0 WHERE Id = {_offerId};");
    }

    private static async Task<RoundResult> RunRoundAsync(HttpClient http, FreshRescueDbContext db, string[] tokens, int stock)
    {
        var body = JsonSerializer.Serialize(new { saleBatchId = _batchId, items = new[] { new { offerId = _offerId, quantity = 1 } } }, _json);

        // Mọi task dựng xong rồi mới mở cổng, để N request rời máy gần như cùng một thời điểm.
        var gate = new TaskCompletionSource();
        var tasks = tokens.Select(async token =>
        {
            await gate.Task;
            var sw = Stopwatch.StartNew();
            using var request = new HttpRequestMessage(HttpMethod.Post, "orders")
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                using var response = await http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                sw.Stop();
                return new RequestResult((int)response.StatusCode, ReadErrorCode(content), sw.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                sw.Stop();
                return new RequestResult(0, ex.GetType().Name, sw.Elapsed.TotalMilliseconds);
            }
        }).ToArray();

        var wall = Stopwatch.StartNew();
        gate.SetResult();
        var results = await Task.WhenAll(tasks);
        wall.Stop();

        var remaining = await db.Database.SqlQuery<int>($"SELECT RemainingQuantity AS [Value] FROM Offers WHERE Id = {_offerId}").SingleAsync();
        var sold = await db.Database.SqlQuery<int>($"SELECT ISNULL(SUM(oi.Quantity), 0) AS [Value] FROM OrderItems oi JOIN Orders o ON o.Id = oi.OrderId WHERE o.SaleBatchId = {_batchId}").SingleAsync();

        return new RoundResult(results, stock, remaining, sold, wall.Elapsed.TotalMilliseconds);
    }

    private static string? ReadErrorCode(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void PrintSummary(LoadTestOptions options, List<RoundResult> rounds)
    {
        var all = rounds.SelectMany(r => r.Requests).ToList();
        var successes = rounds.Select(r => r.Successes).ToList();
        var conflicts = all.Count(r => r.ErrorCode == "CONCURRENCY_CONFLICT");
        var soldOut = all.Count(r => r.ErrorCode == "OFFER_SOLD_OUT");
        var other = all.Count(r => r.StatusCode != 201 && r.ErrorCode is not ("CONCURRENCY_CONFLICT" or "OFFER_SOLD_OUT"));

        Console.WriteLine($"## Kết quả — {options.Label} ({options.Users} khách / {options.Stock} suất / {rounds.Count} lượt)");
        Console.WriteLine();
        Console.WriteLine("| Chỉ số | Giá trị |");
        Console.WriteLine("| --- | --- |");
        Console.WriteLine($"| Đơn thành công mỗi lượt | {successes.Min()}–{successes.Max()} / {options.Stock} (trung bình {successes.Average():0.0}) |");
        Console.WriteLine($"| Lượt đạt đúng {options.Stock}/{options.Stock} | {successes.Count(s => s == options.Stock)}/{rounds.Count} |");
        Console.WriteLine($"| 409 OFFER_SOLD_OUT | {soldOut} |");
        Console.WriteLine($"| 409 CONCURRENCY_CONFLICT (còn hàng vẫn bị từ chối) | {conflicts} |");
        Console.WriteLine($"| Mã khác / lỗi mạng | {other} |");
        Console.WriteLine($"| Bán lố (over-sell) | {(rounds.Any(r => r.Oversold) ? "CÓ" : "không")} |");
        Console.WriteLine($"| Độ trễ p50 / p95 / max (ms) | {Percentile(all, 0.50):0} / {Percentile(all, 0.95):0} / {all.Max(r => r.Milliseconds):0} |");
        Console.WriteLine($"| Độ trễ đơn thành công p50 (ms) | {Percentile(all.Where(r => r.StatusCode == 201).ToList(), 0.50):0} |");
        Console.WriteLine($"| Thời gian xử lý hết {options.Users} request, trung bình (ms) | {rounds.Average(r => r.WallMilliseconds):0} |");
    }

    private static double Percentile(List<RequestResult> requests, double p)
    {
        if (requests.Count == 0)
        {
            return 0;
        }

        var sorted = requests.Select(r => r.Milliseconds).OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(p * sorted.Count) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }
}

internal sealed record RequestResult(int StatusCode, string? ErrorCode, double Milliseconds);

internal sealed record RoundResult(RequestResult[] Requests, int Stock, int Remaining, int Sold, double WallMilliseconds)
{
    public int Successes => Requests.Count(r => r.StatusCode == 201);

    // Bán lố khi kho âm, hoặc số suất đã bán không khớp số suất đã trừ.
    public bool Oversold => Remaining < 0 || Sold > Stock || Remaining != Stock - Sold || Successes != Sold;

    public override string ToString()
    {
        var codes = Requests.Where(r => r.StatusCode != 201)
            .GroupBy(r => r.ErrorCode ?? r.StatusCode.ToString(CultureInfo.InvariantCulture))
            .Select(g => $"{g.Key}×{g.Count()}");
        return $"{Successes,2}/{Stock} thành công | kho còn {Remaining,2} | {WallMilliseconds,5:0} ms | {string.Join(", ", codes)}{(Oversold ? "  ⚠ BÁN LỐ" : "")}";
    }
}

internal sealed record LoadTestOptions(string Api, string Connection, int Users, int Stock, int Rounds, int Warmup, string Label)
{
    public const string Usage = """
        Cách dùng: dotnet run --project tools/FreshRescue.LoadTest -- [tuỳ chọn]
          --api <url>          Địa chỉ API (mặc định https://localhost:7001)
          --connection <cs>    Chuỗi kết nối SQL Server (mặc định LocalDB FreshRescue)
          --users <n>          Số khách bấm mua cùng lúc (mặc định 50)
          --stock <n>          Số suất còn lại của offer (mặc định 10)
          --rounds <n>         Số lượt đo (mặc định 5)
          --warmup <n>         Số lượt làm nóng không tính (mặc định 1)
          --label <tên>        Nhãn in ra, ví dụ Pessimistic / Optimistic
        """;

    public static LoadTestOptions? Parse(string[] args)
    {
        var api = "https://localhost:7001";
        var connection = "Server=(localdb)\\MSSQLLocalDB;Database=FreshRescue;Trusted_Connection=True;TrustServerCertificate=True";
        int users = 50, stock = 10, rounds = 5, warmup = 1;
        var label = "(chưa đặt nhãn)";

        for (var i = 0; i < args.Length; i++)
        {
            var value = i + 1 < args.Length ? args[i + 1] : null;
            switch (args[i])
            {
                case "--api" when value is not null: api = value; i++; break;
                case "--connection" when value is not null: connection = value; i++; break;
                case "--users" when value is not null: users = int.Parse(value, CultureInfo.InvariantCulture); i++; break;
                case "--stock" when value is not null: stock = int.Parse(value, CultureInfo.InvariantCulture); i++; break;
                case "--rounds" when value is not null: rounds = int.Parse(value, CultureInfo.InvariantCulture); i++; break;
                case "--warmup" when value is not null: warmup = int.Parse(value, CultureInfo.InvariantCulture); i++; break;
                case "--label" when value is not null: label = value; i++; break;
                default: return null;
            }
        }

        return new LoadTestOptions(api, connection, users, stock, rounds, warmup, label);
    }
}
