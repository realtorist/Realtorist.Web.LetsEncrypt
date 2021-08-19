using ExtCore.Infrastructure.Actions;
using LettuceEncrypt;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Realtorist.Models.Helpers;
using Realtorist.Models.Settings;
using Realtorist.Services.Abstractions.Providers;
using System;
using System.IO;

namespace Realtorist.Web.LetsEncrypt
{
    public class Startup : IConfigureServicesAction
    {
        public int Priority => 5;

        void IConfigureServicesAction.Execute(IServiceCollection services, IServiceProvider serviceProvider)
        {
            var env = serviceProvider.GetService<IWebHostEnvironment>();
            var configuration = serviceProvider.GetService<IConfiguration>();
            var settingsProvider = serviceProvider.GetService<ISettingsProvider>();
            var logger = serviceProvider.GetService<ILogger<Startup>>();
            
            var settings = settingsProvider.GetSettingAsync<WebsiteSettings>(SettingTypes.Website).Result;
            var profile = settingsProvider.GetSettingAsync<ProfileSettings>(SettingTypes.Profile).Result;
            services.AddLettuceEncrypt(option =>
            {
                if (settings?.WebsiteAddress.IsNullOrEmpty() ?? true) return;

                option.AcceptTermsOfService = true;
                if (env.IsDevelopment())
                {
                    var domainName = configuration["LetsEncrypt:DomainName"];
                    if (domainName.IsNullOrEmpty()) 
                    {
                        logger.LogWarning("Domain name for lets encrypt is not set");
                        return;
                    }

                    option.DomainNames = new[] { domainName };
                    option.UseStagingServer = true;
                }
                else
                {
                    option.DomainNames = new[] { settings.WebsiteAddress, $"www.{settings.WebsiteAddress}" };
                }

                option.EmailAddress = profile.Email;
            })
            .PersistDataToDirectory(new DirectoryInfo("certs"), "password");
        }
    }
}
