using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Actors.Queries.GetDetail;

public class GetPersonDetailQueryHandler : IRequestHandler<GetPersonDetailQuery, Result<PersonDetailDto>>
{
    private readonly ITmdbService _tmdbService;

    public GetPersonDetailQueryHandler(ITmdbService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    public async Task<Result<PersonDetailDto>> Handle(GetPersonDetailQuery request, CancellationToken cancellationToken)
    {
        return await _tmdbService.GetPersonDetailAsync(request.PersonId, cancellationToken);
    }
}
