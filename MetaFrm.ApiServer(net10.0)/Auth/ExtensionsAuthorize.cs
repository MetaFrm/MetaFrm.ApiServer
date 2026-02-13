using MetaFrm.Api;
using Microsoft.AspNetCore.Http;

namespace MetaFrm.ApiServer.Auth
{
    internal static class ExtensionsAuthorize
    {
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