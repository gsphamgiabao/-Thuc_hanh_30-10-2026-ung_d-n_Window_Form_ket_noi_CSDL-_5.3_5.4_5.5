namespace HotelManagement.BUS;

public static class BillingCalculator
{
    public static decimal CalculateRoomTotal(DateTime checkIn, DateTime checkOut, decimal hourlyRate)
    {
        if (checkOut <= checkIn) throw new ArgumentException("Thời điểm trả phòng phải sau thời điểm nhận phòng.");
        if (hourlyRate < 0) throw new ArgumentOutOfRangeException(nameof(hourlyRate));
        var billedHours = Math.Max(1, (int)Math.Ceiling((checkOut - checkIn).TotalHours));
        return billedHours * hourlyRate;
    }

    public static decimal CalculateTotal(DateTime checkIn, DateTime checkOut, decimal hourlyRate, decimal serviceTotal)
    {
        if (serviceTotal < 0) throw new ArgumentOutOfRangeException(nameof(serviceTotal));
        return CalculateRoomTotal(checkIn, checkOut, hourlyRate) + serviceTotal;
    }
}
