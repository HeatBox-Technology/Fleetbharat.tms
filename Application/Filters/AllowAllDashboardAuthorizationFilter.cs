using Hangfire.Dashboard;

namespace FleetBharat.TMSService.Application.Filters
{
    public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}
