using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore; // para LINQ no EF
using Microsoft.IdentityModel.Tokens;
using Starter.Api.Data;
using Starter.Api.Models;

public class JwtService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly string _secret;
    private readonly string? _issuer;
    private readonly string? _audience;

    public JwtService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
        _secret  = Environment.GetEnvironmentVariable("JWT_SECRET")
                   ?? _config["Jwt:Secret"]
                   ?? "dev_secret_change_me";
        _issuer  = _config["Jwt:Issuer"];
        _audience= _config["Jwt:Audience"];
    }

    // --- 100% síncrono ---
    public string CreateToken(User user)
    {
        // 1) Carrega roles e permissions do banco (sync)
        var roles = _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        // fallback para o campo antigo User.Role
        if (roles.Count == 0 && !string.IsNullOrWhiteSpace(user.Role))
            roles.Add(user.Role);
        if (roles.Count == 0)
            roles.Add("User");

        var perms = _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name))
            .Distinct()
            .ToList();

        // 2) Claims
        var claims = new List<Claim>
        {
            // identificação/usuário
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),                 // subject (numérico)
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),                   // <- necessário p/ TryGetUserId
            // new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", user.Id.ToString()), // (opcional) se quiser compat legado

            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),          // id único do token
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),

            // arrays em JSON p/ consumo do front (opcional)
            new Claim("roles", JsonSerializer.Serialize(roles)),
            new Claim("perms", JsonSerializer.Serialize(perms))
        };

        // compatibilidade com [Authorize(Roles="...")]
        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r));

        // um claim por permissão (para policies "Perm:XYZ")
        foreach (var p in perms)
            claims.Add(new Claim("perm", p));

        // 3) Criptografia e expiração
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresMinutes = _config.GetValue<int?>("Jwt:ExpiresMinutes") ?? 60;

        var token = new JwtSecurityToken(
            issuer: _issuer,                 // pode ser null; configure se for validar
            audience: _audience,             // pode ser null; configure se for validar
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
