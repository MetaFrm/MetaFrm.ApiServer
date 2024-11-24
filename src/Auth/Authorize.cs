using MetaFrm.Database;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;

namespace MetaFrm.ApiServer.Auth
{
    /// <summary>
    /// Authorize
    /// </summary>
    public class Authorize : TypeFilterAttribute, ICore
    {
        static bool IsFirst = true;

        static Authorize? Instance;

        static string? Type = "FILE";

        static readonly object lockObject = new();

        static readonly string path = $"{Factory.FolderPathDat}AuthorizeTokenList.dat";

        internal static Dictionary<string, AuthorizeToken> AuthorizeTokenList = new();

        /// <summary>
        /// Authorize class 생성자
        /// </summary>
        public Authorize() : base(typeof(AuthorizeFilter))
        {
            lock (lockObject)
                if (Instance == null)
                    Instance = this;
        }

        private static void LoadToken()
        {
            Type = Instance == null ? "FILE" : Instance.GetAttribute("Type");

            switch (Type)
            {
                case "DB":
                    LoadTokenDB();
                    break;
                case "FILE":
                    LoadTokenFile();
                    break;
            }
        }
        private static void LoadTokenDB()
        {
            IService service;
            Response response;

            if (Instance == null) return;

            ServiceData data = new();
            data["1"].CommandText = Instance.GetAttribute("Select");
            data["1"].AddParameter("ACCESSKEY", DbType.NVarChar, 100, Factory.AccessKey);

            service = (IService)Factory.CreateInstance(data.ServiceName);
            response = service.Request(data);

            if (response.Status == Status.OK)
            {
                if (response.DataSet != null && response.DataSet.DataTables != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                    foreach (Data.DataRow dataRow in response.DataSet.DataTables[0].DataRows)
                    {
                        string TOKEN_STR = dataRow.String("TOKEN_STR") ?? throw new Exception("TOKEN_STR is null");

                        AuthorizeTokenList.Add(TOKEN_STR,
                            new AuthorizeToken(TOKEN_STR
                            , dataRow.Decimal("PROJECT_ID") ?? throw new Exception("PROJECT_ID is null")
                            , dataRow.Decimal("SERVICE_ID") ?? throw new Exception("SERVICE_ID is null")
                            , dataRow.String("TOKEN_TYPE") ?? throw new Exception("TOKEN_TYPE is null")
                            , dataRow.DateTime("EXPIRY_DATETIME") ?? throw new Exception("EXPIRY_DATETIME is null")
                            , dataRow.String("USER_KEY")
                            , dataRow.String("IP")));
                    }
            }
        }
        private static void LoadTokenFile()
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

        internal static bool IsToken(string token, string tokenType)
        {
            lock (lockObject)
            {
                if (IsFirst)
                {
                    IsFirst = false;
                    LoadToken();
                }

                AuthorizeTokenList.TryGetValue(token, out AuthorizeToken? authorizeToken);

                if (authorizeToken == null)
                    return Type switch
                    {
                        "DB" => IsTokenDB(token, tokenType),
                        "FILE" => false,
                        _ => false,
                    };

                if (authorizeToken.IsExpired)
                    return false;

                if (authorizeToken.TokenType != tokenType)
                    return false;
            }

            return true;
        }
        private static bool IsTokenDB(string token, string tokenType)
        {
            IService service;
            Response response;

            if (Instance == null) return false;

            ServiceData data = new();
            data["1"].CommandText = Instance.GetAttribute("SelectCheck");
            data["1"].AddParameter("TOKEN_STR", DbType.NVarChar, 100, token);

            service = (IService)Factory.CreateInstance(data.ServiceName);
            response = service.Request(data);

            if (response.Status == Status.OK)
            {
                if (response.DataSet != null && response.DataSet.DataTables != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                    foreach (Data.DataRow dataRow in response.DataSet.DataTables[0].DataRows)
                    {
                        string? TOKEN_TYPE = dataRow.String("TOKEN_TYPE");

                        if (TOKEN_TYPE != tokenType)
                            return false;

                        string TOKEN_STR = dataRow.String("TOKEN_STR") ?? throw new Exception("TOKEN_STR is null");

                        AuthorizeTokenList.Add(TOKEN_STR,
                            new AuthorizeToken(TOKEN_STR
                            , dataRow.Decimal("PROJECT_ID") ?? throw new Exception("PROJECT_ID is null")
                            , dataRow.Decimal("SERVICE_ID") ?? throw new Exception("SERVICE_ID is null")
                            , TOKEN_TYPE ?? throw new Exception("TOKEN_TYPE is null")
                            , dataRow.DateTime("EXPIRY_DATETIME") ?? throw new Exception("EXPIRY_DATETIME is null")
                            , dataRow.String("USER_KEY")
                            , dataRow.String("IP")));

                        return true;
                    }
            }

            return false;
        }

        /// <summary>
        /// CreateToken
        /// </summary>
        /// <param name="projectID"></param>
        /// <param name="serviceID"></param>
        /// <param name="tokenType"></param>
        /// <param name="userKey"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static AuthorizeToken CreateToken(decimal projectID, decimal serviceID, string tokenType, string? userKey, string? ip)
        {
            return AddAuthorizeTokenList(new AuthorizeToken(projectID, serviceID, tokenType, userKey, ip));
        }
        /// <summary>
        /// CreateToken
        /// </summary>
        /// <param name="projectID"></param>
        /// <param name="serviceID"></param>
        /// <param name="tokenType"></param>
        /// <param name="expiryTimeSpan"></param>
        /// <param name="userKey"></param>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static AuthorizeToken CreateToken(decimal projectID, decimal serviceID, string tokenType, TimeSpan expiryTimeSpan, string? userKey, string? ip)
        {
            return AddAuthorizeTokenList(new AuthorizeToken(projectID, serviceID, tokenType, expiryTimeSpan, userKey, ip));
        }
        private static AuthorizeToken AddAuthorizeTokenList(AuthorizeToken authorizeToken)
        {
            if (authorizeToken.Token != null)
            {
                //var projectServiceBase = AuthorizeTokenList.Where(x => x.Value.ProjectServiceBase.ProjectID == projectID && x.Value.ProjectServiceBase.ServiceID == serviceID);

                //if (projectServiceBase != null && projectServiceBase.Any())
                //    projectServiceBase.FirstOrDefault().Value.ExpiryDateTime = DateTime.UtcNow;
                lock (lockObject)
                    AuthorizeTokenList.Add(authorizeToken.Token, authorizeToken);

                Task.Run(delegate
                {
                    switch (Type)
                    {
                        case "DB":
                            SaveTokenDB(authorizeToken);
                            break;
                        case "FILE":
                            SaveTokenFile();
                            break;
                    }
                });
            }

            return authorizeToken;
        }

        private static void SaveTokenDB(AuthorizeToken authorizeToken)
        {
            IService service;
            Response response;

            if (Instance == null) return;

            ServiceData data = new();
            data["1"].CommandText = Instance.GetAttribute("Save");
            data["1"].AddParameter("TOKEN_STR", DbType.NVarChar, 100, authorizeToken.Token);
            data["1"].AddParameter("EXPIRY_DATETIME", DbType.DateTime, 0, authorizeToken.ExpiryDateTime);
            data["1"].AddParameter("PROJECT_ID", DbType.Decimal, 18, authorizeToken.ProjectServiceBase.ProjectID);
            data["1"].AddParameter("SERVICE_ID", DbType.Decimal, 18, authorizeToken.ProjectServiceBase.ServiceID);
            data["1"].AddParameter("TOKEN_TYPE", DbType.NVarChar, 50, authorizeToken.TokenType);
            data["1"].AddParameter("USER_KEY", DbType.NVarChar, 100, authorizeToken.UserKey);
            data["1"].AddParameter("IP", DbType.NVarChar, 100, authorizeToken.IP);

            service = (IService)Factory.CreateInstance(data.ServiceName);
            response = service.Request(data);

            if (response.Status != Status.OK)
            {
                Console.WriteLine(response.Message);
                SaveTokenFile();
            }
        }
        private static void SaveTokenFile()
        {
            try
            {
                lock (lockObject)
                    Factory.SaveInstance(DeleteToken(new(AuthorizeTokenList)), path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private static Dictionary<string, AuthorizeToken> DeleteToken(Dictionary<string, AuthorizeToken> authorizeToken)
        {
            List<string> delete = new();

            foreach (var item in authorizeToken.Keys)
                if (authorizeToken[item].IsExpired)
                    delete.Add(item);

            foreach (var item in delete)
                authorizeToken.Remove(item);

            return authorizeToken;
        }
    }
}