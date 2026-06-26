using Microsoft.EntityFrameworkCore;
using Locatic.Data;
using Locatic.Entities;
using Locatic.Interfaces;

namespace Locatic.Services
{
    public class VoitureService : IVoitureService
    {
        private readonly LocaticDbContext _context;

        public VoitureService(LocaticDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Voiture>> GetAllVoituresAsync()
        {
            return await _context.Voitures
                .Include(v => v.Modele)
                    .ThenInclude(m => m.Marque)
                .ToListAsync();
        }

        public async Task<Voiture?> GetVoitureByIdAsync(int id)
        {
            return await _context.Voitures
                .Include(v => v.Modele)
                    .ThenInclude(m => m.Marque)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task AddVoitureAsync(Voiture voiture)
        {
            _context.Voitures.Add(voiture);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVoitureAsync(Voiture voiture)
        {
            _context.Voitures.Update(voiture);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVoitureAsync(int id)
        {
            var voiture = await _context.Voitures.FindAsync(id);
            if (voiture != null)
            {
                _context.Voitures.Remove(voiture);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Modele>> GetAllModelesWithMarquesAsync()
        {
            return await _context.Modeles
                .Include(m => m.Marque)
                .OrderBy(m => m.Marque.Nom)
                .ThenBy(m => m.Nom)
                .ToListAsync();
        }
    }
}