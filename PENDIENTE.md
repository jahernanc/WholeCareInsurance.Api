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

### 1.3 Buscador / filtro de pólizas — ✅ Hecho
Filtros disponibles: nombre del titular, apellido del titular, número de póliza, status, tipo.

- Implementado con la opción B (recomendada): `GET /api/policies?firstName=&lastName=&policyNumber=&status=&type=` con `Where` dinámico contra la DB (`PolicyService.Search`), no filtrado en memoria — así escala mejor que el `GetAll()` + filtro in-memory que había antes.
- `firstName`/`lastName` filtran por `Customer.FirstName`/`LastName` (contains, case-insensitive por la collation default de SQL Server); `policyNumber` es contains; `status`/`type` son match exacto. Todos combinables (AND).
- Frontend: barra de filtros arriba de la tabla de Policies con inputs de texto + selects de Status/Type, botones Search y Clear.
- De paso se eliminó la duplicación de código: el filtrado en memoria que hacía `GetAll` antes de tener query params ya no existe; `GetById`/`GetPoliciesForCustomer` (en `CustomersController`) siguen usando `GetAll()` sin cambios.

### 1.4 Vista de detalle de póliza — ✅ Hecho (contenido base, faltan campos por definir)
- Botón 🔍 "View details" en cada fila de la tabla de Policies, abre un modal con:
  - Datos de la póliza: tipo, status, fechas, prima, número de póliza.
  - Datos del titular: nombre, SSN, email, teléfono, dirección, estatus migratorio (de `customers`, ya cargado en el frontend, sin fetch nuevo).
  - Lista de dependientes (reutiliza `GET /api/policies/{id}/dependents`).
- **Pendiente:** el responsable del requerimiento aún no definió qué información adicional debería mostrarse en el detalle (más allá de los datos que ya existen hoy en `Policy`/`Customer`). Cuando se defina, sumar esos campos al modal — no debería requerir cambios de estructura, solo agregar más `<p>` en la sección correspondiente (o nuevos campos en el modelo si la info no existe todavía).

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

### 2.2 Botón de WhatsApp para agentes — ✅ Hecho
Permitir que el agente contacte directamente al cliente (para pedir documentación u otro trámite) desde la vista de Policies.
- Implementado como click-to-chat: botón 💬 en cada fila de la tabla de Policies (junto a Editar/Eliminar) que abre `https://wa.me/<telefono>?text=...` en una pestaña nueva, tomando el `Phone` del `Customer` titular y limpiándolo a solo dígitos.
- Cada agente escribe desde su propio WhatsApp Web/Desktop activo en su computadora — el link no tiene un "número emisor" configurable, solo define el destinatario.
- Se descartó integrar WhatsApp Business API (mensajes automatizados desde el backend, número fijo de la empresa): decisión del usuario, no se necesita por ahora.

---

## 3. Refactorizaciones

### 3.1 Cliente API centralizado + variable de entorno + manejo de 401 — ✅ Hecho
- `VITE_API_URL` en `wholecare-admin-vs/.env` (gitignorado, mismo patrón que `appsettings.Development.json`) + `.env.example` commiteado documentando el valor esperado.
- `src/api.js`: módulo plano (no un hook, ninguna llamada necesita lifecycle de React) con `apiFetch(path, options)` que adjunta el `accessToken` automáticamente y agrega `Content-Type` cuando hay body.
- Manejo de 401: si la respuesta es 401 y hay `refreshToken`, `apiFetch` refresca y reintenta la request original una vez. El refresh está deduplicado (una sola llamada a `/auth/refresh` aunque varias requests en paralelo devuelvan 401 al mismo tiempo) — necesario porque el refresh token rota en cada uso, así que dos refrescos concurrentes con el token viejo se pisarían. Si el refresh falla, se llama a `logout()` (también deduplicado) y redirige a `/login`.
- De paso se conectó el botón de Logout (`Header.jsx`) a `POST /auth/logout`, que antes no se llamaba — el refresh token quedaba vivo en la DB hasta expirar (7 días) en vez de invalidarse al instante.
- `Login.jsx` no pasa por `apiFetch` (sin token, no aplica refresh) — solo usa `import.meta.env.VITE_API_URL` para la URL.
- Verificado con Playwright: login real, CRUD de una póliza vía UI, simulación de access token corrupto (auto-refresh transparente, un solo POST /auth/refresh), simulación de ambos tokens corruptos (logout automático deduplicado, redirect a /login, localStorage limpio).

### 3.2 Mover DTOs de Customer/Policy a archivos separados — pendiente
Quedó fuera de esta sesión a propósito: es un reordenamiento mecánico del backend sin relación con el resto del refactor de cliente API. `CustomersController`/`PoliciesController` siguen con sus DTOs anidados.

---

## 4. Orden sugerido de trabajo

1. ~~Tipo en Policy (backend + frontend)~~ ✅ Hecho
2. ~~Dependientes (backend: modelo + endpoints → frontend: buscador + botón agregar)~~ ✅ Hecho
3. ~~Botón de WhatsApp (click-to-chat)~~ ✅ Hecho
4. ~~Buscador/filtro de pólizas~~ ✅ Hecho
5. ~~Modal de detalle de póliza~~ ✅ Hecho (contenido base; faltan campos por definir, ver §1.4)
6. ~~Refactorizaciones: variable de entorno, cliente API, refresh automático~~ ✅ Hecho (ver §3.1)
7. Mover DTOs de Customer/Policy a archivos separados — pendiente, sin prioridad definida (ver §3.2)
8. Firma digital de consentimiento — bloqueado hasta que el responsable elija proveedor (ver §2.1)
