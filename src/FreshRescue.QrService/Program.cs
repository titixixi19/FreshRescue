using FreshRescue.QrService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<QrTokenSigner>();

var app = builder.Build();

// Tạo ngay lúc khởi động để thiếu khoá thì báo lỗi luôn, không đợi tới lần cấp vé đầu tiên.
app.Services.GetRequiredService<QrTokenSigner>();

app.MapGrpcService<QrTicketService>();

app.Run();
