using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc.Versioning.Conventions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyCodeCamp.Controllers;
using MyCodeCamp.Data;
using MyCodeCamp.Data.Entities;
using MyCodeCamp.Models;
using Newtonsoft.Json;
using System.Text;

namespace MyCodeCamp
{
    public class Startup
    {
        private readonly IHostingEnvironment _env;
        IConfigurationRoot _config;
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            _env = env;
            _config = builder.Build();
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_config);
            services.AddDbContext<CampContext>(ServiceLifetime.Scoped);
            services.AddScoped<ICampRepository, CampRepository>();
            services.AddTransient<CampDbInitializer>();
            services.AddTransient<CampIdentityInitializer>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddAutoMapper();

            services.AddMemoryCache();

            services.AddIdentity<CampUser, IdentityRole>()
                    .AddEntityFrameworkStores<CampContext>()
                    .AddDefaultTokenProviders();
            services.AddAuthentication()
                 .AddCookie()
                 .AddJwtBearer(cfg =>
                 {
                     cfg.TokenValidationParameters = new TokenValidationParameters()
                     {
                         ValidIssuer = _config["Tokens:Issuer"],
                         ValidAudience = _config["Tokens:Audience"],
                         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:Key"]))
                     };
                 });
            services.AddApiVersioning(cfg =>
           {
               cfg.DefaultApiVersion = new ApiVersion(1, 1);
               cfg.AssumeDefaultVersionWhenUnspecified = true;
               cfg.ReportApiVersions = true;
               cfg.ApiVersionReader = new HeaderApiVersionReader("ver", "X-MyCodeCamp-Version");
               cfg.Conventions.Controller<TalksController>()
                   .HasApiVersion(new ApiVersion(1, 0))
                   .HasApiVersion(new ApiVersion(1, 1))
                   .HasApiVersion(new ApiVersion(2, 0))
                   .Action(m => m.Post(default(string), default(int), default(TalkModel)))
                   .MapToApiVersion(new ApiVersion(2, 0));

           });
            //services.ConfigureApplicationCookie(config =>
            //{
            //    config.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
            //    config.Events.OnRedirectToLogin = (ctx) =>
            //        {
            //            if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
            //            {
            //                ctx.Response.StatusCode = 401;
            //            }
            //            return Task.CompletedTask;
            //        };
            //    config.Events.OnRedirectToAccessDenied = (ctx) =>
            //       {
            //           if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200)
            //           {
            //               ctx.Response.StatusCode = 403;
            //           }
            //           return Task.CompletedTask;
            //       };
            //});

            services.AddCors(cfg =>
            {
                cfg.AddPolicy("Fluer", bldr =>
                {
                    bldr.AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins("http://vioricalfluer.nl");
                });
                cfg.AddPolicy("AnyGet", bldr =>
                {
                    // anyone can read the API but not Post to the api
                    bldr.AllowAnyHeader()
                        .WithMethods("GET")
                        .AllowAnyOrigin();
                });
            });
            services.AddAuthorization(cfg =>
            {
                cfg.AddPolicy("SuperUser", p => p.RequireClaim("SuperUser", "True"));
            });
            // Add framework services.
            services.AddMvc(opt =>
           {
               if (!_env.IsProduction())
               {
                   opt.SslPort = 44318;
               }
               // this is to add SSL (https basically to be called the api)
               opt.Filters.Add(new RequireHttpsAttribute());
           })
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
                            IHostingEnvironment env,
                            ILoggerFactory loggerFactory,
                            CampDbInitializer seeder,
                            CampIdentityInitializer identitySeeder)
        {
            loggerFactory.AddConsole(_config.GetSection("Logging"));
            loggerFactory.AddDebug();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseCors(cfg =>
            //{
            //    cfg.AllowAnyHeader()
            //        .AllowAnyMethod()
            //        .WithOrigins("http://vioricafluer.nl");
            //});

            //app.UseIdentity();
            app.UseAuthentication();

            //app.UseJwtBearerAuthentication(new JwtBearerOptions()
            //{
            //    AutomaticAuthenticate = true,
            //    AutomaticChallenge = true,
            //    TokenValidationParameters = new TokenValidationParameters()
            //    {
            //        ValidIssuer = _config["Tokens:Issuer"],
            //        ValidAudience = _config["Tokens:Audience"],
            //        ValidateIssuerSigningKey = true,
            //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:KEy"])),
            //        ValidateLifetime = true
            //    }
            //});


            app.UseMvc(config =>
            {
                config.MapRoute("MainAPIRoute", "api/{controller}/{action}");
            });

            seeder.Seed().Wait();
            identitySeeder.Seed().Wait();
            //app.Run(async (context) =>
            //{
            //    await context.Response.WriteAsync("Hello World!");
            //});

        }
    }
}
