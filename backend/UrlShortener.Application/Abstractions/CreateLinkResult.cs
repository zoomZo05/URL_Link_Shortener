using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Abstractions;

public abstract record CreateLinkResult;

public sealed record LinkCreated(ShortLink Link) : CreateLinkResult;

public sealed record InvalidLinkInput(string Error) : CreateLinkResult;

public sealed record CustomAliasAlreadyInUse(string Alias) : CreateLinkResult;

public sealed record ShortCodeGenerationUnavailable : CreateLinkResult;
