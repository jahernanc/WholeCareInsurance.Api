# WholeCareInsurance

Sistema de administración de seguros. Consta de una API REST en ASP.NET Core y un panel de administración en React.

## Proyectos

| Proyecto | Tecnología | Puerto |
|---|---|---|
| `WholeCareInsurance.api` | .NET 9 · ASP.NET Core · EF Core 9 · SQL Server | `5279` |
| `wholecare-admin-vs` | React 19 · React Router v7 · Vite 8 | `5173` |

## Requisitos previos

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (incluido con Visual Studio) o SQL Server Express
- [Node.js 20+](https://nodejs.org/)

## Puesta en marcha

### 1. API

```bash
cd WholeCareInsurance.api

# Aplicar migraciones y crear la base de datos
dotnet ef database update

# Levantar el servidor (crea el usuario admin al arrancar)
dotnet run
```

La primera vez que arranca, `AdminUserSeeder` crea automáticamente el usuario administrador:

| Campo | Valor |
|---|---|
| Email | `admin@wholecare.com` |
| Contraseña | `Admin123!` |
| Rol | `Admin` |

Swagger disponible en `http://localhost:5279/swagger`.

### 2. Frontend

```bash
cd wholecare-admin-vs
npm install
npm run dev
```

Panel disponible en `http://localhost:5173`. Crea un archivo `.env` (excluido del repo, ver `.env.example`) con la URL de la API:

```
VITE_API_URL=http://localhost:5279
```

## Configuración

Copia `appsettings.json` y crea `appsettings.Development.json` (excluido del repo) para sobreescribir valores locales:

```json
{
  "Jwt": {
    "Key": "cambia-esta-clave-en-produccion",
    "Issuer": "WholeCareApi",
    "Audience": "WholeCareClient",
    "AccessTokenMinutes": 60
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=WholeCareInsuranceDb;Trusted_Connection=True;"
  }
}
```

> La `Jwt:Key` debe tener al menos 32 caracteres en producción.

## Endpoints principales

### Auth

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| `POST` | `/auth/login` | Público | Devuelve `accessToken` + `refreshToken` |
| `POST` | `/auth/refresh` | Público | Rota el refresh token |
| `POST` | `/auth/logout` | Autenticado | Invalida el refresh token |
| `POST` | `/auth/register` | Admin | Crea un nuevo usuario |

### Recursos

| Método | Ruta | Acceso |
|---|---|---|
| `GET/POST` | `/customers` | Autenticado |
| `GET/PUT/DELETE` | `/customers/{id}` | Autenticado |
| `GET/POST` | `/policies` | Autenticado |
| `GET/PUT/DELETE` | `/policies/{id}` | Autenticado |
| `GET/POST` | `/users` | Admin |
| `GET/PUT/DELETE` | `/users/{id}` | Admin |

## Flujo de autenticación

1. El cliente hace `POST /auth/login` y recibe un access token (JWT, 60 min) y un refresh token.
2. El access token se envía en el header `Authorization: Bearer <token>`.
3. Al expirar, el cliente hace `POST /auth/refresh` para obtener nuevos tokens (rotación automática).
4. `POST /auth/logout` invalida el refresh token en base de datos.

Los refresh tokens se almacenan como hash SHA-256 con índice filtrado para búsquedas eficientes.

## Roles

| Rol | Permisos |
|---|---|
| `Admin` | Acceso total, puede registrar nuevos usuarios |
| `Agente` | Acceso a clientes y pólizas |

## Despliegue (VPS / EasyPanel)

VPS: Ubuntu 24.04, KVM2 (2 CPU, 8GB RAM, 100GB disco), con [EasyPanel](https://easypanel.io) preinstalado.

**Arquitectura:**
- SQL Server corre como **contenedor** Docker (`mcr.microsoft.com/mssql/server`), no instalación nativa — Ubuntu 24.04 no es compatible con SQL Server nativo.
- Un solo contenedor de SQL Server compartido entre Test y Producción (limitación de RAM del VPS), con 2 bases de datos separadas: `WholeCareInsuranceDb_Test` y `WholeCareInsuranceDb_Prod`.
- El frontend resuelve `VITE_API_URL` vía **build-arg**, no en runtime (Vite inlinea las variables `VITE_*` en el bundle al buildear) — cada ambiente reconstruye su propia imagen apuntando a su propia API.
- Las migraciones de EF Core corren automáticamente al iniciar el contenedor de la API (`dbContext.Database.MigrateAsync()` en `Program.cs`, solo fuera de `Development`) — no hace falta correr `dotnet ef database update` a mano en Test/Prod.
- Detrás del proxy de EasyPanel (termina TLS ahí): la API no fuerza redirect a HTTPS dentro del contenedor (rompería con un redirect loop) y confía en el header `X-Forwarded-Proto` vía `UseForwardedHeaders`.

**Archivos de referencia** (repo root y cada proyecto):
- `WholeCareInsurance.api/Dockerfile` — build multi-stage (SDK → runtime `aspnet:9.0`), escucha en el puerto `8080`.
- `wholecare-admin-vs/Dockerfile` — build multi-stage (Node → `nginx:alpine`), sirve el build estático con fallback SPA (`wholecare-admin-vs/nginx.conf`) para que las rutas de React Router no den 404 al refrescar.
- `docker-compose.yml` — referencia de la topología completa (sqlserver + api-test + api-prod + frontend-test + frontend-prod). Los valores de contraseñas/dominios/claves son placeholders — **reemplazarlos** antes de usarlo. En EasyPanel normalmente se da de alta cada servicio por separado desde su UI (apuntando al Dockerfile de cada proyecto), pero el compose sirve como documentación de qué variables necesita cada uno y también se puede correr tal cual con `docker compose up` para probar la topología completa en local.

**Variables de entorno que necesita la API** (ver `docker-compose.yml` para el mapeo completo):

| Variable | Ejemplo | Notas |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Test` / `Production` | Cualquier valor distinto de `Development` desactiva Swagger y activa el auto-migrate |
| `ConnectionStrings__DefaultConnection` | `Server=sqlserver;Database=WholeCareInsuranceDb_Prod;...` | |
| `Jwt__Key` | (mín. 32 caracteres) | Distinta por ambiente |
| `Jwt__Issuer` / `Jwt__Audience` / `Jwt__AccessTokenMinutes` | | Opcional, tienen default en `appsettings.json` |
| `Cors__AllowedOrigin` | `https://tu-dominio.com` | Debe matchear el dominio real del frontend de ese ambiente |
| `Frontend__BaseUrl` | `https://tu-dominio.com` | Usado para armar el link de "olvidé mi contraseña" |
| `Brevo__ApiKey` / `Brevo__SenderEmail` | | Sin `Brevo__ApiKey`, el backend cae a un servicio que solo loguea el email en vez de enviarlo — hay que setearla en Test/Prod para que la recuperación de contraseña envíe emails reales |

**Volumen persistente:** `App_Data/PolicyDocuments` (dentro del contenedor, relativo al `WORKDIR /app`) necesita un volumen — sin él, los documentos de pólizas se pierden en cada redeploy.

## Estructura del repositorio

```
WholeCareInsurance/
├── WholeCareInsurance.api/       # Backend ASP.NET Core
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── DTOs/
│   ├── Data/
│   └── Migrations/
└── wholecare-admin-vs/           # Frontend React
    └── src/
```
