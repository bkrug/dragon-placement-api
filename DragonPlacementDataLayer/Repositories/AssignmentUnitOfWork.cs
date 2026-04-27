using System;
using DragonPlacementDataLayer.Models;

namespace DragonPlacementDataLayer.Repositories;

public interface IAssignmentUnitOfWork
{
    IGenericRepository<Dragon> DragonRepository { get; }
    IGenericRepository<Job> JobRepository { get; }
    IGenericRepository<Assignment> AssignmentRepository { get; }

    void Dispose();
    void Save();
}

public class AssignmentUnitOfWork(DragonPlacementContext context) : IDisposable, IAssignmentUnitOfWork
{
    private readonly DragonPlacementContext _context = context;
    public IGenericRepository<Dragon> DragonRepository { get; } = new GenericRepository<Dragon>(context);
    public IGenericRepository<Job> JobRepository { get; } = new GenericRepository<Job>(context);
    public IGenericRepository<Assignment> AssignmentRepository { get; } = new GenericRepository<Assignment>(context);

    public void Save()
    {
        _context.SaveChanges();
    }

    private bool disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
        this.disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}