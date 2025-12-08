using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        optionsBuilder
            .UseSqlite(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public DbSet<ActionLog> ActionLogs { get; set; }
    public DbSet<Actuator> Actuators { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Gateway> Gateways { get; set; }
    public DbSet<Home> Homes { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Rule> Rules { get; set; }
    public DbSet<Sensor> Sensors { get; set; }
    public DbSet<SensorData> SensorData { get; set; }
    public DbSet<User> Users { get; set; }
}
