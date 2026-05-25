using Application.Common.Data;
using Core.Domain.DeviceTelemetry;
using Core.Domain.Automations;
using Core.Domain.DeviceCommands;
using Core.Domain.Devices;
using Core.Domain.Floors;
using Core.Domain.Homes;
using Core.Domain.Scenes;
using Core.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppReadDbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;
    private readonly ILogger<AppDbContext>? _logger;

    private readonly IConfiguration _configuration;

    public AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration,
        IPublisher publisher, ILogger<AppDbContext>? logger = null) : base(options)
    {
        _configuration = configuration;
        _publisher = publisher;
        _logger = logger;
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

        try
        {
            foreach (var domainEvent in domainEvents)
            {
                try
                {
                    await _publisher.Publish(domainEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "Domain event {DomainEventType} failed after persistence commit.",
                        domainEvent.GetType().Name);
                }
            }
        }
        finally
        {
            foreach (var entity in entities)
            {
                entity.ClearDomainEvents();
            }
        }

        return result;
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction is not null)
        {
            await action(cancellationToken);
            return;
        }

        var strategy = Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public DbSet<DeviceCapabilityStateHistory> DeviceCapabilityStateHistories { get; set; }
    public DbSet<AutomationRule> AutomationRules { get; set; }
    public DbSet<AutomationCondition> AutomationConditions { get; set; }
    public DbSet<AutomationAction> AutomationActions { get; set; }
    public DbSet<AutomationExecution> AutomationExecutions { get; set; }
    public DbSet<AutomationExecutionAction> AutomationExecutionActions { get; set; }
    public DbSet<DeviceCommandExecution> DeviceCommandExecutions { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Floor> Floors { get; set; }
    public DbSet<Home> Homes { get; set; }
    public DbSet<Scene> Scenes { get; set; }
    public DbSet<SceneAction> SceneActions { get; set; }
    public DbSet<SceneExecution> SceneExecutions { get; set; }
    public DbSet<SceneExecutionAction> SceneExecutionActions { get; set; }

    IQueryable<Device> IAppReadDbContext.Devices => Devices;
    IQueryable<AutomationRule> IAppReadDbContext.AutomationRules => AutomationRules;
    IQueryable<AutomationExecution> IAppReadDbContext.AutomationExecutions => AutomationExecutions;
    IQueryable<AutomationExecutionAction> IAppReadDbContext.AutomationExecutionActions => AutomationExecutionActions;
    IQueryable<Floor> IAppReadDbContext.Floors => Floors;
    IQueryable<Home> IAppReadDbContext.Homes => Homes;
    IQueryable<Scene> IAppReadDbContext.Scenes => Scenes;
    IQueryable<SceneExecution> IAppReadDbContext.SceneExecutions => SceneExecutions;
    IQueryable<SceneExecutionAction> IAppReadDbContext.SceneExecutionActions => SceneExecutionActions;
    IQueryable<DeviceCommandExecution> IAppReadDbContext.DeviceCommandExecutions => DeviceCommandExecutions;
    IQueryable<DeviceCapabilityStateHistory> IAppReadDbContext.DeviceCapabilityStateHistories =>
        DeviceCapabilityStateHistories;
}
