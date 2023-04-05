using App.Domain.Common.Shop;
using App.Domain.Config;
using App.Infrastructure.Repository;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.Utility.Common;
using App.Infrastructure.Validation;
using FluentValidation;
using KingfoodIO.Application.Filter;
using KingfoodIO.Application.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NetCore.AutoRegisterDi;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hangfire;
using Microsoft.Azure.Documents.Client;
using LogManager = App.Infrastructure.Utility.Common.LogManager;

namespace KingfoodIO
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            NLog.LogManager.LoadConfiguration(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "nlog.config"));
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddCors();
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Wiiya", Version = "v1" });

                c.AddSecurityDefinition("WAuthToken", new OpenApiSecurityScheme()
                {
                    Name = "WAuthToken",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "WAuthToken"
                            },
                            Scheme = "oauth2",
                            Name = "WAuthToken",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
            });


            Hangfire.Azure.DocumentDbStorageOptions options = new Hangfire.Azure.DocumentDbStorageOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(30),
                ExpirationCheckInterval = TimeSpan.FromMinutes(2),
                CountersAggregateInterval = TimeSpan.FromMinutes(2),
                QueuePollInterval = TimeSpan.FromSeconds(15),
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp,
                EnablePartition = true, // default: false true; to enable partition on /type

            };


            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseAzureDocumentDbStorage("https://wiiyabatch.documents.azure.com:443/"
                    , "NgYw07nv13kY0RCVzRigi3O7HpPlhj2HE0iMfHAmzhuo7VxB4CzYQ7GRpzTNxF9qbxzoeSgErypI00bTN1D0mA=="
                    , "wiiyabatch", "hangfire", options)
                );

            // Add the processing server as IHostedService
            services.AddHangfireServer();


            services.Configure<AppSettingConfig>(Configuration.GetSection("AppSetting"));

            services.Configure<CacheSettingConfig>(Configuration.GetSection("CacheSetting"));

            services.Configure<DocumentDbConfig>(Configuration.GetSection("DocumentDb"));

            //DI resolver
            var assemblyToScan = Assembly.GetAssembly(typeof(BookingBatchServiceHandler));

            services.RegisterAssemblyPublicNonGenericClasses(assemblyToScan)
              .Where(c => c.Name.EndsWith("ServiceHandler"))
              .AsPublicImplementedInterfaces();

            services.RegisterAssemblyPublicNonGenericClasses(assemblyToScan)
                .Where(c => c.Name.EndsWith("Helper"))
                .AsPublicImplementedInterfaces();

            services.RegisterAssemblyPublicNonGenericClasses(assemblyToScan)
                .Where(c => c.Name.EndsWith("Util"))
                .AsPublicImplementedInterfaces();

            services.RegisterAssemblyPublicNonGenericClasses(assemblyToScan)
                .Where(c => c.Name.EndsWith("Builder"))
                .AsPublicImplementedInterfaces();

            services.AddScoped(typeof(IDbCommonRepository<>), typeof(DbCommonRepository<>));
            services.AddTransient<IValidator<DbShop>, ShopValidator>();
            services.AddSingleton<IRedisCache, RedisCache>();
            services.AddTransient<IAppVersionService, AppVersionService>();
            services.AddSingleton<ILogManager, LogManager>();

            services.AddScoped<AuthActionFilter>();
            services.AddScoped<AdminAuthFilter>();

            //Nlog Context

            var settings = Configuration.GetSection("AppSetting").Get<AppSettingConfig>();
            GlobalDiagnosticsContext.Set("server", settings.ShopUrl);
            GlobalDiagnosticsContext.Set("serverinstance", Guid.NewGuid().ToString());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
#if DEBUG
                //app.UseCors(builder =>
                //    builder.WithOrigins("http://127.0.0.1:2712").AllowAnyMethod()
                //        .AllowAnyHeader());
                app.UseCors(builder =>
                 builder.WithOrigins("*").AllowAnyMethod()
                     .AllowAnyHeader());
#else
app.UseCors(builder =>
                    builder.WithOrigins("https://groupmeals.z16.web.core.windows.net/").AllowAnyMethod()
                        .AllowAnyHeader());
#endif
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });

            app.UseSwagger(c =>
            {
                c.RouteTemplate = "/apis/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"v1/swagger.json", $" Wiiya api v{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}");
                c.RoutePrefix = $"apis";
            });

            app.UseHangfireDashboard();
        }
    }
}