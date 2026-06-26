using Microsoft.EntityFrameworkCore;
using Locatic.Data;
using Locatic.Entities;
using Locatic.Interfaces;

namespace Locatic.Services
{
    public class ClientService : IClientService
    {
        private readonly LocaticDbContext _context;

        public ClientService(LocaticDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync()
        {
            return await _context.Clients
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task AddClientAsync(Client client)
        {
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
        }
    }
}