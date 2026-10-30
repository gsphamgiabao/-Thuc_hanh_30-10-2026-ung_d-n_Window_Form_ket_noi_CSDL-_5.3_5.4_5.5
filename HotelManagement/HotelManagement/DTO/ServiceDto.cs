namespace HotelManagement.DTO;

public sealed class ServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}

public sealed class ServiceUsageDto
{
    public int BookingId { get; set; }
    public int ServiceId { get; set; }
    public int Quantity { get; set; }
    public DateTime UsedAt { get; set; }
}
