namespace Locatic.Entities
{
    public class Marque
    {
        public int Id { get; set; }
        public string Nom { get; set; } = null!;
        public string? PaysOrigine { get; set; }

        public List<Modele> Modeles { get; set; } = new();
    }
}