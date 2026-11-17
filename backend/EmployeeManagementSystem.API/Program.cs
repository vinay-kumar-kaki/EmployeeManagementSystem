using System.Security.Claims;
using System.Text;
using EmployeeManagementSystem.API.Data;
using EmployeeManagementSystem.API.Models.Entities;
using EmployeeManagementSystem.API.Services.Implementations;
using EmployeeManagementSystem.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Hosting platforms such as Render inject PORT; ASP.NET Core must bind to it.
var hostedPort = builder.Configuration["PORT"];
if (!string.IsNullOrWhiteSpace(hostedPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{hostedPort}");
}

// 1. Database Connection: Supports both Sqlite (instant, zero-setup) and SqlServer
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection") 
        ?? "Data Source=EmployeeManagement.db";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteConnection));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// 2. ASP.NET Core Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    // Uppercase is optional so the documented demo passwords (admin@123 / employee@123) work.
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. JWT Authentication Setup
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey is missing from configuration.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Deny tokens belonging to an employee whose profile has since been deactivated.
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                context.Fail("User identifier claim is missing from the token.");
                return;
            }

            var employee = await db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee != null && !employee.IsActive)
            {
                context.Fail("This account has been deactivated.");
            }
        }
    };
});

// 4. Register Custom Application Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// 5. CORS Configuration (Allows the Angular client to make API calls)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 6. Controllers and JSON formatting
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// 7. Swagger Documentation with JWT Bearer Button
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Employee & Leave Management API",
        Version = "v1",
        Description = "ASP.NET Core Web API for managing employees, departments, leaves, and attendance."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token directly (e.g. eyJhbGciOi...)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (secretKey.StartsWith("SuperSecretKeyFor", StringComparison.OrdinalIgnoreCase))
{
    app.Logger.LogWarning("JWT SecretKey is still the development default. Set JwtSettings__SecretKey via environment variable before deploying.");
}

// 8. Automatic Database Migration and Seeding
await DbInitializer.SeedAsync(app.Services, app.Configuration);

// 9. HTTP Request Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Management API v1");
    });
}

// Use CORS before Authentication & Authorization
app.UseCors("AllowAngularDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Lightweight health endpoint used by the hosting platform.
app.MapGet("/health", () => Results.Ok("healthy"));

// Serve the Angular SPA from the same origin as the API in production.
if (Directory.Exists(Path.Combine(app.Environment.ContentRootPath, "wwwroot")))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

app.Run();
