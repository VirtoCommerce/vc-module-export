using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ExportModule.Core;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Services;
using VirtoCommerce.ExportModule.CsvProvider;
using VirtoCommerce.ExportModule.Data.Security;
using VirtoCommerce.ExportModule.Data.Services;
using VirtoCommerce.ExportModule.JsonProvider;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.JsonConverters;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ExportModule.Web
{
    public class Module : IModule
    {
        public ManifestModuleInfo ModuleInfo { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<KnownExportTypesService>();
            serviceCollection.AddSingleton<IKnownExportTypesRegistrar>(serviceProvider => serviceProvider.GetRequiredService<KnownExportTypesService>());
            serviceCollection.AddSingleton<IKnownExportTypesResolver>(serviceProvider => serviceProvider.GetRequiredService<KnownExportTypesService>());

            serviceCollection.AddTransient<Func<ExportDataRequest, IExportProvider>>(serviceProvider => request => new JsonExportProvider(request));
            serviceCollection.AddTransient<Func<ExportDataRequest, IExportProvider>>(serviceProvider => request => new CsvExportProvider(request));
            serviceCollection.AddTransient<IExportProviderFactory, ExportProviderFactory>();

            serviceCollection.AddTransient<IDataExporter, DataExporter>();
            serviceCollection.AddTransient<IExportFileStorage, ExportFileStorage>();

            serviceCollection.Configure<MvcOptions>(configure =>
            {
                configure.Filters.Add(typeof(AnyPolicyAuthorizationFilter));
            });
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            //Register module permissions
            var permissionsProvider = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsProvider.RegisterPermissions(ModuleConstants.Security.Permissions.AllPermissions.Select(x => new Permission { GroupName = "Generic Export", Name = x }).ToArray());

            PolymorphJsonConverter.RegisterTypeForDiscriminator(typeof(ExportDataQuery), nameof(ExportDataQuery.ExportTypeName));

            AbstractTypeFactory<IExportProviderConfiguration>.RegisterType<JsonProviderConfiguration>();
            AbstractTypeFactory<IExportProviderConfiguration>.RegisterType<CsvProviderConfiguration>();

            PolymorphJsonConverter.RegisterTypeForDiscriminator(typeof(IExportProviderConfiguration), nameof(IExportProviderConfiguration.Type));
        }

        public void Uninstall()
        {
        }
    }
}
