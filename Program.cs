
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Starter.Api.Data;
using Starter.Api.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Starter.Api.Auth;
using FluentValidation;
using FluentValidation.AspNetCore;
using Starter.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration));

// Configurations
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "dev_secret_change_me";
var connString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
                 ?? "server=localhost;port=3306;user=root;password=YourPassword;database=starterdb;";

// EF Core + MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connString);
});

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

// JWT Auth

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<PasswordHasher>();


var app = builder.Build();

app.UsePathBase("/starter");

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global error handling minimal
app.UseSerilogRequestLogging();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();      
app.UseStaticFiles(); 

app.MapControllers();

app.MapFallbackToFile("/index.html");

// Ensure DB created and seed admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await Seed.EnsureAdminAsync(scope.ServiceProvider);
}

app.Run();

public static class Seed
{
    public static async Task EnsureAdminAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<PasswordHasher>();

        // 1) Permissions (insere se não existirem)
        string[] perms =
        {
            // Items
            "Items.Read","Items.Create","Items.Update","Items.Delete",
            // Users
            "Users.Read","Users.Create","Users.Update","Users.Delete",
            // Roles
            "Roles.Read","Roles.Create","Roles.Update","Roles.Delete",
            // Permissions
            "Permissions.Read","Permissions.Create","Permissions.Update","Permissions.Delete",
            // Amarrações
            "RolePermissions.Assign","UserRoles.Assign",
            // Settings
            "Settings.Access"
        };

        foreach (var name in perms)
        {
            if (!await db.Permissions.AnyAsync(p => p.Name == name))
            {
                db.Permissions.Add(new Permission { Name = name, Description = name });
            }
        }
        await db.SaveChangesAsync();

        // 2) Roles (Admin e User)
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            adminRole = new Role { Name = "Admin", Description = "Administrator" };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();
        }

        var userRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (userRole == null)
        {
            userRole = new Role { Name = "User", Description = "Default user" };
            db.Roles.Add(userRole);
            await db.SaveChangesAsync();
        }

        // 3) Admin recebe TODAS as permissões
        var allPermIds = await db.Permissions.Select(p => p.Id).ToListAsync();
        var existingAdminPerms = await db.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var toAdd = allPermIds.Except(existingAdminPerms).Select(pid =>
            new RolePermission { RoleId = adminRole.Id, PermissionId = pid }).ToList();

        if (toAdd.Count > 0)
        {
            db.RolePermissions.AddRange(toAdd);
            await db.SaveChangesAsync();
        }

        // 4) Usuário admin (cria se não existir)
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser == null)
        {
            adminUser = new User
            {
                Username = "admin",
                Email = "admin@example.com",
                Role = "Admin", // legado; o que vale são as relações abaixo
                PasswordHash = hasher.Hash("ChangeMe123!")
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync();
            Log.Information("Seeded default admin user (username: admin / password: ChangeMe123!). Change ASAP.");
        }

        // 5) Vincular admin → Role Admin (UserRoles)
        var hasLink = await db.UserRoles.AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);
        if (!hasLink)
        {
            db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
            await db.SaveChangesAsync();
        }
    }
}

