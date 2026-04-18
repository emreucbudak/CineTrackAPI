using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Actors.Queries.Search;

public class SearchActorsQueryHandler : IRequestHandler<SearchActorsQuery, Result<List<SearchPersonDto>>>
{
    private readonly ITmdbService _tmdbService;

    public SearchActorsQueryHandler(ITmdbService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    public async Task<Result<List<SearchPersonDto>>> Handle(SearchActorsQuery request, CancellationToken cancellationToken)
    {
        return await _tmdbService.SearchPeopleAsync(request.Query, request.Page, cancellationToken);
    }
}
