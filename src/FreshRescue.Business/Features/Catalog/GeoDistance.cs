namespace FreshRescue.Business.Features.Catalog;

/// <summary>
/// Toán toạ độ cho "cửa hàng gần tôi". Lọc hộp bao trước để dùng được chỉ mục
/// <c>(Latitude, Longitude)</c>, rồi mới tính Haversine trên tập nhỏ (02-DOMAIN-MODEL.md §5.1).
/// </summary>
public static class GeoDistance
{
    private const double EarthRadiusKm = 6371.0088;

    /// <summary>1 độ vĩ ≈ 111 km.</summary>
    private const double KmPerDegreeLatitude = 111.0;

    /// <summary>Khoảng cách cung tròn lớn giữa hai điểm, đơn vị km.</summary>
    public static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2)
            + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Pow(Math.Sin(dLng / 2), 2);

        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>Hộp bao chữ nhật quanh một điểm, rộng hơn hoặc bằng hình tròn bán kính <paramref name="radiusKm"/>.</summary>
    public static (decimal MinLat, decimal MaxLat, decimal MinLng, decimal MaxLng) BoundingBox(
        double lat, double lng, double radiusKm)
    {
        var dLat = radiusKm / KmPerDegreeLatitude;

        // 1 độ kinh ≈ 111 km × cos(vĩ độ); gần cực cos → 0 nên cho hộp phủ hết kinh độ.
        var cosLat = Math.Cos(ToRadians(lat));
        var dLng = cosLat < 0.01 ? 180.0 : radiusKm / (KmPerDegreeLatitude * cosLat);

        return (
            (decimal)(lat - dLat),
            (decimal)(lat + dLat),
            (decimal)Math.Max(-180, lng - dLng),
            (decimal)Math.Min(180, lng + dLng));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
