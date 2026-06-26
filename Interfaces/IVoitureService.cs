using Locatic.Entities;

namespace Locatic.Interfaces
{
    public interface IVoitureService
    {
        Task<IEnumerable<Voiture>> GetAllVoituresAsync();
        Task<Voiture?> GetVoitureByIdAsync(int id);
        Task AddVoitureAsync(Voiture voiture);
        Task UpdateVoitureAsync(Voiture voiture);
        Task DeleteVoitureAsync(int id);
        Task<IEnumerable<Modele>> GetAllModelesWithMarquesAsync();
    }
}