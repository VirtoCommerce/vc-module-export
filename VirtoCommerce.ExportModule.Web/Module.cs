using System;
using System.Web.Http;
using Hangfire.Common;
using Microsoft.Practices.Unity;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Security;
using VirtoCommerce.ExportModule.Core.Services;
using VirtoCommerce.ExportModule.CsvProvider;
using VirtoCommerce.ExportModule.Data.Security;
using VirtoCommerce.ExportModule.Data.Services;
using VirtoCommerce.ExportModule.JsonProvider;
using VirtoCommerce.ExportModule.Web.JsonConverters;
using VirtoCommerce.Platform.Core.Modularity;

namespace VirtoCommerce.ExportModule.Web
{
    public class Module : ModuleBase
    {
        private readonly IUnityContainer _container;

        public Module(IUnityContainer container)
        {
            _container = container;
        }

        public override void Initialize()
        {
            base.Initialize();

            _container.RegisterInstance(new KnownExportTypesService());
            _container.RegisterInstance<IKnownExportTypesRegistrar>(_container.Resolve<KnownExportTypesService>());
            _container.RegisterInstance<IKnownExportTypesResolver>(_container.Resolve<KnownExportTypesService>());

            _container.RegisterType<Func<ExportDataRequest, IExportProvider>>(nameof(JsonExportProvider), new InjectionFactory(i =>
               new Func<ExportDataRequest, IExportProvider>(request => new JsonExportProvider(request))));

            _container.RegisterType<Func<ExportDataRequest, IExportProvider>>(nameof(CsvExportProvider), new InjectionFactory(i =>
                new Func<ExportDataRequest, IExportProvider>(request => new CsvExportProvider(request))));

            _container.RegisterType<IExportProviderFactory, ExportProviderFactory>();
            _container.RegisterInstance<IExportSecurityHandlerRegistrar>(new ExportSecurityHandlerRegistrar());
            _container.RegisterType<IDataExporter, DataExporter>();


            //Next lines allow to use polymorph types in API controller methods
            var httpConfiguration = _container.Resolve<HttpConfiguration>();
            httpConfiguration.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new PolymorphicExportDataQueryJsonConverter());

            // This line refreshes Hangfire JsonConverter with the current JsonSerializerSettings - PolymorphicExportDataQueryJsonConverter needs to be included
            JobHelper.SetSerializerSettings(httpConfiguration.Formatters.JsonFormatter.SerializerSettings);

        }

        public override void PostInitialize()
        {
            base.PostInitialize();

            // This method is called for each installed module on the second stage of initialization.
        }
    }
}
