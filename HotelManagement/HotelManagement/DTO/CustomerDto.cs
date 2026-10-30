namespace HotelManagement.DTO;

public sealed class CustomerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
