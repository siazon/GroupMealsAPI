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
using Stripe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using KingfoodIO.Common;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using App.Domain.Common.Customer;
using App.Domain.Common.Auth;

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
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddDistributedMemoryCache();
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

            var jwtTokenConfig = Configuration.GetSection("Jwt").Get<JwtTokenConfig>();
            services.AddSingleton(jwtTokenConfig);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = true;// default is true 为metadata或者authority验证请求https
                x.SaveToken = true;// default is true, 将JWT保存到当前的HttpContext, 以至于可以获取它通过await HttpContext.GetTokenAsync("Bearer","access_token"); 如果想设置为false, 将token保存在claim中, 然后获取通过User.FindFirst("access_token")?.value.
                x.TokenValidationParameters = new TokenValidationParameters
                {// 设置参数用于验证身份token
                    ValidateIssuer = true,
                    ValidIssuer = jwtTokenConfig.Issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtTokenConfig.SecretKey)),
                    ValidAudience = jwtTokenConfig.Audience,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)//对token过期时间验证的允许时间
                };
                x.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["Wauthtoken"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/Chat")))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddSignalR();
            services.AddCors(options =>
            {
                options.AddPolicy(name: "allowCors",
                                  policy =>
                                  {
                                      policy.WithOrigins("http://127.0.0.1:2712/profile");
                                      policy.AllowAnyHeader();
                                      policy.WithMethods("GET", "POST");
                                      policy.AllowCredentials();
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

            services.Configure<AzureStorageConfig>(Configuration.GetSection("AzureStorageConfig"));

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
            var settings = Configuration.GetSection("AppSetting").Get<AppSettingConfig>();
            StripeConfiguration.ApiKey = settings.StripeKey;//"sk_test_51MsNeuEOhoHb4C89kuTDIQd4WTiRiWGXSrFMnJMxsk0ufrGw7VMTsilTZKmVYbYn9zHyW98De7hXcrOwfrbGJXcY00DE8tswlW";
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseCors(builder => builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());

            }
            else
            {
                app.UseCors(builder => builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());
                //app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.UseCors(builder => builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader());
            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseCors("allowCors");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapHub<ClockHub>("/Chat").RequireCors(t => t.SetIsOriginAllowed((host) => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
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