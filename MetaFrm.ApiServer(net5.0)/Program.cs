using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace MetaFrm
{
    /// <summary>
    /// Program
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Factory.Init("https://deploy.metafrm.net/", 1M, 2M, "aaabbbccc", Devices.Platform.Unknown);

            Api.Auth.Authorize.LoadToken();//저장된 토큰 불러오기

            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// CreateHostBuilder
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}