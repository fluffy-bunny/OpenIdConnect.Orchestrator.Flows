using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using WebClient.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.UI.Services;
using WebClient.Services;
using Common;

namespace WebClient
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostEnvironment HostingEnvironment { get; }
        private ILogger _logger;
        private Exception _deferedException;
        public Startup(
            IConfiguration configuration,
            IHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
            _logger = new LoggerBuffered(LogLevel.Debug);
            _logger.LogInformation($"Create Startup {hostingEnvironment.ApplicationName} - {hostingEnvironment.EnvironmentName}");
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddDbContext<ApplicationDbContext>(config =>
                {
                    // for in memory database  
                    config.UseInMemoryDatabase("InMemoryDataBase");
                });
                services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();
                // services.AddDefaultIdentity must be adding its own fake
                // Switched to services.AddIdentity<IdentityUser,IdentityRole>, and now I have to add it.
                services.AddScoped<IEmailSender, FakeEmailSender>();
                services.AddScoped<ISigninManager, DefaultSigninManager>();
                services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, SeedSessionClaimsPrincipalFactory>();

                services.ConfigureApplicationCookie(options =>
                {
                    options.Cookie.Name = $"{Configuration["applicationName"]}.AspNetCore.Identity.Application";
                });
                services.AddAuthentication();
                services.AddAuthentication<IdentityUser>(Configuration);
                //*************************************************
                //*********** COOKIE Start ************************
                //*************************************************

                var cookieTTL = Convert.ToInt32(Configuration["authAndSessionCookies:ttl"]);
                services.Configure<CookiePolicyOptions>(options =>
                {
                    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                    options.CheckConsentNeeded = context => true;
                    options.MinimumSameSitePolicy = SameSiteMode.None;
                });
                services.ConfigureExternalCookie(config =>
                {
                    config.Cookie.SameSite = SameSiteMode.None;
                });
                services.ConfigureApplicationCookie(options =>
                {
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                    options.ExpireTimeSpan = TimeSpan.FromSeconds(cookieTTL);
                    options.SlidingExpiration = true;
                    options.Cookie.Name = $"{Configuration["applicationName"]}.AspNetCore.Identity.Application";
                    options.LoginPath = $"/Identity/Account/Login";
                    options.LogoutPath = $"/Identity/Account/Logout";
                    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
                    options.Events = new CookieAuthenticationEvents()
                    {

                        OnRedirectToLogin = (ctx) =>
                        {
                            if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == StatusCodes.Status200OK)
                            {
                                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                return Task.CompletedTask;
                            }
                            ctx.Response.Redirect(ctx.RedirectUri);
                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = (ctx) =>
                        {
                            if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == StatusCodes.Status200OK)
                            {
                                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                                return Task.CompletedTask;
                            }
                            ctx.Response.Redirect(ctx.RedirectUri);
                            return Task.CompletedTask;
                        }
                    };
                });


                //*************************************************
                //*********** COOKIE END **************************
                //*************************************************
                services.AddSession(options =>
                {
                    options.Cookie.IsEssential = true;
                    options.Cookie.Name = $"{Configuration["applicationName"]}.Session";
                    // Set a short timeout for easy testing.
                    options.IdleTimeout = TimeSpan.FromSeconds(cookieTTL);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                });
                services.AddControllers()
                    .AddSessionStateTempDataProvider();
                IMvcBuilder builder = services.AddRazorPages();
                builder.AddSessionStateTempDataProvider();
                if (HostingEnvironment.IsDevelopment())
                {
                    builder.AddRazorRuntimeCompilation();
                }

                // Adds a default in-memory implementation of IDistributedCache.
                services.AddDistributedMemoryCache();

                
            }
            catch (Exception ex)
            {
                _deferedException = ex;
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
                   IApplicationBuilder app,
                   IWebHostEnvironment env,
                   IServiceProvider serviceProvider,
                   ILogger<Startup> logger)
        {
            if (_logger is Microsoft.Extensions.Logging.LoggerBuffered)
            {
                (_logger as LoggerBuffered).CopyToLogger(logger);
            }
            _logger = logger;
            _logger.LogInformation("Configure");
            if (_deferedException != null)
            {
                _logger.LogError(_deferedException.Message);
                throw _deferedException;
            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCookiePolicy();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<Common.AuthSessionValidationMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
