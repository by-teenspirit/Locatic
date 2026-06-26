using Microsoft.EntityFrameworkCore;
using Locatic.Entities;

namespace Locatic.Data
{
    public class LocaticDbContext : DbContext
    {
        public LocaticDbContext(DbContextOptions<LocaticDbContext> options) : base(options)
        {
        }

        public DbSet<Marque> Marques { get; set; } = null!;
        public DbSet<Modele> Modeles { get; set; } = null!;
        public DbSet<Voiture> Voitures { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Reservation> Reservations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Optionnel mais bien vu : on s'assure qu'on ne peut pas avoir deux fois la même immatriculation
            modelBuilder.Entity<Voiture>()
                .HasIndex(v => v.Immatriculation)
                .IsUnique();
        }
    }
}