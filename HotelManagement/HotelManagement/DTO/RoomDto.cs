namespace HotelManagement.DTO;

public sealed class RoomDto
{
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public string Status { get; set; } = string.Empty;
}
