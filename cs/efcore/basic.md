## TestApp
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TestApp.DbContext\TestApp.DbContext.csproj" />
    <ProjectReference Include="..\TestApp.SqliteMigrations\TestApp.SqliteMigrations.csproj" />
  </ItemGroup>
</Project>
```
## Add sqlite service
```cs
Host.CreateDefaultBuilder()
.ConfigureServices((context, services) => {
    services.AddDbContext<MyDbContext>(options =>
    {
        string? provider = config.GetValue("Provider", "Sqlite");
        string? connectionString = config.GetConnectionString("MyDbContext");
        if (provider == "Sqlite")
            options.UseSqlite(connectionString,
                x => x.MigrationsAssembly("TestApp.SqliteMigrations"));
        else
            throw new NotSupportedException(provider);
    });
}).Build();
```
## Create/Migrate db
```cs
var context = _host.Services.GetRequiredService<MyDbContext>();
await context.Database.MigrateAsync();
```
## TestApp.SqliteMigrations.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
    <ProjectReference Include="..\TestApp.DbContext\TestApp.DbContext.csproj" />
  </ItemGroup>
</Project>
```
## TestApp.DbContext
Adding migration - in Package Manager Console:
- *(comment `<RuntimeIdentifier>` in csproj/props if any)*
- select project 'TestApp.SqliteMigrations'
- Add-Migration SomeName -Args "--provider Sqlite"
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
  </ItemGroup>
</Project>
```
```cs
using Microsoft.EntityFrameworkCore;

public class MyDbContext : DbContext
{
    public DbSet<DbTask> Tasks { get; set; } = null!;
    public DbSet<DbPlannedTask> PlannedTasks { get; set; } = null!;

    public MyDbContext(DbContextOptions<MyDbContext> context) : base(context) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<DbPlannedTask>()
            .HasKey(x => new { x.PlanDay, x.TaskId });

        builder.Entity<DbTask>()
            .HasMany(e => e.ChildTasks)
            .WithOne(e => e.Parent)
            .HasForeignKey(e => e.ParentId);
    }
}
public class DbTask
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public DbTask? Parent { get; set; }
    public List<DbTask> ChildTasks { get; set; } = null!;
}
public class DbPlannedTask
{
    public int PlanDay { get; set; }
    public int TaskId { get; set; }
}
```
