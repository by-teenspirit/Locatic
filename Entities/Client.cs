using System.ComponentModel.DataAnnotations;

namespace Locatic.Entities
{
    public class Client
    {
        public int Id { get; set; }

        [Required]
        public string Nom { get; set; } = null!;

        [Required]
        public string Prenom { get; set; } = null!;

        [EmailAddress]
        public string Email { get; set; } = null!;

        public string Telephone { get; set; } = null!;

        // Un client peut réserver plusieurs fois
        public List<Reservation> Reservations { get; set; } = new();
    }
}