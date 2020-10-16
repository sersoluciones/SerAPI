using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SerAPI.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SerAPI.Models;
using Amazon.S3;
using SerAPI.Managers;
using GraphQL;
using SerAPI.GraphQl.Generic;
using SerAPI.GraphQl;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi.Models;
using SerAPI.Utils.IdentityServerTransient;
using IdentityServer4.Validation;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using IdentityServer4.AccessTokenValidation;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using SerAPI.GraphQl.Custom;
using GraphQL.Server.Ui.Playground;
using Microsoft.Extensions.Options;
using SerAPI.Hubs;
using Swashbuckle.AspNetCore.SwaggerUI;
using SerAPI.Services;
using SerAPI.Utils;
using Microsoft.AspNetCore.ResponseCompression;

namespace SerAPI
{
    public class Startup
    {
        readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        public IWebHostEnvironment CurrentEnvironment { get; set; }
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("PsqlConnection"), o => o.UseNetTopologySuite())
                );

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // options.JsonSerializerOptions.WriteIndented = true;
                    // options.JsonSerializerOptions.PropertyNamingPolicy = null; // SnakeCaseNamingPolicy.Instance;
                });

            services.AddDefaultAWSOptions(Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonS3>();

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles();

            services.AddMemoryCache();

            services.AddScoped<AuditManager>();

            services.AddScoped<IRepository<ApplicationUser>, UsersManager>();
            services.AddScoped<IRepository<ApplicationRole>, RolesManager>();
            services.AddScoped<IRepository<IdentityRoleClaim<string>>, ClaimsManager>();
            services.AddScoped<IRepository<Permission>, GenericModelFactory<Permission>>();
            services.AddScoped<IRepository<CommonOption>, GenericModelFactory<CommonOption>>();
            services.AddScoped<IRepository<Attachment>, AttachmentManager>();

            services.AddScoped<IRepository<Car>, GenericModelFactory<Car>>();

            services.AddScoped<IViewRenderService, ViewRenderService>();
            services.AddScoped<Locales>();
            services.AddScoped<AuthMessageSender>();
            services.AddScoped<IRepository<Xlsx>, XlsxManager>();
            services.AddScoped<XlsxHelpers>();

            #region GraphQL
            services.AddConfigGraphQl();
            services.AddScopedModelsDynamic();

            #endregion

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<PostgresQLService>();

            #region Swagger

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1.0.0",
                    Title = "SerAPI API 2020",
                    Description = "API for SerAPI Server",
                    //TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Ser Soluciones SAS",
                        Email = "contacto@sersoluciones.com",
                        Url = new Uri("https://www.sersoluciones.com/")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Use under LICX",
                        Url = new Uri("https://example.com/license"),
                    }
                });

                options.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please enter into field the word 'Bearer' following by space and JWT",
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey
                    });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}
                    }
                });
            });

            #endregion

            #region SIGNAL R
            services.AddSignalR();
            #endregion

            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();


            #region Identity Server
            var builder = services.AddIdentityServer()
               .AddInMemoryApiResources(Config.Apis)
               .AddInMemoryClients(Config.Clients)
               .AddInMemoryIdentityResources(Config.Ids)
               .AddAspNetIdentity<ApplicationUser>();

            builder.AddExtensionGrantValidator<DelegationGrantValidator>();
            services.AddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator<ApplicationUser>>();
            services.AddTransient<IProfileService, ProfileService>();
            builder.AddDeveloperSigningCredential();

            var _loggerFactory = new LoggerFactory();
            services.AddSingleton<ICorsPolicyService>(new DefaultCorsPolicyService(_loggerFactory.CreateLogger<DefaultCorsPolicyService>())
            {
                AllowAll = true
            });

            #endregion

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
            })

            .AddIdentityServerAuthentication(options =>
            {
                options.Authority = Configuration.GetSection("IdentityServer")["Authority"];
                options.SupportedTokens = SupportedTokens.Jwt;
                options.RequireHttpsMetadata = false; // Note: Set to true in production
                options.ApiName = "ser_api"; // TODO: Fill
            });

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
            });

            services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Optimal);
            services.AddResponseCompression(options =>
            {
                options.MimeTypes = new[]
                {
                    "text/plain",
                    "text/css",
                    "application/javascript",
                    "text/html",
                    "application/xml",
                    "text/xml",
                    "application/json",
                    "text/json",
                    "image/svg+xml",
                    "image/png",
                    "image/jpg",
                    "application/font-woff",
                    "application/font-woff2"
                };
            });

            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                    x =>
                    {
                        //x.WithOrigins(
                        //    "http://127.0.0.1:5000",
                        //    "http://localhost:5000",
                        //    "https://localhost:5001",
                        //    "http://localhost:8080",
                        //    "http://192.168.0.18:5000",
                        //    "http://192.168.0.18:8080"
                        //);
                        x.AllowAnyMethod();
                        x.AllowAnyHeader();
                        //x.AllowCredentials();
                        x.AllowAnyOrigin();
                    });
            });

            #region Localization
            services.AddLocalization(options => options.ResourcesPath = "Resources");

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("es")
                };

                options.DefaultRequestCulture = new RequestCulture(culture: "es", uiCulture: "es");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
            });

            services.AddControllersWithViews()
                .AddDataAnnotationsLocalization()
                .AddViewLocalization(options => options.ResourcesPath = "Resources");

            //services.AddRazorPages();

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment() || env.IsStaging())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseIdentityServer();

            app.UseCors(MyAllowSpecificOrigins);
            app.UseResponseCompression();

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            #region GraphQL
            app.UseGraphQLWithAuth();
            app.UseGraphQLPlayground(options: new GraphQLPlaygroundOptions() { GraphQLEndPoint = "/api/graphql/v1" });
            #endregion

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                // Signal R
                endpoints.MapHub<StateHub>("/hub");
            });

            #region UseSwagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SerAPI API V1");
                c.DocExpansion(DocExpansion.None);
                c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
            });
            #endregion

            //DBContextSeedData.SeedRoles(app.ApplicationServices).GetAwaiter().GetResult();
            //DBContextSeedData.SeedPermissions(app.ApplicationServices).GetAwaiter().GetResult();
            //DBContextSeedData.SeedClaims(app.ApplicationServices).GetAwaiter().GetResult();
            //DBContextSeedData.CreateSuperUser(app.ApplicationServices).GetAwaiter().GetResult();

            Console.WriteLine($"Iniciando servidor SerAPI, DB en {Configuration["ConnectionStrings:PsqlConnection"]}");
        }

    }
}
