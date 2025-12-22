using MetaFrm.Extensions;
using MetaFrm.Maui.Devices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace MetaFrm.ApiServer
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// RunMetaFrm
        /// </summary>
        /// <param name="builder"></param>
        public static void RunMetaFrm(this WebApplicationBuilder builder)
        {
            builder.Services.AddMetaFrm();

            var app = builder.Build();

            app.UseMetaFrm();

            app.Run();
        }
        /// <summary>
        /// AddMetaFrm
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMetaFrm(this IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition =
                       System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

            services.AddOpenApi();

            services.AddFactory(new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true), DevicePlatform.Server);

            return services;
        }

        /// <summary>
        /// UseMetaFrm
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static WebApplication UseMetaFrm(this WebApplication builder)
        {
            builder.MapOpenApi();

            //if (app.Environment.IsDevelopment())
            //{
            //    app.MapScalarApiReference();
            //}
            builder.MapScalarApiReference();

            builder.UseHttpsRedirection();

            builder.UseAuthorization();

            builder.MapControllers();

            return builder;
        }
    }
}