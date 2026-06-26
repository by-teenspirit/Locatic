using Locatic.Entities;

namespace Locatic.Interfaces
{
    public interface IClientService
    {
        Task<IEnumerable<Client>> GetAllClientsAsync();
        Task AddClientAsync(Client client);
    }
}