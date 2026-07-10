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
- Ver §1.6 para los campos `RelacionConPrincipal` (en `Customer`) e `IsAplicante` (en `PolicyDependent`) agregados después sobre esta misma base.

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
- Ver §1.7 para la sección "Documentos" (subir/descargar/eliminar archivos) agregada después dentro de este mismo modal.

### 1.5 Campo Compañía aseguradora (dropdown) — ✅ Hecho
- `InsuranceCompany` nuevo en `Policy`, requerido, restringido a `WholeCareInsurance`, `Otro` vía `[AllowedValues]` en `PolicyCreateDto` — mismo patrón que `Type` (§1.1).
- Migración EF Core aplicada: pólizas ya existentes en la base recibieron `WholeCareInsurance` como default (columna `NOT NULL`, no podía quedar en `null`).
- Frontend: `<select>` en el formulario de Policies, columna nueva en la tabla, filtro superior y línea nueva en el modal de detalle (§1.4).
- `PolicyService.Search` extendido con filtro opcional `insuranceCompany` (mismo patrón que `type`).

### 1.6 Relación con el principal (Customer) + Es aplicante (dependiente de póliza) — ✅ Hecho
- `RelacionConPrincipal` nuevo en `Customer` (no en `PolicyDependent`): es un atributo fijo de la persona, no cambia según la póliza. Requerido, `[AllowedValues]` con `Cónyuge`, `Hijo/a`, `Madre`, `Padre`, `Sobrino/a`, `Nieto/a`, `Hijastro/a`, `Hermano/a`, `Otro` — mismo patrón que `MigrationStatus`/`Type`. Clientes ya existentes recibieron `Otro` como default en la migración.
- `IsAplicante` (bool) nuevo en `PolicyDependent` (la tabla intermedia), no en `Customer`: a diferencia de `RelacionConPrincipal`, la misma persona puede ser aplicante en una póliza (ej. Obama Care) y no en otra (ej. Auto). Default `false` para los dependientes ya existentes — no marcado significa que sigue siendo parte del grupo familiar de esa póliza pero no se contabiliza como aplicante en reportes futuros.
- Nuevo endpoint `PUT /api/policies/{id}/dependents/{customerId}` para togglear `IsAplicante` de un dependiente ya agregado, sin tener que quitarlo y volver a agregarlo.
- Frontend: `<select>` de Relación con el principal en el formulario de Customers (y visible en la tarjeta de cada cliente); checkbox "Es aplicante" junto a cada dependiente ya agregado en la sección Dependientes del formulario de Policies.

### 1.7 Documentos de póliza (subir / descargar / eliminar) — ✅ Hecho
Nueva sección "Documentos" dentro del modal de detalle de póliza (§1.4). Tipos permitidos: `.pdf`, `.docx`, `.jpg`, `.jpeg`; tamaño máximo 5 MB.

- Nuevo modelo `PolicyDocument` (tabla nueva vía migración `AddPolicyDocuments`, sin impacto en datos existentes) con `PolicyId` (FK cascade), nombre original, nombre en disco, tipo de contenido, tamaño y fecha de subida.
- Archivos guardados en disco del servidor (no en la nube), organizados en una carpeta por `PolicyId` (`App_Data/PolicyDocuments/{policyId}/`), **fuera de `wwwroot`** y sin `UseStaticFiles()` — solo accesibles vía el endpoint autenticado, nunca por URL directa. Nombre en disco es un GUID (evita path traversal y colisiones); el nombre original del usuario nunca se usa para construir una ruta.
- Validación en el backend de extensión, tamaño **y contenido real del archivo** (magic bytes para PDF/JPEG; para `.docx` se verifica además que las entradas OOXML — `[Content_Types].xml`, `word/document.xml` — existan dentro del ZIP, no solo la firma de ZIP), todo con clases del BCL sin agregar ningún paquete NuGet nuevo.
- Endpoints: `POST/GET /api/policies/{id}/documents`, `GET/DELETE /api/policies/{id}/documents/{documentId}` (el GET de descarga valida `PolicyId` y `documentId` combinados para evitar acceso cruzado entre pólizas). Al borrar una `Policy` se limpia también su carpeta de documentos en disco.
- Frontend: tarjeta "Documents" con botón "+ New" para subir, lista de documentos con nombre (link azul), tamaño y fecha, y menú de tres puntos (⋮) con "Descargar"/"Eliminar" por documento — con confirmación antes de eliminar.
- De paso se corrigió un bug en `apiFetch` (`src/api.js`): forzaba `Content-Type: application/json` en cualquier request con body, lo que rompía los uploads `multipart/form-data` (el browser necesita fijar su propio `Content-Type` con el boundary).
- Verificado con curl (extensión inválida, tamaño excedido, contenido falso vs. real, acceso cruzado entre pólizas → 404, borrado físico, cleanup de carpeta al borrar la póliza) y con Playwright (subida real por la UI, validación de extensión en cliente, descarga con el nombre original correcto).

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

