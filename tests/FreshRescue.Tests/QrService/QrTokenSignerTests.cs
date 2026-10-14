using FluentAssertions;
using FreshRescue.QrService.Services;
using Microsoft.Extensions.Configuration;

namespace FreshRescue.Tests.QrService;

/// <summary>NFR-04: token tự chứa dữ liệu, sửa một ký tự là chữ ký sai.</summary>
public class QrTokenSignerTests
{
    private const string Key = "test-key-only-for-unit-tests-at-least-32-chars";

    private static readonly DateTimeOffset _now = new(2026, 10, 14, 13, 0, 0, TimeSpan.Zero);

    private readonly Guid _orderId = Guid.NewGuid();
    private readonly Guid _storeId = Guid.NewGuid();

    [Fact]
    public void Verify_TokenVuaKy_HopLeVaTraDungDuLieu()
    {
        var signer = CreateSigner(_now);
        var exp = _now.AddHours(1).ToUnixTimeSeconds();

        var result = signer.Verify(signer.Sign(_orderId, _storeId, exp));

        result.Valid.Should().BeTrue();
        result.Payload.Should().Be(new QrPayload(_orderId, _storeId, exp));
    }

    [Fact]
    public void Verify_SuaMotKyTuPayload_BaoChuKySai()
    {
        var signer = CreateSigner(_now);
        var token = signer.Sign(_orderId, _storeId, _now.AddHours(1).ToUnixTimeSeconds());

        var tampered = (token[0] == 'A' ? 'B' : 'A') + token[1..];

        signer.Verify(tampered).ErrorCode.Should().Be(QrTokenSigner.ErrorInvalidSignature);
    }

    [Fact]
    public void Verify_KyBangKhoaKhac_BaoChuKySai()
    {
        var token = CreateSigner(_now, "another-key-also-at-least-32-characters-long")
            .Sign(_orderId, _storeId, _now.AddHours(1).ToUnixTimeSeconds());

        CreateSigner(_now).Verify(token).ErrorCode.Should().Be(QrTokenSigner.ErrorInvalidSignature);
    }

    [Theory]
    [InlineData("")]
    [InlineData("khong-co-dau-cham")]
    [InlineData("a.b.c")]
    [InlineData("!!!.???")]
    public void Verify_TokenRac_BaoChuKySaiKhongNemLoi(string token)
    {
        CreateSigner(_now).Verify(token).ErrorCode.Should().Be(QrTokenSigner.ErrorInvalidSignature);
    }

    [Fact]
    public void Verify_QuaHan_BaoQrExpired()
    {
        var signer = CreateSigner(_now);
        var token = signer.Sign(_orderId, _storeId, _now.AddSeconds(-1).ToUnixTimeSeconds());

        var result = signer.Verify(token);

        result.Valid.Should().BeFalse();
        result.ErrorCode.Should().Be(QrTokenSigner.ErrorExpired);
    }

    [Fact]
    public void KhoiTao_KhoaQuaNgan_BaoLoiRo()
    {
        var act = () => CreateSigner(_now, "short");

        act.Should().Throw<InvalidOperationException>().WithMessage("*QrSigning:Key*");
    }

    private static QrTokenSigner CreateSigner(DateTimeOffset now, string key = Key)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["QrSigning:Key"] = key })
            .Build();
        return new QrTokenSigner(configuration, new FixedTimeProvider(now));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
