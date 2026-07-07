# Pendientes — WholeCareInsurance

---

## 1. Policies — campos y funcionalidades nuevas

### 1.1 Campo Tipo (dropdown)
- Agregar `Type` al modelo `Policy` como campo requerido.
- Valores permitidos: `Obama Care`, `Salud`, `Auto`, `Otro`.
- En el frontend: `<select>` igual al de estatus migratorio en Customers.
- Validación con `[AllowedValues]` en el DTO del backend.
- Nueva migración EF Core.

### 1.2 Dependientes
Los dependientes son **Customers** vinculados a un Customer principal dentro de una póliza.
Ejemplo: Javier Hernández es el titular; su esposa e hijo son dependientes en la misma póliza.

**Diseño propuesto:**
- Crear tabla intermedia `PolicyDependents` con columnas:
  - `PolicyId` (FK → Policies)
  - `CustomerId` (FK → Customers) — el dependiente
- El dependiente ES un Customer ya registrado en el sistema.
- En el formulario de póliza: botón **"Agregar dependiente"** que abre un buscador de clientes existentes para seleccionarlos.
- En el backend: endpoint `POST /api/policies/{id}/dependents` y `DELETE /api/policies/{id}/dependents/{customerId}`.

### 1.3 Buscador / filtro de pólizas
Filtros disponibles:
- Nombre del titular
- Apellido del titular
- Número de póliza
- Status
- Tipo

**Opciones de implementación:**
- A. Filtro en el frontend (si el volumen de datos es bajo): traer todas las pólizas y filtrar con JS.
- B. Query params en el backend: `GET /api/policies?nombre=&tipo=&status=` con `Where` dinámico en EF.
- Recomendado: opción B para escalar.

### 1.4 Vista de detalle de póliza (pendiente para más adelante)
- Al hacer clic en un resultado del buscador, abrir un **modal** (o página de detalle) con toda la info de la póliza:
  - Datos del titular
  - Lista de dependientes
  - Tipo, status, fechas, prima, número de póliza
- Implementar cuando los filtros y dependientes estén listos.

---

## 2. Refactorizaciones pendientes

- Mover la URL base de la API (`http://localhost:5279`) a una variable de entorno Vite (`VITE_API_URL`) para no tener el puerto hardcodeado en cada componente.
- Crear un hook `useApi` o un cliente centralizado (`api.js`) que adjunte el token automáticamente, en lugar de leer `localStorage` en cada componente.
- Agregar manejo de token expirado: si la API devuelve 401, intentar refresh automático y reintentar; si falla, redirigir al login.
- Considerar mover las DTOs de Customer y Policy a archivos separados fuera del controlador.

---

## 3. Orden sugerido de trabajo

1. Tipo en Policy (backend + frontend) — es pequeño y desbloquea el dropdown
2. Dependientes (backend: modelo + endpoints → frontend: buscador + botón agregar)
3. Buscador/filtro de pólizas (backend: query params → frontend: inputs de filtro)
4. Modal de detalle de póliza
5. Refactorizaciones (variable de entorno, hook de API, refresh automático)
