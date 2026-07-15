using Microsoft.AspNetCore.Mvc;

namespace WholeCareInsurance.api.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostEnvironment _env;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, IHostEnvironment env, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción no controlada en {Method} {Path}", context.Request.Method, context.Request.Path);

                // Ya se empezó a escribir la respuesta (ej. streaming de documentos) —
                // no se puede pisar el status code ni el body, solo queda loguear.
                if (context.Response.HasStarted)
                    throw;

                var problem = new ProblemDetails
                {
                    Title = "Ocurrió un error inesperado.",
                    Status = StatusCodes.Status500InternalServerError
                };

                // Nunca en producción: ni ex.Message (puede traer datos internos,
                // ej. fragmentos de connection string en errores de SQL) ni el stack trace.
                if (_env.IsDevelopment())
                    problem.Detail = ex.ToString();

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                // WriteAsJsonAsync pisa cualquier ContentType seteado antes si no se lo
                // pasa explícito acá — sin el parámetro, manda "application/json" en vez
                // de "application/problem+json" (mismo Content-Type que ya usa el resto
                // de la API para ProblemDetails, vía BadRequest(new ProblemDetails{...})).
                await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
            }
        }
    }
}
