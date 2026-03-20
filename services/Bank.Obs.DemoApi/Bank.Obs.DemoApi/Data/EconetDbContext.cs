using Microsoft.EntityFrameworkCore;

namespace Bank.Obs.DemoApi.Data;

public class EconetDbContext : DbContext
{
    public EconetDbContext(DbContextOptions<EconetDbContext> options) : base(options)
    {
    }

    public DbSet<Transferencia> Transferencias { get; set; }
    public DbSet<TransferenciaInterbancaria> TransferenciaInterbancarias { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configuraciones adicionales si es necesario
    }
}
