using Locatic.Entities;

namespace Locatic.Interfaces
{
    public interface IReservationService
    {
        Task<IEnumerable<Reservation>> GetAllReservationsAsync();
        Task<(bool Success, string Message)> CreateReservationAsync(Reservation reservation);
    }
}