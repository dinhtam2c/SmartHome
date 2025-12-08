namespace Application.Interfaces;

public interface IUnitOfWork
{
    Task Begin();

    Task Commit();

    Task Rollback();
}
