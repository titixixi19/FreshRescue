using FreshRescue.Business.Common;

namespace FreshRescue.API.Extensions;

/// <summary>T4-TV6-02. Đăng ký gRPC client gọi <c>FreshRescue.QrService</c>.</summary>
public static class QrServiceClientExtensions
{
    public static IServiceCollection AddQrServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        var address = configuration["QrService:Address"]
            ?? throw new InvalidOperationException("Thiếu cấu hình QrService:Address.");

        services.AddGrpcClient<QrService.Protos.QrService.QrServiceClient>(o => o.Address = new Uri(address));
        services.AddScoped<IQrTicketClient, GrpcQrTicketClient>();

        return services;
    }
}
