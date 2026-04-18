using Application.Common.Data;
using Core.Domain.Data;
using Core.Domain.DeviceCommandExecutions;
using Core.Domain.Devices;
using Core.Domain.Homes;
using Core.Domain.Scenes;
using Core.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppReadDbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;

    private readonly IConfiguration _configuration;

    public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration,
        IPublisher publisher) : base(options)
    {
        _configuration = configuration;
        _publisher = publisher;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        optionsBuilder
            .UseSqlite(connectionString);

        optionsBuilder.ConfigureWarnings(w =>
            w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(Entity.DomainEvents));
            }
        }
    }

    // TODO: outbox pattern
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entities = ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        return result;
    }

    public DbSet<DeviceCapabilityStateHistory> DeviceCapabilityStateHistories { get; set; }
    public DbSet<DeviceCommandExecution> DeviceCommandExecutions { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Home> Homes { get; set; }
    public DbSet<Scene> Scenes { get; set; }
    public DbSet<SceneTarget> SceneTargets { get; set; }
    public DbSet<SceneSideEffect> SceneSideEffects { get; set; }
    public DbSet<SceneExecution> SceneExecutions { get; set; }
    public DbSet<SceneExecutionTarget> SceneExecutionTargets { get; set; }
    public DbSet<SceneExecutionSideEffect> SceneExecutionSideEffects { get; set; }

    IQueryable<Device> IAppReadDbContext.Devices => Devices;
    IQueryable<Home> IAppReadDbContext.Homes => Homes;
    IQueryable<Scene> IAppReadDbContext.Scenes => Scenes;
    IQueryable<SceneExecution> IAppReadDbContext.SceneExecutions => SceneExecutions;
    IQueryable<SceneExecutionTarget> IAppReadDbContext.SceneExecutionTargets => SceneExecutionTargets;
    IQueryable<SceneSideEffect> IAppReadDbContext.SceneSideEffects => SceneSideEffects;
    IQueryable<SceneExecutionSideEffect> IAppReadDbContext.SceneExecutionSideEffects => SceneExecutionSideEffects;
    IQueryable<DeviceCommandExecution> IAppReadDbContext.DeviceCommandExecutions => DeviceCommandExecutions;
    IQueryable<DeviceCapabilityStateHistory> IAppReadDbContext.DeviceCapabilityStateHistories =>
        DeviceCapabilityStateHistories;
}
