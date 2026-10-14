using FluentAssertions;
using FreshRescue.Business.Features.Auth.Dtos;
using FreshRescue.Business.Features.Auth.Validators;

namespace FreshRescue.Tests.Auth;

public class AuthValidatorsTests
{
    [Fact]
    public void RegisterCustomerValidator_HopLeKhiDuLieuDung()
    {
        var validator = new RegisterCustomerRequestValidator();
        var request = new RegisterCustomerRequest
        {
            FullName = "Nguyễn Văn B",
            Email = "customer@example.com",
            Password = "Password123",
            PhoneNumber = "0901234567",
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterCustomerValidator_BaoLoiKhiEmailSaiDinhDang()
    {
        var validator = new RegisterCustomerRequestValidator();
        var request = new RegisterCustomerRequest
        {
            FullName = "Nguyễn Văn B",
            Email = "not-an-email",
            Password = "Password123",
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCustomerRequest.Email));
    }

    [Fact]
    public void RegisterCustomerValidator_BaoLoiKhiMatKhauNganHon6KyTu()
    {
        var validator = new RegisterCustomerRequestValidator();
        var request = new RegisterCustomerRequest
        {
            FullName = "Nguyễn Văn B",
            Email = "valid@example.com",
            Password = "123",
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterCustomerRequest.Password));
    }

    [Fact]
    public void RegisterBrandValidator_HopLeKhiDayDuThongTin()
    {
        var validator = new RegisterBrandRequestValidator();
        var request = new RegisterBrandRequest
        {
            FullName = "Trần Thị C",
            Email = "brand@example.com",
            Password = "Password123",
            BrandName = "Tous les Jours",
            BrandSlug = "tous-les-jours",
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterBrandValidator_BaoLoiKhiSlugChuaKyTuInHoaHoacDauCach()
    {
        var validator = new RegisterBrandRequestValidator();
        var request = new RegisterBrandRequest
        {
            FullName = "Trần Thị C",
            Email = "brand@example.com",
            Password = "Password123",
            BrandName = "Tous les Jours",
            BrandSlug = "Tous Les Jours",
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterBrandRequest.BrandSlug));
    }

    [Fact]
    public void LoginValidator_BaoLoiKhiThieuEmailHoacPassword()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest
        {
            Email = "",
            Password = "",
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
