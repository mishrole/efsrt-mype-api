# Post Morten # 001-2026

| Campo                     | Detalle                                                                                   |
| ------------------------- | ----------------------------------------------------------------------------------------- |
| **Tipo**                  | Incidente detectado durante el desarrollo                                                 |
| **Entorno afectado**      | Desarrollo                                                                                |
| **Fecha**                 | 18/10/2026                                                                                |
| **Impacto observado**     | Interrupción de la solicitud y detención del depurador                                    |
| **Impacto para usuarios** | No identificado durante el incidente                                                      |
| **Riesgo potencial**      | Respuestas HTTP 500 y registro incorrecto de solicitudes canceladas como errores internos |
| **Estado**                | Corregido                                                                                 |

## Error

Al añadir una nueva ruta en frontend para navegar fuera del dashboard (primer espacio en el que el usuario está autenticado y tiene una sesión activa), se lanzaba un error no controlado en backend, detectado por el depurador durante el desarrollo.

### Backend

<img width="1487" height="945" alt="image" src="https://github.com/user-attachments/assets/7bf4e144-1802-4a31-b306-798a1748b1f4" />

### Frontend

<img width="1408" height="320" alt="image" src="https://github.com/user-attachments/assets/cbe59ee5-b268-46f9-ba2c-007321c807b1" />

## Contexto

Las excepciones son atrapadas por `ExceptionHandlingMiddleware` para ser loggeadas y construir la respuesta; sin embargo, no se tenía contenido el escenario de `OperationCanceledException` en el que efectivamente la solicitud en curso puede ser cancelada por una acción del usuario sin que eso constituya un error.

En frontend las solicitudes en curso se cancelan con `AbortController.abort()` y, al navegar hacia la nueva ruta, al existir solicitudes en vuelo que fueron canceladas, en backend se marca la cancelación de la solicitud con `context.RequestAborted.IsCancellationRequested == true`, `IsActiveAsync` recibe ese token y Entity Framework observa el token cancelado e interrumpe la consulta, propagando `OperationCanceledException`.

`RequestAborted` existe precisamente para notificar que la conexión fue abandonada y cancelar el trabajo asociado en curso.

El detalle es que `ExceptionHandlingMiddleware` lo atrapaba como error general y llamaba a `HandleExceptionAsync` para escribir la respuesta.

```csharp
private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                return Task.CompletedTask;
            }

            var detail = _env.IsDevelopment() ? exception.StackTrace : null;

            var response = HttpErrorMapper.FromException(
                exception,
                context.TraceIdentifier,
                detail
            );

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = response.StatusCode;

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(response, _jsonOptions),
                context.RequestAborted
            );
        }
    }
```

Pero, ¿qué pasa si el token ya está cancelado?

Al llegar al return e intentar escribir `context.RequestAborted` con un token cancelado, la escritura podía lanzar (y lanzó) una segunda `OperationCanceledException` no controlada. Esta nueva excepción no vuelve a entrar en los otros catch del mismo try, por lo que podía propagarse (y se propagó) fuera de `ExceptionHandlingMiddleware`.

## Remediación

1. Incluir el catch del escenario no controlado para evitar que ingrese en `HandleExcepcionAsync`
2. Evitar la escritura con `context.Response.HasStarted || context.RequestAborted.IsCancellationRequested` cuando una solicitud ya fue cancelada
3. No reutilizar `RequestAborted` como token para escribir errores reales, evitando una segunda cancelación ante un token ya cancelado

```csharp
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mype.Api.Common;
using Mype.Shared.Constants;

namespace Mype.Api.Middlewares
{
    [ExcludeFromCodeCoverage]
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment env
        )
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogDebug(
                    ErrorMessages.ExceptionHandlingMiddlewareRequestAbortedError,
                    context.TraceIdentifier
                );
            }
            catch (BadHttpRequestException exception)
            {
                _logger.LogWarning(exception, ErrorMessages.BadHttpRequestReceived);
                await HandleExceptionAsync(context, exception);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    ErrorMessages.ExceptionHandlingMiddlewareError,
                    exception.Message
                );
                await HandleExceptionAsync(context, exception);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted || context.RequestAborted.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            var detail = _env.IsDevelopment() ? exception.StackTrace : null;

            var response = HttpErrorMapper.FromException(
                exception,
                context.TraceIdentifier,
                detail
            );

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = response.StatusCode;

            var serializedResponse = JsonSerializer.Serialize(response, _jsonOptions);

            return context.Response.WriteAsync(serializedResponse, CancellationToken.None);
        }
    }
}

```
