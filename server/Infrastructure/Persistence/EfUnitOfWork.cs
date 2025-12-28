using Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork, IDisposable, IAsyncDisposable
{
    private readonly AppDbContext _context;

    private IDbContextTransaction? _transaction;

    public EfUnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task Begin()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task Commit()
    {
        try
        {
            await _context.SaveChangesAsync();
            if (_transaction is not null)
                await _transaction.CommitAsync();
        }
        catch
        {
            await Rollback();
            throw;
        }
    }

    public async Task Rollback()
    {
        if (_transaction is not null)
            await _transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();
    }
}
