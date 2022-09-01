using MetaFrm.Api.Models;

namespace MetaFrm.ApiServer.Auth
{
    /// <summary>
    /// AuthorizeToken
    /// </summary>
    public class AuthorizeToken
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
        /// <summary>
        /// Expiry TimeSpan
        /// </summary>
        public TimeSpan ExpiryTimeSpan { get; set; } = TimeSpan.FromDays(7);
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
                this.CreateToken();

                return this.Token;
            }
        }
        /// <summary>
        /// User Key
        /// </summary>
        public string? UserKey { get; set; }

        /// <summary>
        /// ProjectServiceBase
        /// </summary>
        public ProjectServiceBase ProjectServiceBase { get; set; }

        /// <summary>
        /// AuthorizeToken
        /// </summary>
        public AuthorizeToken() { this.ProjectServiceBase = new(); }

        /// <summary>
        /// AuthorizeToken class 생성자
        /// </summary>
        /// <param name="projectID"></param>
        /// <param name="serviceID"></param>
        /// <param name="userKey"></param>
        public AuthorizeToken(decimal projectID, decimal serviceID, string? userKey)
        {
            this.ProjectServiceBase = new() { ProjectID = projectID, ServiceID = serviceID };
            this.UserKey = userKey;
            this.CreateToken();
        }

        /// <summary>
        /// AuthorizeToken class 생성자
        /// </summary>
        /// <param name="projectID"></param>
        /// <param name="serviceID"></param>
        /// <param name="expiryTimeSpan"></param>
        /// <param name="userKey"></param>
        public AuthorizeToken(decimal projectID, decimal serviceID, TimeSpan expiryTimeSpan, string? userKey)
        {
            this.ProjectServiceBase = new() { ProjectID = projectID, ServiceID = serviceID };
            this.ExpiryTimeSpan = expiryTimeSpan;
            this.UserKey = userKey;
            this.CreateToken();
        }

        /// <summary>
        /// CreateToken
        /// </summary>
        private void CreateToken()
        {
            if (this.Token.IsNullOrEmpty())
            {
                this.Token = Guid.NewGuid().ToString().Replace("-", "");
                this.ExpiryDateTime = CreateDateTime.AddTicks(ExpiryTimeSpan.Ticks);
            }
        }
    }
}