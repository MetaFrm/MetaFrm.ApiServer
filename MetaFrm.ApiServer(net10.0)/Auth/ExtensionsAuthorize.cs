using MetaFrm.Api;
using Microsoft.AspNetCore.Http;

namespace MetaFrm.ApiServer.Auth
{
    internal static class ExtensionsAuthorize
    {
        internal static AuthorizeToken? GetAuthorizeToken(this HttpRequest httpRequest, string? token)
        {
            string? authHeader = httpRequest.Headers.Authorization;

            if (authHeader != null && authHeader.StartsWith($"{Headers.Bearer} "))
                token = authHeader.Split(' ')[1];

            if (token == null || !Authorize.AuthorizeTokenList.TryGetValue(token, out AuthorizeToken? authorizeToken) || authorizeToken == null)
                return null;

            return authorizeToken;
        }
        internal static AuthorizeToken? GetAuthorizeToken(this HttpRequest httpRequest)
        {
            string? authHeader = httpRequest.Headers.Authorization;

            if (authHeader != null && authHeader.StartsWith($"{Headers.Bearer} "))
            {
                if (Authorize.AuthorizeTokenList.TryGetValue(authHeader.Split(' ')[1], out AuthorizeToken? authorizeToken))
                    return authorizeToken;
            }

            return null;
        }
    }
}