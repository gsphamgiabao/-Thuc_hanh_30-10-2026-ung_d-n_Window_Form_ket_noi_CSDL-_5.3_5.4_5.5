namespace HotelManagement.DTO;

public sealed class BookingDto
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;
    public DateTime CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }
    public decimal PaymentTotal { get; set; }
}

public sealed class CheckoutResultDto
{
    public int BookingId { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public TimeSpan StayDuration { get; init; }
    public decimal RoomTotal { get; init; }
    public decimal ServiceTotal { get; init; }
    public decimal GrandTotal => RoomTotal + ServiceTotal;
}
