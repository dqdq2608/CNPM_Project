using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace IdentityServerLogic.Identity;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Lấy connection string từ env, nếu không có thì fallback Neon cứng
        var connectionString =
            Environment.GetEnvironmentVariable("IDS_DB_CONN")
            ?? "Host=ep-rough-mountain-a1gy8bdl-pooler.ap-southeast-1.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=npg_0HugRTp1zJlZ;Ssl Mode=Require;Trust Server Certificate=true";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
