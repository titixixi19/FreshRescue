namespace FreshRescue.API.Extensions;

/// <summary>
/// Chủ sở hữu: TV6 (T4-TV6-02). Đăng ký gRPC client gọi <c>FreshRescue.QrService</c>.
/// </summary>
public static class QrServiceClientExtensions
{
    public static IServiceCollection AddQrServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO(TV6, 2026-10-06): AddGrpcClient<...>(o => o.Address = configuration["QrService:Address"]).
        // Cần thêm Grpc.Tools + Google.Protobuf vào Directory.Packages.props để sinh code client — nhắn Tùng.
        return services;
    }
}
