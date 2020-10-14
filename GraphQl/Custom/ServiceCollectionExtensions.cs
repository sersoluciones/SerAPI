using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Server;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SerAPI.GraphQl.Generic;
using SerAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SerAPI.GraphQl.Custom
{
    public static class ServiceCollectionExtensions
    {
        public static void AddConfigGraphQl(this IServiceCollection services)
        {
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddSingleton<ITableNameLookup, TableNameLookup>();
            services.AddSingleton<TableMetadata>();
            services.AddSingleton<IDatabaseMetadata, DatabaseMetadata>();

            services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            services.AddSingleton<DataLoaderDocumentListener>();

            services.AddScoped<GraphQLQuery>();
            services.AddScoped<FillDataExtensions>();
            services.AddScoped<ISchema, AppSchema>();

            //permissions
            services.AddTransient<IValidationRule, RequiresAuthValidationRule>();
            //services.AddTransient<IValidationRule, AuthorizationValidationRule>();

            services.AddHttpContextAccessor();

            services
                .AddGraphQL(o =>
                {
                    o.ExposeExceptions = false; // CurrentEnvironment.IsDevelopment();
                    o.EnableMetrics = false; // CurrentEnvironment.IsDevelopment();
                    o.UnhandledExceptionDelegate = ctx => Console.WriteLine("error: " + ctx.OriginalException.Message);
                })
                .AddGraphTypes(ServiceLifetime.Scoped)
                .AddSystemTextJson(deserializerSettings => { }, serializerSettings => { })
                .AddGraphQLAuthorization(options =>
                {
                    options.AddPolicy("Authorized", x => x.RequireAuthenticatedUser());
                })
                .AddUserContextBuilder(httpContext => new GraphQLUserContext { User = httpContext.User })
                .AddDataLoader();
        }

        public static void AddScopedModelsDynamic(this IServiceCollection services)
        {
            services.AddScoped<IGraphRepository<ApplicationRole>, GenericGraphRepository<ApplicationRole>>();
            services.AddScoped<IGraphRepository<ApplicationUser>, GenericGraphRepository<ApplicationUser>>();
            services.AddScoped<IGraphRepository<ApplicationUserRole>, GenericGraphRepository<ApplicationUserRole>>();

            var assembly = Assembly.GetCallingAssembly();
            foreach (var type in assembly.GetTypes()
               .Where(x => !x.IsAbstract && typeof(BasicModel).IsAssignableFrom(x)))
            {
                var interfaceType = typeof(IGraphRepository<>).MakeGenericType(new Type[] { type });
                var inherateType = typeof(GenericGraphRepository<>).MakeGenericType(new Type[] { type });
                ServiceLifetime serviceLifetime = ServiceLifetime.Scoped;
                //Console.WriteLine($"Dependencia IGraphRepository registrada type {type.Name}");
                services.TryAdd(new ServiceDescriptor(interfaceType, inherateType, serviceLifetime));
            }
        }
    }
}
