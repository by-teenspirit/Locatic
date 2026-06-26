using Microsoft.EntityFrameworkCore;
using Locatic.Data;
using Locatic.Entities;
using Locatic.Interfaces;

namespace Locatic.Services
{
    public class ReservationService : IReservationService
    {
        private readonly LocaticDbContext _context;

        public ReservationService(LocaticDbContext context)
        {
            _context = context;
        }

public async Task<IEnumerable<Reservation>> GetAllReservationsAsync()
{
    return await _context.Reservations
        .Include(r => r.Client)
        .Include(r => r.Voiture)
            .ThenInclude(v => v.Modele)
                .ThenInclude(m => m.Marque) 
        .OrderByDescending(r => r.DateDebut)
        .ToListAsync();
}

        public async Task<(bool Success, string Message)> CreateReservationAsync(Reservation reservation)
        {
            if (reservation.DateFin <= reservation.DateDebut)
            {
                return (false, "La date de fin doit être postérieure à la date de début.");
            }

            if (reservation.DateDebut.Date < DateTime.Now.Date)
            {
                return (false, "Impossible de planifier une réservation dans le passé.");
            }

            bool voitureOccupee = await _context.Reservations.AnyAsync(r =>
                r.VoitureId == reservation.VoitureId &&
                r.DateDebut < reservation.DateFin && 
                r.DateFin > reservation.DateDebut);

            if (voitureOccupee)
            {
                return (false, "Ce véhicule est déjà loué sur la période sélectionnée.");
            }

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return (true, "La réservation a bien été enregistrée.");
        }
    }
}