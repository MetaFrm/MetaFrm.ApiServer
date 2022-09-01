using Microsoft.AspNetCore.Mvc;

namespace MetaFrm.ApiServer.Auth
{
    /// <summary>
    /// Authorize
    /// </summary>
    public class Authorize : TypeFilterAttribute
    {
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
            if (AuthorizeTokenList == null || AuthorizeTokenList.Count == 0)
                LoadToken();

            AuthorizeTokenExpiredListDelete();

            if (AuthorizeTokenList != null && token.Token != null)
            {
                //var projectServiceBase = AuthorizeTokenList.Where(x => x.Value.ProjectServiceBase.ProjectID == projectID && x.Value.ProjectServiceBase.ServiceID == serviceID);

                //if (projectServiceBase != null && projectServiceBase.Any())
                //    projectServiceBase.FirstOrDefault().Value.ExpiryDateTime = DateTime.UtcNow;

                AuthorizeTokenList.Add(token.Token, token);
            }

            try
            {
                Factory.SaveInstance(AuthorizeTokenList, path);
            }
            catch (Exception)
            {
            }

            return token;
        }
        private static void AuthorizeTokenExpiredListDelete()
        {
            List<string> authorizeTokenListDelete = new();

            foreach (var item in AuthorizeTokenList.Keys)
                if (AuthorizeTokenList[item].IsExpired)
                    authorizeTokenListDelete.Add(item);

            foreach (var item in authorizeTokenListDelete)
                AuthorizeTokenList.Remove(item);
        }

        /// <summary>
        /// Authorize class 생성자
        /// </summary>
        public Authorize() : base(typeof(AuthorizeFilter)) { }
    }
}