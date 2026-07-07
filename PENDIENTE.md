# Pendientes — WholeCareInsurance

---

## 1. Policies — campos y funcionalidades nuevas

### 1.1 Campo Tipo (dropdown) — ✅ Hecho
- `Type` en `Policy` restringido a `Obama Care`, `Salud`, `Auto`, `Otro` vía `[AllowedValues]` en `PolicyCreateDto`.
- Frontend: `<select>` en el formulario de Policies, igual al de estatus migratorio en Customers.
- El campo `Type` ya existía en el modelo/DB; no hizo falta migración nueva, solo la validación.

### 1.2 Dependientes — ✅ Hecho
Los dependientes son **Customers** vinculados a un Customer principal dentro de una póliza.
Ejemplo: Javier Hernández es el titular; su esposa e hijo son dependientes en la misma póliza.

- Tabla intermedia `PolicyDependents` (`PolicyId`, `CustomerId` — clave compuesta). Cascade al borrar la póliza; Restrict en el FK a Customer (necesario para evitar múltiples cascade paths en SQL Server, ya que Customer también cascadea a Policy vía el titular).
- Endpoints: `GET/POST /api/policies/{id}/dependents`, `DELETE /api/policies/{id}/dependents/{customerId}` (el GET no estaba en el diseño original pero es necesario para listar los dependientes actuales en el frontend).
- Frontend: sección "Dependientes" en el formulario de Policies, visible solo al **editar** una póliza ya guardada (no al crear una nueva). Buscador de clientes filtra en el cliente la lista ya cargada de `customers` (sin nuevo endpoint de búsqueda).
- De paso se corrigió un bug preexistente: el `<select>` de Customer y la columna Customer de la tabla referenciaban `c.name`/`c.documentNumber` (inexistentes) en lugar de `firstName`/`lastName`/`socialSecurityNumber`.

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

## 2. Consentimiento firmado y comunicación con clientes

### 2.1 Firma digital de consentimiento de póliza — ⏸ Pendiente de decisión del responsable
Al generar una póliza, enviar notificación (SMS y/o email) al cliente para que firme digitalmente el consentimiento; nosotros recibimos la notificación y el PDF firmado.

**Opciones de proveedor de firma electrónica (a elegir por quien paga/decide):**
- **SignWell** — API simple, plan pago accesible (~$8-20/mes según volumen), hosted signing page, webhook al firmar. Menor curva de integración.
- **Dropbox Sign (HelloSign)** — muy conocido, API similar de simple, precio parecido, buena documentación.
- **Documenso** (self-host, open source) — sin costo por envío, pero hay que hostearlo nosotros mismos (más trabajo inicial de infraestructura).
- **DocuSign** — el más robusto/enterprise, pero más complejo de dar de alta (aprobación de cuenta developer) y más caro; probablemente overkill para este caso.

**Notificación — SMS vs. email:**
- Solo email (más simple): el proveedor de firma ya envía el email con el link, cero integraciones extra.
- Email + SMS: sumar Twilio (~10-15 líneas) para reenviar el mismo link de firma por SMS. Requiere cuenta de Twilio.

**Flujo general (independiente del proveedor elegido):**
1. Al crear la póliza, generar el PDF de consentimiento y crear la solicitud de firma vía la API del proveedor.
2. El proveedor notifica al cliente (email, y opcionalmente SMS con el mismo link).
3. Cliente firma en la hosted signing page del proveedor.
4. Proveedor llama a un webhook nuestro cuando se firma → descargamos el PDF firmado y lo asociamos a la `Policy` (nuevo campo de estado de consentimiento + ubicación del PDF).

Implementación pausada hasta que el responsable del requerimiento elija proveedor (y quién lo paga).

### 2.2 Botón de WhatsApp para agentes — pendiente
Permitir que el agente contacte directamente al cliente (para pedir documentación u otro trámite) desde Customers/Policies.
- Opción simple y de bajo esfuerzo: botón "WhatsApp" que abre un link `https://wa.me/<telefono>?text=...` (click-to-chat), usando el `Phone` ya guardado en `Customer`. No requiere API de WhatsApp Business ni credenciales — solo formatear el teléfono a formato internacional y abrir el link.
- Alternativa más compleja (no necesaria por ahora): integrar WhatsApp Business API para enviar mensajes automatizados/plantillas desde el backend — requiere cuenta de Meta Business verificada y proveedor (Twilio, 360dialog, etc.).

---

## 3. Refactorizaciones pendientes

- Mover la URL base de la API (`http://localhost:5279`) a una variable de entorno Vite (`VITE_API_URL`) para no tener el puerto hardcodeado en cada componente.
- Crear un hook `useApi` o un cliente centralizado (`api.js`) que adjunte el token automáticamente, en lugar de leer `localStorage` en cada componente.
- Agregar manejo de token expirado: si la API devuelve 401, intentar refresh automático y reintentar; si falla, redirigir al login.
- Considerar mover las DTOs de Customer y Policy a archivos separados fuera del controlador.

---

## 4. Orden sugerido de trabajo

1. ~~Tipo en Policy (backend + frontend)~~ ✅ Hecho
2. ~~Dependientes (backend: modelo + endpoints → frontend: buscador + botón agregar)~~ ✅ Hecho
3. Buscador/filtro de pólizas (backend: query params → frontend: inputs de filtro) — siguiente
4. Botón de WhatsApp (click-to-chat) — rápido, sin dependencias externas
5. Modal de detalle de póliza
6. Refactorizaciones (variable de entorno, hook de API, refresh automático)
7. Firma digital de consentimiento — bloqueado hasta que el responsable elija proveedor (ver §2.1)
