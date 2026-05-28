using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using InternshipPortal.Services;
using InternshipPortal.Controllers;
using Resend;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers and routing mechanics
builder.Services.AddControllers();

// 2. Configure Database Connection via Dapper
builder.Services.AddScoped<IDbConnection>(sp => 
    new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Register Feature Services with Clean Structural Namespace Overlays
builder.Services.AddScoped<IApplicationAdminService, ApplicationAdminService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// 4. Configure Resend Client Options
builder.Services.AddOptions();
builder.Services.Configure<ResendClientOptions>(builder.Configuration.GetSection("ResendByline"));
builder.Services.AddHttpClient<IResend, ResendClient>();

// 5. Enforce Strict Frontend CORS Policies
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularAppPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 6. Set Up Secure JWT Authentication Guards
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "name"
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 7. Establish the Middleware Request Pipeline
app.UseHttpsRedirection();
app.UseCors("AngularAppPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();