### 3.2 Mover DTOs de Customer/Policy a archivos separados — ✅ Hecho
- Nuevas carpetas `DTOs/Customers/` (`CustomerCreateDto`, `CustomerUpdateDto`, `CustomerResponseDto`) y `DTOs/Policies/` (`PolicyCreateDto`, `PolicyUpdateDto`, `PolicyResponseDto`, `DependentCreateDto`, `DependentResponseDto`), mismo patrón que `DTOs/Auth/` y `DTOs/Users/`.
- De paso se eliminó una duplicación: `PolicyResponseDto` estaba definida idéntica dentro de `PoliciesController` y de `CustomersController` (usada en `GetPoliciesForCustomer`) — ahora es una sola clase compartida.
- Verificado con curl: validaciones (`MigrationStatus`, `Type` inválidos → 400), alta de póliza, alta/listado de dependientes, todo funcionando igual que antes de mover los archivos.

---

## 4. Customers — asignación de agentes y datos demográficos

### 4.1 Agente / Agente Asistente / Agente Record + datos demográficos en Customer — ✅ Hecho
- Nuevos campos opcionales en `Customer`: `ZipCode`, `State`, `City`, `County`, `MaritalStatus`, `Occupation`. Ninguno usa `[AllowedValues]` salvo lo constreñido por el `<select>` del frontend — se probó que `[AllowedValues]` de .NET rechaza `null` en vez de tratarlo como "sin validar", así que hubiera roto estos campos por ser opcionales (mismo motivo por el que `County` tampoco lo usa: además la lista es demasiado grande).
- `Condado`: dataset de los 3143 condados oficiales del US Census Bureau (`national_county.txt`), bundleado como JSON estático en `wholecare-admin-vs/src/data/usCounties.json`, agrupado por estado. El `<select>` de Condado se resetea si cambia el Estado.
- `AgentId`/`AssistantAgentId`/`RecordAgentId` nuevos en `Customer` (FKs a `User`, `OnDelete Restrict`, todas nullable — los Customers ya existentes quedan sin agente asignado, no se les inventó un valor). `IsEncargado` nuevo en `User` para poder filtrar el dropdown de Agente Record (solo agentes marcados como Encargado).
- **La asociación automática de un Customer al agente logueado no existía antes de este cambio** (se creía que sí, se confirmó auditando el código que no había ningún vínculo). Ahora está enteramente en el servidor, no solo oculto en el UI: un usuario no-Admin siempre se auto-asigna como `AgentId` al crear un Customer (el backend ignora cualquier valor que mande el body) y no puede reasignar agentes al editar uno existente; un Admin puede setear los tres campos, validados server-side contra `Rol == "Agente"` (y `IsEncargado == true` para Agente Record).
- Nueva página `/agentes` (solo visible/accesible para Admin, con redirect si un no-Admin navega ahí directamente) para alta y edición de agentes, con el checkbox "Encargado". Requirió agregar `PUT /users/{id}`, que no existía — antes no había ninguna forma de editar un `User` ya creado.
- De paso, fix de seguridad: `GET /users` y `GET /users/me` devolvían la entidad `User` cruda (`PasswordHash`/`RefreshTokenHash` incluidos en el JSON) — ahora usan los DTOs de respuesta que ya existían pero no se usaban.
- Verificado con curl (auto-asignación de agente para no-Admin, validación de `Rol`/`IsEncargado` en los 3 campos de agente, filtro `GET /users?role=Agente`, que un no-Admin no puede reasignar agentes al editar) y con Playwright (formulario de Customers con y sin los dropdowns según rol, alta/edición de agentes, reset de Condado al cambiar Estado, redirect de `/agentes` para no-Admin).

