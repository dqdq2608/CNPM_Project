using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityServerLogic.Identity;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // 🔥 Đọc lại connection string hoặc fallback mặc định
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite("Data Source=IdentityServer.db"); 
        // (hoặc UseSqlServer(...) nếu bạn dùng SQL Server)

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
