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

Panel disponible en `http://localhost:5173`.

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
