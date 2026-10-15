namespace FreshRescue.WebClient.Models;

/// <summary>Model cho <c>Views/Shared/_Pager.cshtml</c> — dùng chung Customer và Dashboard.</summary>
/// <param name="Page">Trang hiện tại, bắt đầu từ 1.</param>
/// <param name="TotalPages">Lấy thẳng từ <c>PagedResponse.TotalPages</c>.</param>
/// <param name="UrlForPage">Sinh link cho một số trang, giữ nguyên các tham số lọc khác.</param>
public record PagerModel(int Page, int TotalPages, Func<int, string?> UrlForPage);
