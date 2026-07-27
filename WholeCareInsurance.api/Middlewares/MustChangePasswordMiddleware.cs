using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WholeCareInsurance.api.Data;

namespace WholeCareInsurance.api.Middlewares
{
    // Cierra el gap documentado en PENDIENTE.md §10.1: MustChangePassword solo se
    // aplicaba en el frontend (redirect de rutas) — un access token válido podía
    // seguir pegándole a cualquier endpoint mientras el flag siguiera en true.
    //
    // MustChangePassword no viaja como claim del JWT (a propósito: así un Admin
    // puede forzar el cambio sobre una sesión ya activa sin esperar a que expire
    // el token — ver AppLayout.jsx, que ya reconcilia esto contra GET /users/me).
    // Por eso acá también se consulta la base en cada request autenticado, no un claim.
    public class MustChangePasswordMiddleware
    {
        // GET /users/me queda exento a propósito: es el endpoint que el frontend
        // ya usa (AppLayout.jsx) para detectar el flag en una sesión activa y
        // redirigir a /change-password. Si lo bloqueáramos acá, el frontend
        // recibiría un 403 en vez del flag y nunca se enteraría de que tiene
        // que cambiar la contraseña — se rompería el mecanismo que este mismo
        // punto busca reforzar, no solo dejarlo sin tapar.
        private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/auth/change-password",
            "/auth/logout",
            "/auth/refresh",
            "/users/me",
        };

        private readonly RequestDelegate _next;

        public MustChangePasswordMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            if (context.User.Identity?.IsAuthenticated == true && !ExemptPaths.Contains(context.Request.Path.Value ?? string.Empty))
            {
                var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    var mustChangePassword = await db.Users
                        .Where(u => u.Id == userId)
                        .Select(u => u.MustChangePassword)
                        .FirstOrDefaultAsync();

                    if (mustChangePassword)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new ProblemDetails
                        {
                            Title = "Hay que cambiar la contraseña antes de continuar.",
                            Status = StatusCodes.Status403Forbidden
                        }, options: null, contentType: "application/problem+json");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
