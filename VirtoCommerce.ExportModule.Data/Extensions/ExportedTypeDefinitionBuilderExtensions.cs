using System;
using VirtoCommerce.ExportModule.Core.Model;
using VirtoCommerce.ExportModule.Core.Security;
using VirtoCommerce.ExportModule.Data.Services;

namespace VirtoCommerce.ExportModule.Data.Extensions
{
    public static class ExportedTypeDefinitionBuilderExtensions
    {
        public static ExportedTypeDefinitionBuilder WithDataSourceFactory(this ExportedTypeDefinitionBuilder builder, Func<ExportDataQuery, IPagedDataSource> factory)
        {
            builder.ExportedTypeDefinition.ExportedDataSourceFactory = factory;
            return builder;
        }

        public static ExportedTypeDefinitionBuilder WithMetadata(this ExportedTypeDefinitionBuilder builder, ExportedTypeMetadata metadata)
        {
            builder.ExportedTypeDefinition.MetaData = metadata;
            return builder;
        }

        public static ExportedTypeDefinitionBuilder WithTabularMetadata(this ExportedTypeDefinitionBuilder builder, ExportedTypeMetadata tabularMetadata)
        {
            builder.ExportedTypeDefinition.TabularMetaData = tabularMetadata;
            return builder;
        }

        public static ExportedTypeDefinitionBuilder WithTypeName(this ExportedTypeDefinitionBuilder builder, string typeName)
        {
            builder.ExportedTypeDefinition.TypeName = typeName;
            return builder;
        }

        public static ExportedTypeDefinitionBuilder WithGroup(this ExportedTypeDefinitionBuilder builder, string group)
        {
            builder.ExportedTypeDefinition.Group = group;
            return builder;
        }

        public static ExportedTypeDefinitionBuilder WithExportDataQueryType(this ExportedTypeDefinitionBuilder builder, string exportDataQueryType)
        {
            builder.ExportedTypeDefinition.ExportDataQueryType = exportDataQueryType;
            return builder;
        }

        public static ExportedTypeDefinitionBuilder WithPermissionAuthorization(this ExportedTypeDefinitionBuilder builder, params string[] permissions)
        {
            builder.ExportedTypeDefinition.RequiredPermissions = permissions;
            return builder;
        }

        public static ExportedTypeDefinitionBuilder WithAuthorizationHandler(this ExportedTypeDefinitionBuilder builder, IExportSecurityHandler exportSecurityHandler)
        {
            builder.ExportedTypeDefinition.SecurityHandler = exportSecurityHandler;
            return builder;
        }

    }
}