---

## 5. Dashboard y UX general

### 5.1 Dashboard — pendiente de definiciones
El Dashboard hoy es un placeholder (`<h1>Dashboard ✅</h1>` en `App.jsx`). Puede haber contenido nuevo para definir luego de la reunión con el responsable del requerimiento.

### 5.2 Selector de idioma (Español/Inglés) en el Header — ✅ Hecho
`react-i18next` con diccionarios por namespace (`common`, `login`, `customers`, `policies`, `agentes`, `enums`) en inglés/español, Inglés como default.
- Alcance: toda la UI (labels, botones, títulos, mensajes) **y** los valores mostrados en los dropdowns (`Type`, `MigrationStatus`, `RelacionConPrincipal`, `MaritalStatus`, `InsuranceCompany`, `Status`, `Rol`). El `translateEnum()` de `src/i18n/translateEnum.js` desacopla el valor guardado en la DB (siempre en español, el backend no cambió) del texto mostrado — no usa `t()` a propósito porque varios valores tienen `/` o espacios (`Hijo/a`, `Unión libre`) que romperían el key-parsing por defecto de i18next.
- Persistencia: `User.PreferredLanguage` nuevo en el backend (default `"en"`, migración aplicada), incluido directo en la respuesta de `/auth/login` (sin round-trip extra) y actualizable vía `PUT /users/me/language` (self-service, cualquier usuario autenticado, no solo Admin).
- Carga antes de renderizar: al hacer login el idioma llega en la misma respuesta; al recargar con sesión ya activa, `localStorage` actúa solo como cache de arranque rápido (pinta instantáneo) y `AppLayout` reconcilia en segundo plano contra `GET /users/me` sin bloquear el primer render — si el usuario cambió el idioma desde otra computadora, se corrige apenas responde esa llamada.
- Verificado con curl (`preferredLanguage` en la respuesta de login, `PUT /users/me/language`, rechazo de idioma inválido) y con Playwright (traducción de dropdowns en ambos idiomas, cambio de idioma en caliente con el modal de detalle de póliza abierto, persistencia tras logout/login, y el escenario de "otra computadora" con un browser context sin cache).

---

## 7. Hosting y despliegue (VPS)

### 7.1 Infraestructura — ⏸ Pendiente de implementación (Dockerfiles + compose + README)
VPS ya comprado y corriendo: Ubuntu 24.04, KVM2 (2 CPU, 8GB RAM, 100GB disco), con **EasyPanel** preinstalado (panel de gestión basado en Docker).

**Cambio de plan respecto a la decisión original (Hostinger + Ubuntu 22.04 + SQL Server nativo + NGINX/Certbot/systemd manual):** en vez de instalar SQL Server nativo en el servidor (no soportado oficialmente en Ubuntu 24.04), todo corre **vía Docker a través de EasyPanel**: SQL Server como contenedor (imagen oficial de Microsoft), la API .NET como contenedor, y el frontend como contenedor NGINX sirviendo el build estático. EasyPanel maneja reverse proxy y SSL automático — ya no hace falta configurar NGINX/Certbot/systemd a mano.

Dos ambientes (test y producción) como proyectos separados dentro de EasyPanel, cada uno con sus propias variables de entorno y contenedores de API/frontend.

**Decisiones tomadas para la dockerización (2026-07-10):**
- **Frontend / `VITE_API_URL`:** Vite hornea esta variable en build time, no se puede cambiar en runtime. Se pasa como `ARG` de Docker (build-arg) — cada app de EasyPanel (test/prod) define su propio valor y reconstruye la imagen para ese ambiente.
- **Migraciones EF Core:** auto-migrate al iniciar (`dbContext.Database.Migrate()` en `Program.cs`, junto al `AdminUserSeeder` que ya corre ahí) — sin esto habría que correr `dotnet ef database update` manualmente contra el contenedor en cada deploy.
- **Topología de SQL Server:** un solo contenedor `mcr.microsoft.com/mssql/server` compartido entre test y producción, con dos bases de datos separadas dentro (no dos contenedores completos) — con 8GB de RAM totales repartidos entre 2 ambientes de API+frontend+DB, dos instancias completas de SQL Server quedarían ajustadas.

