using HotelManagement.DAL;
using HotelManagement.DTO;

namespace HotelManagement.BUS;

public sealed class HotelService
{
    private readonly HotelRepository repository;

    public HotelService(HotelRepository repository)
    {
        this.repository = repository;
    }

    public Task<IReadOnlyList<RoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default) => repository.GetRoomsAsync(cancellationToken);

    public Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default) => repository.GetServicesAsync(cancellationToken);

    public Task<IReadOnlyList<BookingDto>> GetActiveBookingsAsync(CancellationToken cancellationToken = default) => repository.GetActiveBookingsAsync(cancellationToken);

    public async Task<CustomerDto> GetOrCreateCustomerAsync(string fullName, string citizenId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Họ tên không được để trống.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(citizenId)) throw new ArgumentException("CCCD không được để trống.", nameof(citizenId));
        var existing = await repository.FindCustomerByCitizenIdAsync(citizenId.Trim(), cancellationToken);
        if (existing is not null) return existing;
        var customer = new CustomerDto { FullName = fullName.Trim(), CitizenId = citizenId.Trim(), PhoneNumber = phoneNumber.Trim() };
        customer.Id = await repository.AddCustomerAsync(customer, cancellationToken);
        return customer;
    }

    public Task<int> CheckInAsync(string roomNumber, CustomerDto customer, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomNumber) || customer.Id <= 0) throw new ArgumentException("Phòng và khách hàng hợp lệ là bắt buộc.");
        return repository.CheckInAsync(roomNumber, customer.Id, DateTime.Now, cancellationToken);
    }

    public Task AddServiceUsageAsync(int bookingId, int serviceId, int quantity, CancellationToken cancellationToken = default)
    {
        if (bookingId <= 0 || serviceId <= 0 || quantity <= 0) throw new ArgumentException("Phiếu, dịch vụ và số lượng phải hợp lệ.");
        return repository.AddServiceUsageAsync(new ServiceUsageDto { BookingId = bookingId, ServiceId = serviceId, Quantity = quantity, UsedAt = DateTime.Now }, cancellationToken);
    }

    public Task<CheckoutResultDto> CheckoutAsync(int bookingId, CancellationToken cancellationToken = default) => repository.CheckoutAsync(bookingId, DateTime.Now, cancellationToken);
}
