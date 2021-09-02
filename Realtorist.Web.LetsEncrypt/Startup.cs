using ExtCore.Infrastructure.Actions;
using LettuceEncrypt;
using Microsoft.AspNetCore.Builder;
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
    public class Startup : IConfigureServicesAction, IConfigureAction
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

            var domainName = settings?.WebsiteAddress;
            if (env.IsDevelopment() && !configuration["LetsEncrypt:DomainName"].IsNullOrEmpty())
            {
                domainName = configuration["LetsEncrypt:DomainName"];
            }

            if (domainName.IsNullOrEmpty())
            {
                logger.LogWarning($"Won't use LetsEncrypt. Domain name wasn't set neither through website address in setting nor through configuration (LestEncrypt->DomainName property)");
                return;
            }
            
            services.AddLettuceEncrypt(option =>
            {
                option.EmailAddress = profile.Email;
                option.DomainNames = env.IsDevelopment() ? new[] { domainName } : new[] { domainName, $"www.{domainName}" };
                option.AcceptTermsOfService = true;

                if (env.IsDevelopment())
                {
                    option.UseStagingServer = true;
                }
            })
            .PersistDataToDirectory(new DirectoryInfo("certs"), "password");
        }

        void IConfigureAction.Execute(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            var env = serviceProvider.GetService<IWebHostEnvironment>();
            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
        }
    }
}
