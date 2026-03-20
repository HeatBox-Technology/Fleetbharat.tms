using Microsoft.EntityFrameworkCore;
using FleetBharat.TMSService.Domain.Entities.TMS;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<ErrorLog> ErrorLogs { get; set; }
    public DbSet<RouteMaster> Routes { get; set; }
    public DbSet<RouteStop> RouteStops { get; set; }
    public DbSet<TripPlan> TripPlans { get; set; }
    public DbSet<TripPlanRouteDetail> PlanRouteDetails { get; set; }
    public DbSet<TransTrip> TransTrips { get; set; }
    public DbSet<DetTrip> DetTrips { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
