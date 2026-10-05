using System.Security.Cryptography;

namespace FreshRescue.Business.Features.Ordering;

internal static class OrderCodeGenerator
{
    // OQ-19: bỏ O 0 I 1 để nhân viên đọc mã bằng miệng không nhầm. 32 ký tự × 8 vị trí.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate() => "FR-" + RandomNumberGenerator.GetString(Alphabet, 8);
}
