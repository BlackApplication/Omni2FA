using System.Text;
using Example.Backend;
using Example.Backend.Configuration;
using Example.Backend.Services;
using Example.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Omni2FA.AspNetCore.EntityFrameworkCore.Extensions;
using Omni2FA.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("Omni2FaExample"));

builder.Services.Configure<HostJwtOptions>(builder.Configuration.GetSection(HostJwtOptions.SectionName));
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IHostSessionIssuer, HostSessionIssuer>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddOmni2Fa(o => builder.Configuration.GetSection("Omni2Fa").Bind(o));
builder.Services.AddOmni2FaEntityFrameworkStore<AppDbContext>();

var hostJwt = builder.Configuration.GetSection(HostJwtOptions.SectionName).Get<HostJwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = hostJwt.Issuer,
            ValidateAudience = true,
            ValidAudience = hostJwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(hostJwt.SigningKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapOmni2Fa();

app.Run();
