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
using System.Net.Http;
using Microsoft.Azure.Cosmos;

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
                
                var file = Path.Combine(AppContext.BaseDirectory, "KingfoodIO.xml");  // xml�ĵ�����·��
                var path = Path.Combine(AppContext.BaseDirectory, file); // xml�ĵ�����·��
                c.IncludeXmlComments(path, true); // true : ��ʾ��������ע��
                c.OrderActionsBy(o => o.RelativePath); // ��action�����ƽ�����������ж�����Ϳ��Կ���Ч���ˡ�

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
                x.RequireHttpsMetadata = true;// default is true Ϊmetadata����authority��֤����https
                x.SaveToken = true;// default is true, ��JWT���浽��ǰ��HttpContext, �����ڿ��Ի�ȡ��ͨ��await HttpContext.GetTokenAsync("Bearer","access_token"); ���������Ϊfalse, ��token������claim��, Ȼ���ȡͨ��User.FindFirst("access_token")?.value.
                x.TokenValidationParameters = new TokenValidationParameters
                {// ���ò���������֤���token
                    ValidateIssuer = true,
                    ValidIssuer = jwtTokenConfig.Issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtTokenConfig.SecretKey)),
                    ValidAudience = jwtTokenConfig.Audience,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)//��token����ʱ����֤������ʱ��
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



            CosmosClientOptions options = new CosmosClientOptions
            {
                ApplicationName = "hangfire",
                RequestTimeout = TimeSpan.FromSeconds(30),
                ConnectionMode= Microsoft.Azure.Cosmos.ConnectionMode.Direct,

            };

            services.AddHangfire(configuration => { configuration
                //.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                //.UseSimpleAssemblyNameTypeSerializer()
                //.UseRecommendedSerializerSettings()
                .UseAzureCosmosDbStorage("https://wiiyabatch.documents.azure.com:443/"
                    , "NgYw07nv13kY0RCVzRigi3O7HpPlhj2HE0iMfHAmzhuo7VxB4CzYQ7GRpzTNxF9qbxzoeSgErypI00bTN1D0mA=="
                    , "wiiyabatch", "hangfirev", options);
                configuration.UseColouredConsoleLogProvider(Hangfire.Logging.LogLevel.Trace);
                
                });


            //Add the processing server as IHostedService
            services.AddHangfireServer();

            services.Configure<AppSettingConfig>(Configuration.GetSection("AppSetting"));

            services.Configure<CacheSettingConfig>(Configuration.GetSection("CacheSetting"));

            services.Configure<DocumentDbConfig>(Configuration.GetSection("DocumentDb"));

            services.Configure<AzureStorageConfig>(Configuration.GetSection("AzureStorageConfig"));

            services.AddHttpClient();

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


            services.AddSingleton<ExchangeService>();
            //services.AddHostedService(p => p.GetRequiredService<ExchangeService>());

            services.AddTransient<IExchangeUtil, ExchangeUtil>();

            services.AddHostedService<ExchangeService>();


          

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