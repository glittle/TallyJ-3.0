# TallyJ .NET 9 Migration Guide

## Overview

This guide outlines the process for migrating TallyJ from .NET Framework 4.8 to .NET 9. Due to the architectural differences between .NET Framework and .NET 9, this is a substantial migration that requires careful planning and execution.

## Current State Analysis

### Dependencies
- **ASP.NET MVC 5** → Needs migration to **ASP.NET Core MVC**
- **Entity Framework 6** → Needs migration to **Entity Framework Core**  
- **OWIN/Katana** → Needs migration to **ASP.NET Core middleware**
- **Unity DI Container** → Should migrate to **Microsoft.Extensions.DependencyInjection**
- **System.Web.*** → Needs replacement with **ASP.NET Core** equivalents

### Key Challenges
1. **Global.asax.cs** → **Program.cs/Startup.cs** pattern
2. **HttpContext.Current** → **IHttpContextAccessor** injection
3. **Web.config** → **appsettings.json** + **IConfiguration**
4. **System.Web.Mvc** → **Microsoft.AspNetCore.Mvc**
5. **BinaryFormatter** → **System.Text.Json** or safe alternatives

## Migration Strategy

### Phase 1: Preparation (Recommended First Step)
1. **Maintain .NET Framework 4.8 version** as the stable production system
2. **Create comprehensive test coverage** for existing functionality
3. **Document all APIs and external integrations**
4. **Set up CI/CD pipeline** to support both versions during transition

### Phase 2: Create New .NET 9 Project Structure
```
TallyJ-3.0/
├── src/
│   ├── TallyJ.Core/           # Business logic (.NET 9)
│   ├── TallyJ.Data/           # Data access with EF Core (.NET 9)  
│   ├── TallyJ.Web/            # ASP.NET Core web app (.NET 9)
│   └── TallyJ.Legacy/         # Keep existing .NET 4.8 app
├── tests/
│   ├── TallyJ.Core.Tests/
│   ├── TallyJ.Data.Tests/
│   └── TallyJ.Web.Tests/
└── docs/
    └── migration-notes.md
```

### Phase 3: Core Components Migration

#### 3.1 Business Logic Layer (TallyJ.Core)
```csharp
// Extract framework-independent business logic
public class ElectionAnalyzer
{
    // Pure business logic - no System.Web dependencies
}

public class BallotProcessor  
{
    // Voting logic - framework agnostic
}
```

#### 3.2 Data Access Layer (TallyJ.Data)
```csharp
// Migrate from EF6 to EF Core
public class TallyJDbContext : DbContext
{
    public DbSet<Election> Elections { get; set; }
    public DbSet<Ballot> Ballots { get; set; }
    public DbSet<Person> People { get; set; }
    // ... other entities
}
```

#### 3.3 Web Layer (TallyJ.Web)
```csharp
// ASP.NET Core Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<TallyJDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

var app = builder.Build();

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<MainHub>("/mainHub");

app.Run();
```

### Phase 4: Key Code Transformations

#### 4.1 Configuration Migration
```csharp
// OLD: Web.config
<appSettings>
  <add key="Environment" value="Dev"/>
</appSettings>

// NEW: appsettings.json
{
  "Environment": "Dev",
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=TallyJ;Integrated Security=true;"
  }
}

// NEW: Usage in .NET 9
public class SomeService
{
    private readonly IConfiguration _config;
    
    public SomeService(IConfiguration config)
    {
        _config = config;
    }
    
    public string GetEnvironment() => _config["Environment"];
}
```

#### 4.2 Dependency Injection Migration
```csharp
// OLD: Unity Container
UnityInstance.Container.RegisterType<IService, Service>();

// NEW: Microsoft.Extensions.DependencyInjection
builder.Services.AddScoped<IService, Service>();
```

#### 4.3 HTTP Context Migration
```csharp
// OLD: System.Web
var user = HttpContext.Current.User;

// NEW: ASP.NET Core
public class MyController : Controller
{
    public IActionResult Index()
    {
        var user = HttpContext.User; // Available in controllers
    }
}

// Or inject IHttpContextAccessor for services
public class MyService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public MyService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public string GetCurrentUser()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    }
}
```

## Testing Strategy

### 1. Parallel Development
- Keep .NET 4.8 version running in production
- Develop .NET 9 version alongside
- Implement feature parity testing

### 2. Data Migration Testing
- Test EF6 → EF Core migration scripts
- Validate data integrity
- Performance comparison testing

### 3. Integration Testing
- API compatibility testing
- Authentication flow validation
- Real-world usage scenarios

## Deployment Strategy

### Option A: Big Bang Deployment
- Complete migration before deploying
- Higher risk but faster completion
- Requires extensive testing

### Option B: Gradual Migration
- Deploy side-by-side initially
- Migrate users gradually
- Lower risk, longer timeline

## Success Metrics

- [ ] All existing features working in .NET 9
- [ ] Performance equal or better than .NET 4.8 version
- [ ] All tests passing
- [ ] Security audit completed
- [ ] Production deployment successful

## Timeline Estimate

- **Phase 1 (Preparation)**: 2-4 weeks
- **Phase 2 (New Structure)**: 4-6 weeks  
- **Phase 3 (Core Migration)**: 8-12 weeks
- **Phase 4 (Testing & Deployment)**: 4-6 weeks

**Total Estimated Timeline**: 4-6 months

## Next Steps

1. ✅ **Proof of Concept Created** - See `TallyJ.Net9.Demo/` folder
2. **Get Stakeholder Approval** for timeline and approach
3. **Set up Development Environment** for parallel development
4. **Begin Phase 1 Preparation** work
5. **Create detailed technical specifications** for each component

## Proof of Concept

The `TallyJ.Net9.Demo` project demonstrates:
- ✅ .NET 9 compatibility 
- ✅ Modern C# syntax usage
- ✅ System.Text.Json serialization
- ✅ Async/await patterns
- ✅ Modern configuration patterns

This proves TallyJ can successfully run on .NET 9 with appropriate architectural changes.