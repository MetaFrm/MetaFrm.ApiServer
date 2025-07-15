using MetaFrm.Extensions;
using MetaFrm.Maui.Devices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace MetaFrm.ApiServer
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// AddMetaFrm
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMetaFrm(this IServiceCollection services)
        {
            services.AddSingleton<INotifyPropertyChanged, MetaFrm.ComponentModel.DummyNotifyPropertyChanged>();//Dummy Maui xaml

            services.AddFactory(new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true), DevicePlatform.Server);

            return services;
        }
    }
}