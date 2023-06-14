using Microsoft.AspNetCore.Mvc;

namespace MetaFrm.ApiServer.Auth
{
    /// <summary>
    /// Authorize
    /// </summary>
    public class Authorize : TypeFilterAttribute
    {
        static object lockObject = new object();

        static readonly string path = $"{Factory.FolderPathDat}AuthorizeTokenList.dat";

        internal static Dictionary<string, AuthorizeToken> AuthorizeTokenList = new();

        /// <summary>
        /// LoadToken
        /// </summary>
        public static void LoadToken()
        {
            try
            {
                AuthorizeTokenList = Factory.LoadInstance<Dictionary<string, AuthorizeToken>>(path);
            }
            catch (Exception)
            {
                AuthorizeTokenList = new();
            }
        }
        /// <summary>
        /// CreateToken
        /// </summary>
        /// <param name="projectID"></param>
        /// <param name="serviceID"></param>
        /// <param name="userKey"></param>
        /// <returns></returns>
        public static AuthorizeToken CreateToken(decimal projectID, decimal serviceID, string? userKey)
        {
            return AddAuthorizeTokenList(new AuthorizeToken(projectID, serviceID, userKey), projectID, serviceID);
        }
        /// <summary>
        /// CreateToken
        /// </summary>
        /// <param name="projectID"></param>
        /// <param name="serviceID"></param>
        /// <param name="expiryTimeSpan"></param>
        /// <param name="userKey"></param>
        /// <returns></returns>
        public static AuthorizeToken CreateToken(decimal projectID, decimal serviceID, TimeSpan expiryTimeSpan, string? userKey)
        {
            return AddAuthorizeTokenList(new AuthorizeToken(projectID, serviceID, expiryTimeSpan, userKey), projectID, serviceID);
        }
        private static AuthorizeToken AddAuthorizeTokenList(AuthorizeToken token, decimal projectID, decimal serviceID)
        {
            if (AuthorizeTokenList != null && token.Token != null)
            {
                lock (lockObject)
                {
                    //var projectServiceBase = AuthorizeTokenList.Where(x => x.Value.ProjectServiceBase.ProjectID == projectID && x.Value.ProjectServiceBase.ServiceID == serviceID);

                    //if (projectServiceBase != null && projectServiceBase.Any())
                    //    projectServiceBase.FirstOrDefault().Value.ExpiryDateTime = DateTime.UtcNow;

                    AuthorizeTokenList.Add(token.Token, token);
                }
            }

            if (AuthorizeTokenList != null)
                lock (lockObject)
                {
                    try
                    {
                        Factory.SaveInstance(AuthorizeTokenExpiredListDelete(new(AuthorizeTokenList)), path);
                    }
                    catch (Exception)
                    {
                    }
                }

            return token;
        }
        private static Dictionary<string, AuthorizeToken> AuthorizeTokenExpiredListDelete(Dictionary<string, AuthorizeToken> authorizeToken)
        {
            List<string> delete = new();

            foreach (var item in authorizeToken.Keys)
                if (authorizeToken[item].IsExpired)
                    delete.Add(item);

            foreach (var item in delete)
                authorizeToken.Remove(item);

            return authorizeToken;
        }

        /// <summary>
        /// Authorize class 생성자
        /// </summary>
        public Authorize() : base(typeof(AuthorizeFilter)) { }
    }
}