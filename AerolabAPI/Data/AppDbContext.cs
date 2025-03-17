using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Visitor> Visitors { get; set; }
    public DbSet<AttendanceLog> AttendanceLogs { get; set; }
    public DbSet<Faq> Faqs { get; set; }
}
