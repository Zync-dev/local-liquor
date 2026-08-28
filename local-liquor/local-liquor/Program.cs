using System.Globalization;
using System.Threading.RateLimiting;
using local_liquor.Data;
using local_liquor.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;

namespace local_liquor
{
    public class Program
    {
        /// <summary>Danish first — it is a Danish producer — with English as the alternate.</summary>
        private static readonly CultureInfo[] SupportedCultures = [new("da"), new("en")];

        /// <summary>Named policy on the sign-in form, to blunt password guessing.</summary>
        public const string LoginRateLimit = "login";

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Railway (and most PaaS) hand the port over in an environment variable.
            var port = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrWhiteSpace(port))
            {
                builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
            }

            var storage = new StoragePaths(builder.Configuration, builder.Environment);
            builder.Services.AddSingleton(storage);

            builder.Services.AddDbContext<LocalLiquorContext>(options =>
                options.UseSqlite(storage.ConnectionString));

            // Data protection keys sign the login cookie and the antiforgery tokens.
            // They default to a directory inside the container, which Railway throws
            // away on every deploy — so every deploy would log the admin out and
            // invalidate every form already open. Keep them on the volume instead.
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(storage.Keys))
                .SetApplicationName("local-liquor");

            builder.Services.AddRazorPages(options =>
                {
                    options.Conventions.AuthorizeFolder("/Admin");
                    // The way in has to stay open, or there is no way in.
                    options.Conventions.AllowAnonymousToPage("/Admin/Login");
                    options.Conventions.AllowAnonymousToPage("/Admin/Setup");
                })
                .AddViewLocalization();

            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            builder.Services.AddScoped<WineService>();
            builder.Services.AddScoped<MarketService>();
            builder.Services.AddScoped<MediaService>();
            builder.Services.AddScoped<AdminAccountService>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/admin/login";
                    options.LogoutPath = "/admin/logout";
                    options.AccessDeniedPath = "/admin/login";
                    options.ExpireTimeSpan = TimeSpan.FromDays(14);
                    options.SlidingExpiration = true;
                    options.Cookie.Name = "ll.admin";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                        ? CookieSecurePolicy.SameAsRequest
                        : CookieSecurePolicy.Always;
                });
            builder.Services.AddAuthorization();

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(LoginRateLimit, http =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 8,
                            Window = TimeSpan.FromMinutes(5),
                        }));
            });

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(SupportedCultures[0]);
                options.SupportedCultures = SupportedCultures;
                options.SupportedUICultures = SupportedCultures;
                // Cookie first so an explicit choice always beats the browser's Accept-Language.
                options.RequestCultureProviders =
                [
                    new CookieRequestCultureProvider(),
                    new AcceptLanguageHeaderRequestCultureProvider(),
                ];
            });

            builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);

            // Railway terminates TLS at its edge and forwards plain HTTP. Without
            // this the app believes every request is insecure, and UseHttpsRedirection
            // bounces it straight into a redirect loop.
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var app = builder.Build();

            app.UseForwardedHeaders();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            // A wrong /vin/{slug} should land on our page, not the blank browser one.
            app.UseStatusCodePagesWithReExecute("/Error");

            app.UseHttpsRedirection();
            app.UseRequestLocalization();

            // <input type="number"> always posts "8.5" and <input type="date"> always
            // posts "2026-06-14", whatever the page language is. Under da-DK model
            // binding rejects the first and misreads the second, so the admin — which
            // is all forms — runs on the invariant culture. Views that want Danish
            // month names ask for that culture explicitly.
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/admin"))
                {
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                }

                await next(context);
            });

            app.UseRouting();

            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            // Uploaded photos live on the volume, outside wwwroot, so they survive a
            // deploy. Served read-only from a fixed prefix, known types only.
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(storage.Uploads),
                RequestPath = "/media",
                ServeUnknownFileTypes = false,
                OnPrepareResponse = ctx =>
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable",
            });

            app.MapRazorPages()
               .WithStaticAssets();

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LocalLiquorContext>();
                await db.Database.MigrateAsync();
                await Seed.EnsureSeededAsync(db);
            }

            await app.RunAsync();
        }
    }
}
