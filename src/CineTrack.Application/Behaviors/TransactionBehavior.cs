using CineTrack.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineTrack.Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ITransactionalCommand
{
    private readonly IAppDbContext _db;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IAppDbContext db, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        if (_db is not DbContext dbContext)
            return await next(cancellationToken);

        if (dbContext.Database.CurrentTransaction is not null)
            return await next(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Begin transaction for {RequestName}", requestName);

            var response = await next(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Committed transaction for {RequestName}", requestName);

            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning("Rolled back transaction for {RequestName}", requestName);
            throw;
        }
    }
}
