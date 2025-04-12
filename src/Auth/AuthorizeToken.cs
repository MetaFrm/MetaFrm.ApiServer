using MetaFrm.Api.Models;

namespace MetaFrm.ApiServer.Auth
{
    /// <summary>
    /// AuthorizeToken
    /// </summary>
    public class AuthorizeToken : ICore
    {
        /// <summary>
        /// Token
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Create Utc DateTime
        /// </summary>
        public DateTime CreateDateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Expiry Utc DateTime
        /// </summary>
        public DateTime ExpiryDateTime { get; set; }

        private TimeSpan expiryTimeSpan;
        /// <summary>
        /// Expiry TimeSpan
        /// </summary>
        public TimeSpan ExpiryTimeSpan
        {
            get
            {
                return this.expiryTimeSpan;
            }
            set
            {
                this.expiryTimeSpan = value;
                this.ExpiryDateTime = CreateDateTime.AddTicks(this.expiryTimeSpan.Ticks);
            }
        }

        /// <summary>
        /// IsExpired
        /// </summary>
        public bool IsExpired
        {
            get
            {
                return this.ExpiryDateTime < DateTime.Now;
            }
        }

        /// <summary>
        /// GetToken
        /// </summary>
        public string? GetToken
        {
            get
            {
                return this.Token;
            }
        }

        /// <summary>
        /// User Key
        /// </summary>
        public string? UserKey { get; set; }

        /// <summary>
        /// IP
        /// </summary>
        public string? IP { get; set; }

        /// <summary>
        /// ProjectServiceBase
        /// </summary>
        public ProjectServiceBase ProjectServiceBase { get; set; }

        /// <summary>
        /// TokenType
        /// </summary>
        public string? TokenType { get; set; }

        /// <summary>
        /// AuthorizeToken
        /// </summary>
        public AuthorizeToken()
        {
            this.ExpiryTimeSpan = TimeSpan.FromDays(this.GetAttributeInt("ExpiryTimeSpanFromDays"));
            this.ProjectServiceBase = new();
            this.CreateToken();
        }
        /// <summary>
        /// AuthorizeToken
        /// </summary>
        /// <param name="tokenType"></param>
        /// <param name="userKey"></param>
        /// <param name="ip"></param>
        public AuthorizeToken(string tokenType, string? userKey, string? ip) : this()
        {
            this.TokenType = tokenType; 
            this.UserKey = userKey;
            this.IP = ip;
        }
        /// <summary>
        /// AuthorizeToken
        /// </summary>
        /// <param name="tokenType"></param>
        /// <param name="token"></param>
        /// <param name="userKey"></param>
        /// <param name="ip"></param>
        public AuthorizeToken(string tokenType, string token, string? userKey, string? ip)
        {
            this.TokenType = tokenType;
            this.ExpiryTimeSpan = TimeSpan.FromDays(this.GetAttributeInt("ExpiryTimeSpanFromDays"));
            this.ProjectServiceBase = new();
            this.Token = token;
            this.UserKey = userKey;
            this.IP = ip;
        }

        /// <summary>
        /// AuthorizeToken class 생성자
        /// </summary>
        /// <param name="projectID"></param>
        /// <param name="serviceID"></param>
        /// <param name="tokenType"></param>
        /// <param name="userKey"></param>
        /// <param name="ip"></param>
        public AuthorizeToken(decimal projectID, decimal serviceID, string tokenType, string? userKey, string? ip) : this(tokenType, userKey, ip)
        {
            this.ProjectServiceBase.ProjectID = projectID;
            this.ProjectServiceBase.ServiceID = serviceID;
        }
        /// <summary>
        /// AuthorizeToken class 생성자
        /// </summary>
        /// <param name="token"></param>
        /// <param name="projectID"></param>
        /// <param name="serviceID"></param>
        /// <param name="tokenType"></param>
        /// <param name="userKey"></param>
        /// <param name="ip"></param>
        public AuthorizeToken(string token, decimal projectID, decimal serviceID, string tokenType, string? userKey, string? ip) : this(tokenType, token, userKey, ip)
        {
            this.ProjectServiceBase.ProjectID = projectID;
            this.ProjectServiceBase.ServiceID = serviceID;
        }

        /// <summary>
        /// AuthorizeToken class 생성자
        /// </summary>
        /// <param name="projectID"></param>
        /// <param name="serviceID"></param>
        /// <param name="tokenType"></param>
        /// <param name="expiryTimeSpan"></param>
        /// <param name="userKey"></param>
        /// <param name="ip"></param>
        public AuthorizeToken(decimal projectID, decimal serviceID, string tokenType, TimeSpan expiryTimeSpan, string? userKey, string? ip) : this(projectID, serviceID, tokenType, userKey, ip)
        {
            this.ExpiryTimeSpan = expiryTimeSpan;
        }
        /// <summary>
        /// AuthorizeToken class 생성자
        /// </summary>
        /// <param name="token"></param>
        /// <param name="projectID"></param>
        /// <param name="serviceID"></param>
        /// <param name="tokenType"></param>
        /// <param name="expiryDateTime"></param>
        /// <param name="userKey"></param>
        /// <param name="ip"></param>
        public AuthorizeToken(string token, decimal projectID, decimal serviceID, string tokenType, DateTime expiryDateTime, string? userKey, string? ip) : this(token, projectID, serviceID, tokenType, userKey, ip)
        {
            this.ExpiryDateTime = expiryDateTime;
        }

        private void CreateToken()
        {
            if (this.Token.IsNullOrEmpty())
                this.Token = Guid.NewGuid().ToString().Replace("-", "");
        }   
    }
}