using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using WholeCareInsurance.api.Data;
using WholeCareInsurance.api.Middlewares;
using WholeCareInsurance.api.Services;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<AdminUserSeeder>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IPolicyHistoryService, PolicyHistoryService>();
builder.Services.AddScoped<IInsuranceCompanyService, InsuranceCompanyService>();
builder.Services.AddSingleton<IPolicyDocumentStorage, PolicyDocumentStorage>();

// Sin Brevo:ApiKey configurado (dev local sin cuenta real) cae a un servicio que
// solo loguea el email — pasar a envío real en Test/Prod es solo variable de entorno.
if (!string.IsNullOrWhiteSpace(builder.Configuration["Brevo:ApiKey"]))
    builder.Services.AddHttpClient<IEmailService, BrevoEmailService>();
else
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Add services to the container.
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WholeCareInsurance.api", Version = "v1" });

    // 🔐 Configuración para que aparezca el botón Authorize
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese: Bearer {su token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// En Test/Prod se inyecta por Cors__AllowedOrigin (build-arg por ambiente en el
// frontend, cada uno con su propio origin) — default a localhost:5173 para dev local.
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});



builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Rate limiting por IP solo en los endpoints públicos/sensibles de auth (blanco de
// fuerza bruta o spam) — el resto de la API, ya protegida por JWT, queda sin límite.
// Particiona por RemoteIpAddress, que ya viene resuelto vía X-Forwarded-For gracias
// a UseForwardedHeaders (corre antes que UseRateLimiter en el pipeline, ver abajo).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Demasiados intentos. Probá de nuevo en un momento.",
            Status = StatusCodes.Status429TooManyRequests
        }, options: null, contentType: "application/problem+json", cancellationToken: token);
    };

    // Login es el blanco clásico de fuerza bruta — límite más estricto que el resto.
    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Register (requiere Admin autenticado, pero igual limitado por abuso) /
    // forgot-password / reset-password (público, blanco de enumeración o spam de emails).
    options.AddPolicy("AuthSensitivePolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});


var app = builder.Build();

// Primera línea del pipeline a propósito: envuelve todo lo que sigue
// (forwarded headers, CORS, auth, controllers) para capturar cualquier
// excepción no controlada en cualquier punto del request.
app.UseMiddleware<GlobalExceptionMiddleware>();

// Detrás del proxy de EasyPanel (termina TLS ahí, el contenedor solo ve HTTP):
// sin esto, UseHttpsRedirection vería siempre "http" y generaría un redirect
// loop. Con XForwardedProto, la app conoce el esquema real de la request original.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRateLimiter();

app.UseCors("AllowFrontend");

// Auto-migrate al iniciar el contenedor (decisión de despliegue, §8.1) — en dev
// local se sigue usando `dotnet ef database update` a mano. Tiene que correr
// antes del seeder: es Migrate() el que crea la base si todavía no existe.
if (!app.Environment.IsDevelopment())
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<AdminUserSeeder>();
    await seeder.Seed();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
