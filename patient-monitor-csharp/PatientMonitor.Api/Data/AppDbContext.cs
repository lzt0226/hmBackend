using Microsoft.EntityFrameworkCore;
using PatientMonitor.Api.Models;

namespace PatientMonitor.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<BehaviorLog> BehaviorLogs => Set<BehaviorLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("patient");
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.RoomNumber);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Severity);
        });

        modelBuilder.Entity<BehaviorLog>(entity =>
        {
            entity.ToTable("behavior_log");
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.BehaviorType);
            entity.HasIndex(e => e.IsAbnormal);
            entity.HasIndex(e => e.RecordTime);
            entity.HasOne(e => e.Patient)
                  .WithMany()
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// 初始化数据库：建表 + 种子数据
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 确保数据库和表存在
        context.Database.EnsureCreated();

        // 插入默认患者数据（仅当表为空时）
        if (!context.Patients.Any())
        {
            var patients = new[]
            {
                new Patient { Id = 1, Name = "张三", Age = 65, Gender = "男", RoomNumber = "301", Status = "normal", Severity = 0 },
                new Patient { Id = 2, Name = "李四", Age = 72, Gender = "女", RoomNumber = "302", Status = "normal", Severity = 0 },
                new Patient { Id = 3, Name = "王五", Age = 58, Gender = "男", RoomNumber = "303", Status = "normal", Severity = 0 },
                new Patient { Id = 4, Name = "赵六", Age = 80, Gender = "女", RoomNumber = "304", Status = "normal", Severity = 0 },
                new Patient { Id = 5, Name = "孙七", Age = 45, Gender = "男", RoomNumber = "305", Status = "normal", Severity = 0 },
            };
            context.Patients.AddRange(patients);
            context.SaveChanges();
        }
    }
}