**Otros hallazgos a resolver durante la implementación (no solo config, requieren cambio de código):**
- CORS (`Program.cs`, policy `AllowFrontend`) tiene `http://localhost:5173` hardcodeado — debe leerse de una variable de entorno (ej. `Cors:AllowedOrigin`) ya que el frontend en prod estará en otro dominio.
- `app.UseHttpsRedirection()` fuera de Development puede causar redirect loops detrás del reverse proxy de EasyPanel (que termina el SSL y habla HTTP al contenedor) — hay que condicionarlo con una variable propia y agregar `UseForwardedHeaders` para `X-Forwarded-Proto`.
- `App_Data/PolicyDocuments` (documentos subidos, §1.7) necesita un volumen persistente en el contenedor de la API — sin volumen, los documentos se pierden en cada redeploy.
- `Jwt:Key`/connection string hardcodeados en `appsettings.json` (el placeholder de dev y la connection string de `localdb`) deben vaciarse/neutralizarse ahí; en prod se inyectan por variable de entorno (`Jwt__Key`, `ConnectionStrings__DefaultConnection`, convención de doble guion bajo de ASP.NET Core). El `Jwt:Key` de producción debe ser un valor nuevo y fuerte (≥32 chars), distinto del de `appsettings.Development.json`.

**Próximo paso:** implementar `WholeCareInsurance.api/Dockerfile` (multi-stage SDK→runtime), `wholecare-admin-vs/Dockerfile` (multi-stage Node→NGINX), los cambios de código de arriba, un `docker-compose.yml` de referencia para probar localmente, y un `README.md` con los pasos de alta en EasyPanel (SQL Server compartido con 2 DBs → app API con sus env vars por ambiente → app frontend con su build-arg).

### 7.2 Migración de datos del sistema anterior — ⏸ Pendiente de archivo CSV
El responsable del proyecto va a proveer un CSV con la información actual de clientes, agentes y pólizas del sistema anterior, para migrar a esta base de datos. Pendiente de recibir el archivo (o una muestra) para definir estructura y mapeo de relaciones antes de armar el script de migración. La migración se probará primero contra la base del ambiente de test (ver 7.1), nunca directo contra producción.

---

## 6. Orden sugerido de trabajo

1. ~~Tipo en Policy (backend + frontend)~~ ✅ Hecho
2. ~~Dependientes (backend: modelo + endpoints → frontend: buscador + botón agregar)~~ ✅ Hecho
3. ~~Botón de WhatsApp (click-to-chat)~~ ✅ Hecho
4. ~~Buscador/filtro de pólizas~~ ✅ Hecho
5. ~~Modal de detalle de póliza~~ ✅ Hecho (contenido base; faltan campos por definir, ver §1.4)
6. ~~Refactorizaciones: variable de entorno, cliente API, refresh automático~~ ✅ Hecho (ver §3.1)
7. ~~Mover DTOs de Customer/Policy a archivos separados~~ ✅ Hecho (ver §3.2)
8. ~~Campo Compañía aseguradora en Policy~~ ✅ Hecho (ver §1.5)
9. ~~Relación con el principal (Customer) + Es aplicante (dependiente de póliza)~~ ✅ Hecho (ver §1.6)
10. ~~Documentos de póliza (subir/descargar/eliminar)~~ ✅ Hecho (ver §1.7)
11. ~~Agentes (Agente/Asistente/Record) + datos demográficos en Customer~~ ✅ Hecho (ver §4.1)
12. ~~Selector de idioma ES/EN~~ ✅ Hecho (ver §5.2)
13. Firma digital de consentimiento — bloqueado hasta que el responsable elija proveedor (ver §2.1)
14. Dashboard — bloqueado hasta la reunión con el responsable (ver §5.1)
15. Infraestructura de hosting (VPS) — bloqueado hasta la compra del VPS en Hostinger (ver §7.1)
16. Migración de datos del sistema anterior — bloqueado hasta recibir el CSV (ver §7.2)
