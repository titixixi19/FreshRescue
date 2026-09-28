using System.Reflection;
using FluentAssertions;
using FreshRescue.Business.Common;

namespace FreshRescue.Tests.Common;

public class ErrorCodesTests
{
    private static readonly string[] _allCodes = typeof(ErrorCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral)
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToArray();

    // Contract §1.4 chốt đúng 28 mã; lệch số lượng nghĩa là code và contract đã tách nhau.
    [Fact]
    public void ErrorCodes_KhopSoLuongVoiContract()
    {
        _allCodes.Should().HaveCount(28).And.OnlyHaveUniqueItems();
    }

    [Fact]
    public void ErrorCodes_DeuLaChuHoaVaGachDuoi()
    {
        _allCodes.Should().AllSatisfy(code => code.Should().MatchRegex("^[A-Z]+(_[A-Z]+)*$"));
    }

    [Fact]
    public void AppException_Conflict_Mang409VaDungMaLoi()
    {
        var ex = AppException.Conflict(ErrorCodes.OfferSoldOut, "Hết suất.");

        ex.StatusCode.Should().Be(409);
        ex.ErrorCode.Should().Be("OFFER_SOLD_OUT");
        ex.Message.Should().Be("Hết suất.");
    }
}
