using MetaFrm.Auth;
using MetaFrm.Database;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MetaFrm.ApiServer.Auth
{
    /// <summary>
    /// Authorize
    /// </summary>
    public class Authorize : TypeFilterAttribute, ICore
    {
        static bool IsFirst = true;
        static Authorize? Instance;
        static string? Type = AuthType.File;
        static readonly Lock lockObject = new();
        static readonly string path = Path.Combine(Factory.FolderPathDat, "AuthorizeTokenList.dat");
        internal static ConcurrentDictionary<string, AuthorizeToken> AuthorizeTokenList = [];

        /// <summary>
        /// Authorize class 생성자
        /// </summary>
        public Authorize() : base(typeof(AuthorizeFilter))
        {
            lock (lockObject)
                Instance ??= this;
        }

        private static void LoadToken()
        {
            Type = Instance == null ? AuthType.File : Instance.GetAttribute("Type");

            switch (Type)
            {
                case AuthType.DataBase:
                    LoadTokenDB();
                    break;
                case AuthType.File:
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
                bool failTryAdd = false;
                string lastFailToken = "";

                if (response.DataSet != null && response.DataSet.DataTables != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                    foreach (Data.DataRow dataRow in response.DataSet.DataTables[0].DataRows)
                    {
                        string TOKEN_STR = dataRow.String("TOKEN_STR") ?? throw new Exception("TOKEN_STR is null");

                        if (!AuthorizeTokenList.TryAdd(TOKEN_STR,
                            new AuthorizeToken(TOKEN_STR
                            , dataRow.Decimal("PROJECT_ID") ?? throw new Exception("PROJECT_ID is null")
                            , dataRow.Decimal("SERVICE_ID") ?? throw new Exception("SERVICE_ID is null")
                            , dataRow.String("TOKEN_TYPE") ?? throw new Exception("TOKEN_TYPE is null")
                            , dataRow.DateTime("EXPIRY_DATETIME") ?? throw new Exception("EXPIRY_DATETIME is null")
                            , dataRow.String("USER_KEY")
                            , dataRow.String("IP")))
                            && !failTryAdd)
                        {
                            failTryAdd = true;
                            lastFailToken = TOKEN_STR;
                        }
                    }

                if (failTryAdd && Factory.Logger.IsEnabled(LogLevel.Warning))
                    Factory.Logger.LogWarning("LoadTokenDB AuthorizeTokenList TryAdd Fail : {lastFailToken}", lastFailToken);
            }
            else
            {
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError("LoadTokenDB request fail : {Message}", response.Message);
            }
        }
        private static void LoadTokenFile()
        {
            try
            {
                AuthorizeTokenList = Factory.LoadInstance<ConcurrentDictionary<string, AuthorizeToken>>(path);
            }
            catch (Exception ex)
            {
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError(ex, "LoadTokenFile : {Message}", ex.Message);
                AuthorizeTokenList = [];
            }
        }

        internal static bool IsToken(string token, string tokenType)
        {

            if (IsFirst)
            {
                lock (lockObject)
                {
                    if (IsFirst)
                    {
                        IsFirst = false;
                        LoadToken();
                    }

                    if (!AuthorizeTokenList.TryGetValue(token, out AuthorizeToken? authorizeToken))
                    {
                        return Type switch
                        {
                            AuthType.DataBase => IsTokenDB(token, tokenType),
                            AuthType.File => false,
                            _ => false,
                        };
                    }

                    if (authorizeToken.IsExpired)
                        return false;

                    if (authorizeToken.TokenType != tokenType)
                        return false;
                }
            }
            else
            {
                if (!AuthorizeTokenList.TryGetValue(token, out AuthorizeToken? authorizeToken))
                {
                    return Type switch
                    {
                        AuthType.DataBase => IsTokenDB(token, tokenType),
                        AuthType.File => false,
                        _ => false,
                    };
                }

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

                        if (!AuthorizeTokenList.TryAdd(TOKEN_STR,
                            new AuthorizeToken(TOKEN_STR
                            , dataRow.Decimal("PROJECT_ID") ?? throw new Exception("PROJECT_ID is null")
                            , dataRow.Decimal("SERVICE_ID") ?? throw new Exception("SERVICE_ID is null")
                            , TOKEN_TYPE ?? throw new Exception("TOKEN_TYPE is null")
                            , dataRow.DateTime("EXPIRY_DATETIME") ?? throw new Exception("EXPIRY_DATETIME is null")
                            , dataRow.String("USER_KEY")
                            , dataRow.String("IP"))))
                        {
                            if (Factory.Logger.IsEnabled(LogLevel.Warning))
                                Factory.Logger.LogWarning("IsTokenDB AuthorizeTokenList TryAdd Fail : {TOKEN_STR}", TOKEN_STR);
                            return false;
                        }

                        return true;
                    }
            }
            else
            {
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError("IsTokenDB request fail : {Message}", response.Message);
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
                if (IsFirst)
                {
                    lock (lockObject)
                    {
                        IsFirst = false;
                        LoadToken();

                        if (!AuthorizeTokenList.TryAdd(authorizeToken.Token, authorizeToken) && Factory.Logger.IsEnabled(LogLevel.Error))
                            Factory.Logger.LogError("AddAuthorizeTokenList AuthorizeTokenList TryAdd Fail (IsFirst) : {Token}", authorizeToken.Token);
                    }
                }
                else
                {
                    if (!AuthorizeTokenList.TryAdd(authorizeToken.Token, authorizeToken) && Factory.Logger.IsEnabled(LogLevel.Warning))
                        Factory.Logger.LogWarning("AddAuthorizeTokenList AuthorizeTokenList TryAdd Fail : {Token}", authorizeToken.Token);
                }

                Task.Run(delegate
                {
                    switch (Type)
                    {
                        case AuthType.DataBase:
                            SaveTokenDB(authorizeToken);
                            break;
                        case AuthType.File:
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
            data["1"].AddParameter("EXPIRY_DATETIME", DbType.DateTime, 0, authorizeToken.ExpiryDate);
            data["1"].AddParameter("PROJECT_ID", DbType.Decimal, 18, authorizeToken.ProjectServiceBase.ProjectID);
            data["1"].AddParameter("SERVICE_ID", DbType.Decimal, 18, authorizeToken.ProjectServiceBase.ServiceID);
            data["1"].AddParameter("TOKEN_TYPE", DbType.NVarChar, 50, authorizeToken.TokenType);
            data["1"].AddParameter("USER_KEY", DbType.NVarChar, 100, authorizeToken.UserKey);
            data["1"].AddParameter("IP", DbType.NVarChar, 100, authorizeToken.IP);

            service = (IService)Factory.CreateInstance(data.ServiceName);
            response = service.Request(data);

            if (response.Status != Status.OK)
            {
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError("SaveTokenDB request fail : {Message}", response.Message);
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
                if (Factory.Logger.IsEnabled(LogLevel.Error))
                    Factory.Logger.LogError(ex, "SaveTokenFile : {Message}", ex.Message);
            }
        }
        private static ConcurrentDictionary<string, AuthorizeToken> DeleteToken(ConcurrentDictionary<string, AuthorizeToken> authorizeToken)
        {
            List<string> delete = [];

            foreach (var item in authorizeToken.Keys)
                if (authorizeToken[item].IsExpired)
                    delete.Add(item);

            string message;
            foreach (var item in delete)
            {
                if (!authorizeToken.TryRemove(item, out AuthorizeToken? authorize))
                {
                    if (authorize == null)
                        message = "authorize == null";
                    else
                        message = item;

                    if (Factory.Logger.IsEnabled(LogLevel.Warning))
                        Factory.Logger.LogWarning("DeleteToken AuthorizeTokenList TryRemove fail : {message}", message);
                }
            }

            return authorizeToken;
        }
    }
}