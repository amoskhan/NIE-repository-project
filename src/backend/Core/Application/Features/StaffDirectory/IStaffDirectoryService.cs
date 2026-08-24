using Application.Contracts;

namespace Application.Features;

public interface IStaffDirectoryService
{
    Task<StaffDetailsDto?> GetStaffDetailsByEmailAsync(string email);
}

public sealed class StaffDirectoryUnavailableException : Exception
{
    public StaffDirectoryUnavailableException(string message) : base(message) { }
    public StaffDirectoryUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}
