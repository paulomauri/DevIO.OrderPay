using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DevIO.OrderPay.Infra;

// Design-time only (used by `Add-Migration` / `Update-Database`). EF tools find this
// factory and build the DbContext directly, instead of booting the WebApi host —
// which blocks at design time and causes "Timed out waiting for the entry point to
// build the IHost". The running app still gets its connection string from DI/config.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost,1433;Database=OrderPayDb;User Id=sa;Password=Mauri@22;TrustServerCertificate=True;";

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
