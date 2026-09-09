using MyOptiAlloySite.Extensions;
using EPiServer.Cms.Shell.UI;
using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.Data;
using EPiServer.DependencyInjection;
using EPiServer.Scheduler;
using EPiServer.Web.Routing;
using OptiPowerTools.Hangfire.Extensions;
using OptiPowerTools.ScheduledJobsInsights.Extensions;

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

        services.AddOptiPowerToolHangfire(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("EPiServerDB")
                ?? throw new InvalidOperationException("Hangfire connection string is not configured.");
        });

        services.AddOptiPowerToolsScheduledJobsInsights(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("EPiServerDB")
                ?? throw new InvalidOperationException("Scheduled Jobs Insights connection string is not configured.");
        });

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

        app.UseOptiPowerToolHangfire();

        app.UseEndpoints(endpoints =>
        {
            // Scheduled Jobs Insights' Blazor hub has to be mapped inside this block, before
            // MapContent(). MapContent() consolidates every endpoint data source registered on the
            // route builder into its own snapshot; anything flushed to RouteOptions by an earlier,
            // separate UseEndpoints(...) call gets consolidated as well while still staying
            // registered on its own, so it ends up matched twice and every request to it fails with
            // AmbiguousMatchException. Mapping it here keeps it in the single snapshot.
            endpoints.MapOptiPowerToolsScheduledJobsInsights();

            // MapContent() also maps the MVC controllers, so no MapControllers() here — a second
            // call registers an independent ControllerActionEndpointDataSource and duplicates every
            // attribute-routed action in the application, Optimizely's own included.
            endpoints.MapContent();
        });

        // Map... above only maps the hub; Use... is what applies the pending EF migrations, and it
        // no-ops on the hub because the Map... call already marked it as mapped.
        app.UseOptiPowerToolsScheduledJobsInsights();
    }
}
