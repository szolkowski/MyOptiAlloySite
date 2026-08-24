using MyOptiAlloySite.Extensions;
using EPiServer.Cms.Shell.UI;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Data;
using EPiServer.DependencyInjection;
using EPiServer.Scheduler;
using EPiServer.Web.Routing;

namespace MyOptiAlloySite;

public class Startup(IWebHostEnvironment webHostingEnvironment, IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        if (webHostingEnvironment.IsDevelopment())
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(webHostingEnvironment.ContentRootPath, "App_Data"));

            services.Configure<SchedulerOptions>(options => options.Enabled = false);
        }

        services.Configure<DataAccessOptions>(o => o.UpdateDatabaseCompatibilityLevel = true);

        services
            .AddCmsAspNetIdentity<ApplicationUser>()
            .AddCms()
            .AddCommerce()
            .AddAlloy()
            .AddAdminUserRegistration(options =>
            {
                // Defaults are Enabled | LocalRequestsOnly | SingleUserOnly, both of which block
                // this setup: in Docker the site sees host requests coming from the bridge
                // address rather than loopback, and the user table is not empty. Keep only
                // Enabled in development so /Util/Register stays reachable for recovering access.
                if (webHostingEnvironment.IsDevelopment())
                {
                    options.Behavior = RegisterAdminUserBehaviors.Enabled;
                }
            })
            .AddEmbeddedLocalization<Startup>();

        services.AddCommerceSeeding();

        // Required by Wangkanai.Detection
        services.AddDetection();

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromSeconds(10);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Required by Wangkanai.Detection
        app.UseDetection();
        app.UseSession();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapContent();
            endpoints.MapControllers();
        });
    }
}
