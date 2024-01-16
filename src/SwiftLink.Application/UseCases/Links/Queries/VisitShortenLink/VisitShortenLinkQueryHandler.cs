﻿using MediatR;
using Microsoft.Extensions.Options;
using SwiftLink.Application.Common;
using SwiftLink.Application.Common.Interfaces;
using System.Text.Json;

namespace SwiftLink.Application.UseCases.Links.Queries.VisitShortCode;
public class VisitShortenLinkQueryHandler(IApplicationDbContext dbContext,
                                             ICacheProvider cacheProvider,
                                             IShortCodeGenerator codeGenerator,
                                             IOptions<AppSettings> options,
                                             ISharedContext sharedContext)
    : IRequestHandler<VisitShortenLinkQuery, Result<string>>
{

    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICacheProvider _cache = cacheProvider;
    private readonly IShortCodeGenerator _codeGenerator = codeGenerator;
    private readonly ISharedContext _sharedContext = sharedContext;
    private readonly AppSettings _options = options.Value;

    public async Task<Result<string>> Handle(VisitShortenLinkQuery request, CancellationToken cancellationToken)
    {
        Link link;
        var cacheResult = await _cache.Get(request.ShortCode);
        if (!string.IsNullOrEmpty(cacheResult))
            link = JsonSerializer.Deserialize<Link>(cacheResult);
        else
        {
            link = await _dbContext.Set<Link>()
                                   .FirstOrDefaultAsync(x => x.ShortCode == request.ShortCode, cancellationToken);
            await _cache.Set(request.ShortCode, JsonSerializer.Serialize(link));
        }

        if (link.IsBanned)
        {
            //return error for banned Url
        }

        if (link.ExpirationDate <= DateTime.Now)
        {
            //return error for expiration
        }

        if (link.Password is not null && request.Password is null)
        {
            //return error for password
        }

        if (!PasswordHasher.VerifyPassword(request.Password, link.Password))
        {
            //return error for incorrect password
        }

        return Result.Success(link.OriginalUrl);
    }
}