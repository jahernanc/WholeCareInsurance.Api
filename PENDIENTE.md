# Pendientes — WholeCareInsurance

> Auditado contra código real el 2026-07-13 (modelos, DTOs, migraciones aplicadas y componentes de frontend). Donde el pedido del responsable no coincidía con lo implementado, se priorizó lo verificado en código — ver notas "⚠️ Discrepancia" en los puntos afectados.
>
> Re-auditado el 2026-07-27: paginado en Policies (§14), cierre del gap de seguridad de MustChangePassword (§10.1), reconciliación de campos de Policy (§1.11), agentes reales del sistema anterior (§15, completo), Dashboard (§9), Modal/Dialog reutilizable (§16) y unificación de listados de Customers/Agentes + paginado en los 3 (§17) — ver detalle en cada sección.

---

## 1. Policies — campos y funcionalidades

### 1.1 Campo Tipo (dropdown) — ✅ Hecho
`Type` en `Policy` restringido a `Health Insurance (ACA)`, `Medicare`, `Life Insurance`, `Supplemental Plans`, `Auto`, `Otro` vía `[AllowedValues]` en `PolicyCreateDto`. `<select>` en el formulario, igual patrón que el resto de los enums.

**Actualización (§12.10)**: el valor `Salud` se renombró a `Medicare` (era en realidad ese tipo) al implementar los campos específicos de Medicare — migración `AddMedicarePolicyFieldsAndRenameSaludToMedicare` incluye un `UPDATE` de datos para las pólizas ya guardadas con `Type = 'Salud'`.

**Actualización**: `Obama Care` se renombró a `Health Insurance (ACA)` — a pedido del responsable, ahora el valor guardado en `Type` y el texto mostrado en pantalla son idénticos (se descartó la separación previa entre valor interno y label traducido). Migración `UpdateObamaCareToHealthInsuranceACA` incluye el `UPDATE` de datos para las pólizas ya guardadas con `Type = 'Obama Care'`. Se eliminó la entrada `"Obama Care"` de `enums.json` (es/en): `translateEnum` cae al valor crudo cuando no encuentra la key, así que no hace falta un mapeo idéntico.

### 1.2 Dependientes (vínculo con Customers existentes) — ✅ Hecho
Los dependientes son **Customers** vinculados a un Customer principal dentro de una póliza (tabla intermedia `PolicyDependents`, `PolicyId`+`CustomerId`).
- Endpoints: `GET/POST /api/policies/{id}/dependents`, `PUT/DELETE /api/policies/{id}/dependents/{customerId}`.
- Frontend: sección "Dependientes" en el formulario de Policies, visible solo al **editar** una póliza ya guardada. El buscador filtra en el cliente sobre la lista de `customers` ya cargada, y desde esa misma sección también se puede **crear un Customer nuevo** y vincularlo como dependiente en el mismo paso — ver §2, ya cerrado.

### 1.3 Buscador / filtro de pólizas — ✅ Hecho
`GET /api/policies?firstName=&lastName=&policyNumber=&status=&type=&insuranceCompanyId=&period=` con `Where` dinámico contra la DB (`PolicyService.Search`). Filtros combinables (AND), barra de filtros en el frontend con Search/Clear. (El filtro de aseguradora pasó de `insuranceCompany` (texto) a `insuranceCompanyId` cuando ese campo se rediseñó a tabla propia, §1.5 — actualizado acá también.)

### 1.4 Vista de detalle de póliza — ✅ Hecho (contenido base, faltan campos por definir)
Modal con datos de la póliza, datos del titular, lista de dependientes y documentos (§1.7). Pendiente: el responsable aún no definió qué información adicional debería mostrarse — sumar cuando se defina.

### 1.5 Campo Compañía aseguradora — ✅ Hecho, rediseñado a tabla propia (§ análisis archivo real)
**Reemplaza la versión anterior de este punto**: el `[AllowedValues("WholeCareInsurance", "Otro")]` original no se sostenía contra los datos reales (30+ aseguradoras confirmadas en el archivo de migración, ni "WholeCareInsurance" ni "Otro" aparecen). Se rediseñó como tabla propia (`InsuranceCompany`: `Id`, `Name`, `IsActive`) en vez de ampliar el enum — la lista es larga y va a seguir creciendo, y así un Admin puede agregar una aseguradora nueva sin deploy.
- `Policy.InsuranceCompany` (string) → `Policy.InsuranceCompanyId` (FK) + navegación. `PolicyResponseDto` expone `InsuranceCompanyId` + `InsuranceCompanyName` (mismo criterio que `AgentName` en `CustomerResponseDto`).
- CRUD completo: `Controllers/InsuranceCompaniesController.cs` (`GET` para cualquier autenticado, `POST`/`PUT` solo Admin, con chequeo de nombre duplicado). Baja lógica vía `IsActive` (`OnDelete Restrict` en la FK — no se puede borrar en duro una aseguradora que ya tiene pólizas).
- Página Admin `/insurance-companies` (mismo patrón que `/agentes`): alta, edición de nombre, toggle activo/inactivo.
- Migración `20260715173327_AddInsuranceCompaniesAndPolicyPlanDetails` — crea la tabla y siembra 31 aseguradoras confirmadas por el archivo real: Aetna, Ambetter, AmeriHealth Caritas, Ameritas, Anthem, Avmed, Blue Cross Blue Shield, Bright Health, Care Source, Cigna, Community Health Choice, Fl Health Care Plans, Florida Blue, Florida Blue Dental, Friday, Health First, Kaiser Permanente, Medicaid, Molina Healthcare, One Dental, Oscar, Scott And White, Select Health, Simply, U Health Plans, United, Usable - Accidents, Usable - Critical Illness, Usable - Hospitalization, Wellcare, Wellpoint. Sin valor `"Otro"` sembrado — el sentido de la tabla es no necesitar catch-all.
- `Policies.jsx`: el `<select>` de aseguradora ahora carga la lista real por API (no un array hardcodeado); las inactivas se muestran con sufijo "(Inactiva)" para no ocultar el valor ya guardado en una póliza vieja sin ofrecerlo para pólizas nuevas.
- Verificado con curl (listado de 31, alta, alta duplicada rechazada con 400, edición + toggle activo/inactivo, `PolicyService.Search` filtrando por `insuranceCompanyId`) y con Playwright (página `/insurance-companies` completa, dropdown de Policies poblado desde la API, alta de póliza con aseguradora real, nombre correcto en tabla y detalle).

### 1.6 Relación con el principal (Customer) + Es aplicante (dependiente de póliza) — ✅ Hecho
- `RelacionConPrincipal` en `Customer` (`[Required]`, `[AllowedValues]`: `Cónyuge`, `Hijo/a`, `Madre`, `Padre`, `Sobrino/a`, `Nieto/a`, `Hijastro/a`, `Hermano/a`, `Otro`) — atributo fijo de la persona, no cambia según la póliza.
- `IsAplicante` (bool) en `PolicyDependent` (`Models/PolicyDependent.cs:11`) — confirmado en el modelo y en la migración `20260710173730_AddRelacionPrincipalAndIsAplicante`. La misma persona puede ser aplicante en una póliza y no en otra.
- `PUT /api/policies/{id}/dependents/{customerId}` togglea `IsAplicante`. Checkbox "Es aplicante" junto a cada dependiente en el frontend.

### 1.7 Documentos de póliza (subir / descargar / eliminar) — ✅ Hecho
Modelo `PolicyDocument`, migración `AddPolicyDocuments` aplicada. Archivos en disco fuera de `wwwroot`, validación de extensión/tamaño/magic bytes. Endpoints `POST/GET /api/policies/{id}/documents`, `GET/DELETE /api/policies/{id}/documents/{documentId}`. Frontend: tarjeta "Documents" en el modal de detalle.

### 1.8 Period (año de vigencia/cobertura) — ✅ Hecho
`Period` (int, obligatorio) en `Models/Policy.cs`, migración `20260713192538_AddPolicyPeriodAndApplicants` aplicada (default `2026` para pólizas ya existentes — la tabla estaba vacía al momento del cambio). **No es un campo editable dentro del formulario de Policy** — decisión explícita: el único control es un `<select>` en el header global de la app (`Header.jsx`, junto al selector de idioma), con las opciones año actual hasta 5 años atrás (6 valores), default año actual, persistido en `localStorage` (`selectedPeriod`) vía estado levantado a `AppLayout.jsx` y compartido a las páginas ruteadas por `Outlet context`.
- Comportamiento: cambiar el Período en el header **filtra la tabla de Policies** (`PolicyService.Search` ahora acepta `period`, mismo patrón que `insuranceCompany`) y define el valor que se graba en una póliza nueva al crearla. Al editar una póliza existente, el Período grabado se conserva tal cual (no se pisa con el valor activo del header).
- Verificado con curl (filtro `?period=2026`/`?period=2023` devuelve solo lo esperado, `period=1999` rechazado por `[Range(2000,2100)]`) y con Playwright (6 opciones correctas en el header, default año actual, alta de póliza estampa el Período del header, cambiar el header oculta/muestra la póliza en la tabla, editar preserva el Período sin importar el header).

### 1.9 Number of applicants — ✅ Hecho
`NumberOfApplicants` (int, opcional) en `Models/Policy.cs` y DTOs, mismo migración que §1.8. Carga manual del agente, ubicado dentro de la sección "Dependientes" del formulario de Policy (visible solo al editar, mismo criterio que el resto de esa sección, §1.2) y mostrado en el modal de detalle. Verificado con curl (round-trip, rechazo de negativos) y con Playwright (visible solo en edición, persiste tras guardar).

### 1.10 Enum de Status de Policy — ✅ Hecho
`PolicyCreateDto.Status` ahora restringido vía `[AllowedValues]` a 8 valores canónicos en español (mismo patrón que `Type`/`MigrationStatus`): `Draft`, `Pendiente`, `Cancelado`, `Por procesar`, `En proceso`, `Actualizado`, `Procesado`, `Cambio de agente`. Default cambiado de `"Active"` a `"Draft"`. Traducciones EN agregadas en `en/enums.json` (`Pending`, `Canceled`, `To be processed`, `In Process`, `Updated`, `Processed`, `Agent change`).
- Migración `20260713180205_AddPolicyStatusEnum` aplicada: remapea datos existentes (`Cancelled`→`Cancelado`, `Active`/`activa`→`Procesado`, `Expired`→`Cancelado`) con `ELSE Status` como red de seguridad para valores no contemplados. Verificado contra la base de dev (0 pólizas al momento del cambio, por lo que no hubo remapeo real que auditar, pero la lógica quedó lista para Test/Prod).
- **Corrección post-análisis del archivo real de migración (Health/Obamacare)**: el 8vo valor original (`"En corrección"`) no existe en los datos reales — el valor real es `"Actualizado"`. Migración nueva `20260715173817_FixPolicyStatusActualizado` (sin cambio de esquema, solo `UPDATE Policies SET Status = 'Actualizado' WHERE Status = 'En corrección'`, red de seguridad para Test/Prod). La nota anterior de este punto (que decía que `"Actualizado"` no formaba parte del enum) queda revertida — sí es el valor real, reemplaza a `"En corrección"` en el `[AllowedValues]`, el `<select>` del frontend y `enums.json` (es/en). Ver §9.2, ya actualizada también.
- Verificado con curl: `"Active"` y `"En corrección"` (valores viejos) rechazados con 400; `"Actualizado"` (valor real) aceptado con 201.

### 1.11 Campos de plan (ACA) y financieros en Policy — ✅ Hecho (§ análisis archivo real)
5 campos nuevos confirmados por el archivo real de migración (Health/Obamacare, 1258 filas), todos opcionales — `Type` (§1.1) también cubre Auto/Otro, que no tienen metal tier ni Tax Credit/Subsidy:
- `PlanType` (dropdown: `Catastrophic`, `Bronze`, `Silver`, `Gold`, `Platinum` — metal tier de ACA, **distinto** de `Type`, ambos coexisten).
- `InsurancePlan` (texto libre, nombre específico del plan).
- `EffectiveDate` (fecha, inicio de cobertura).
- `TaxCreditSubsidy` (decimal, opcional, rechaza negativos).
- `MonthlyPremiumAmount` (decimal, opcional, rechaza negativos).

Migración `20260715173327_AddInsuranceCompaniesAndPolicyPlanDetails` (misma migración que §1.5 — EF Core no permite separar en dos migraciones distintas cuando ambos cambios de modelo ya están hechos, captura todo el diff pendiente de una vez). Formulario principal y modal de detalle de `Policies.jsx` actualizados.

**✅ Resuelta (análisis de datos + decisión del responsable, post-migración real)**: `Policy` ya tenía `StartDate`/`EndDate` y `Premium` — había superposición conceptual con `EffectiveDate`/`Period`/`MonthlyPremiumAmount`. Analizadas las 1211 pólizas migradas en la base de dev + el código del script (`WholeCareInsurance.Migration/Importers/ImportPipeline.cs:145-176`):
- `StartDate` = primera `EffectiveDate` del historial consolidado de cada póliza (línea 156) — coincide con la `EffectiveDate` vigente en 1198/1211 (99%, pólizas con un solo registro en el sistema viejo); difiere solo en 13 pólizas ACA con más de un registro histórico (cambio de plan a mitad de año).
- `EndDate` = siempre 31/12 del `Period` (línea 147, inferido a propósito porque el origen no tenía fecha de fin real — deja constancia con un warning en el reporte por cada póliza). 1210/1211 lo cumplen sin excepción.
- `Premium` = copia directa de `MonthlyPremiumAmount` cuando el origen la trae, si no queda en `0` (línea 173-176) — NO es `MonthlyPremiumAmount × 12`. 904 pólizas con valores idénticos + 281 con ambos en `0` + 25 con `MonthlyPremiumAmount` nulo y `Premium=0` = 1210/1211.
- El único caso (de 1211) que no encaja en ningún patrón (`Id 7021`, Medicare) tiene fechas y montos que no siguen la lógica del script de migración — es casi seguro un registro cargado a mano por curl/Swagger durante alguna sesión de pruebas, no un dato real migrado.

**Decisión del responsable**: dejar los 3 campos como están, sin deprecar ni redefinir, mientras no haya una necesidad real (ej. Dashboard, §9) que fuerce a resolver la redundancia. Documentado también como comentario corto en `Models/Policy.cs`.

Verificado con curl (alta con los 5 campos, alta sin ellos con `null`, edición, filtro `insuranceCompanyId`) y con Playwright (los 5 campos visibles en el formulario, alta y detalle end-to-end, sin errores de consola).

---

## 2. Extensión del flujo de Dependientes — crear Customer nuevo desde Members — ✅ Hecho

La sección Dependientes de Policies (§1.2) ahora tiene dos botones: "+ Add dependent" (buscar entre Customers existentes, como antes) y "+ Create new dependent" (nuevo). Al crear, se muestra el formulario completo de Customer inline; al enviarlo, el registro se crea vía `POST /api/customers` (Customer normal, sin ninguna tabla ni endpoint especial) y se vincula automáticamente a la póliza vía `POST /api/policies/{id}/dependents` (mismo endpoint que ya usaba el flujo de "buscar existente").

- **Paridad de campos garantizada por estructura, no por copiar/pegar**: se extrajo `src/components/CustomerFormFields.jsx` con todos los campos del formulario de Customer (incluidos los de §3.2), reutilizado tanto por `Customers.jsx` como por esta sección nueva de `Policies.jsx` — un cambio futuro a los campos de Customer se refleja automáticamente en ambos lugares. Las constantes de los `<select>` (`MIGRATION_STATUSES`, `GENDERS`, etc.) y `emptyCustomerForm` se movieron a `src/data/customerFormOptions.js` (archivo de datos puro, no componente, por la regla de Fast Refresh de ESLint que prohíbe mezclar exports de componentes y constantes en el mismo archivo).
- **Bug evitado**: el panel de "crear dependiente nuevo" tiene varios campos `required` (SSN, nombre, email, etc.). Si hubiera quedado anidado dentro del mismo `<form>` de Policy (como estaba el resto de la sección Dependientes), la validación nativa del navegador habría bloqueado el botón "Guardar" del formulario de Policy cada vez que el panel estuviera abierto con campos vacíos — sin importar que el usuario no tuviera intención de crear un dependiente en ese momento. Se movió toda la sección Dependientes (no solo el panel nuevo) a **fuera** del `<form>` de Policy, como hermano después de `</form>`; el guardado de la póliza y el guardado de "Number of applicants" siguen funcionando igual porque `handleSubmit` arma el body a mano desde el estado de React, no depende de que los inputs estén dentro del `<form>`.
- Verificado con Playwright: los 11 campos nuevos de §3.2 (Middle Name, Gender, Address #1/#2, Green Card, Work Permit, Employer Name, Company Phone, Annual Income, Tags, Contact Language) presentes en el panel; guardar la póliza con el panel abierto y vacío **no** se bloquea (confirma el fix de arriba); alta de un dependiente nuevo queda como Customer normal en la base y vinculado en `PolicyDependents` (confirmado por SQL directo); sin errores de consola.

---

## 3. Customers — campos nuevos

### 3.1 Ya implementado: Agente / Agente Asistente / Agente Record + datos demográficos — ✅ Hecho
- `Customer`: `ZipCode`, `State`, `City`, `County`, `MaritalStatus`, `Occupation` (todos opcionales, sin `[AllowedValues]` a propósito — ver comentario en `CustomerCreateDto.cs`).
- `County`: dataset de los 3143 condados del US Census Bureau, bundleado en `src/data/usCounties.json`, filtrado por Estado (`<select>` de Condado se resetea si cambia el Estado).
- `AgentId`/`AssistantAgentId`/`RecordAgentId` en `Customer` (FKs a `User`, nullable, `OnDelete Restrict`). No-Admin se auto-asigna como `AgentId` al crear (forzado server-side); Admin puede setear los tres, validados contra `Rol == "Agente"` (`RecordAgentId` además contra `IsEncargado == true`).
- Página `/agentes` (solo Admin) para alta/edición de agentes.

### 3.2 Campos nuevos de Customer — ✅ Hecho
Los 11 campos agregados a `Models/Customer.cs`, `CustomerCreateDto`/`CustomerResponseDto`, migración `20260713182551_AddCustomerNewFields`, y formulario/tarjeta de `Customers.jsx`:
- `MiddleName` (texto, opcional)
- `Gender` (dropdown: `Masculino`, `Femenino` — 2 valores, sin `[AllowedValues]` por ser opcional, traducidos vía `translateEnum`)
- `GreenCard` (texto, opcional)
- `WorkPermit` (texto, opcional)
- `Address1` (texto, obligatorio — **renombrado desde el campo `Address` original**, migración `RenameColumn` verificada sin pérdida de datos) / `Address2` (texto, opcional, nuevo)
- `EmployerName` (texto, opcional)
- `CompanyPhone` (texto, opcional)
- `AnnualIncome` (decimal, obligatorio, `[Range(0, ...)]` rechaza negativos; default `0` para los customers ya existentes al momento de la migración)
- `Tags` (texto libre — sigue sin definirse el uso exacto con el responsable, implementado como campo simple tal como estaba planteado)
- `ContactLanguage` (dropdown `Inglés`/`Español` — nombrado distinto de `Language` a propósito para no confundirse con `User.PreferredLanguage` §6.2, que es el idioma de la interfaz)

Verificado con curl+sqlcmd (round-trip completo, rechazo de `AnnualIncome` negativo, los 2 customers ya existentes conservaron su dirección bajo `Address1`) y con Playwright (alta, edición con pre-carga correcta, baja, sin errores de consola).

### 3.3 Renombrado "Legal Status" (label, sin cambio de modelo) — ✅ Hecho
`en/customers.json` y `en/policies.json` ahora muestran "Legal Status" en vez de "Migration Status" (español ya decía "Estatus migratorio", sin cambios ahí). El campo, los valores (`Permiso de trabajo`, `Residente permanente`, `Ciudadano`, `Otro`) y el modelo no cambiaron. De paso se agregó `"Asilo"` como quinto valor permitido en `[AllowedValues]` de `MigrationStatus` (sin migración de EF Core — no hay validación a nivel de base, solo DTO), reflejado en el `<select>` del frontend y en ambos diccionarios de `enums.json`.

### 3.4 Cambio en modelo de Agente — `IsEncargado` (NPM) — ✅ Hecho
`IsEncargado` (bool) en `Models/User.cs:10`, checkbox en el formulario de Agentes (`Agentes.jsx`, dentro de la sección de campos del formulario — sin línea fija, el archivo creció con los campos de §11), usado para filtrar el dropdown de Agente Record en Customers (§3.1). No queda nada pendiente en este punto.

---

## 4. Consentimiento firmado y comunicación con clientes

### 4.1 Firma digital de consentimiento de póliza — ⏸ Pendiente de decisión del responsable
Sin cambios desde la última revisión — confirmado que sigue sin implementar (no hay ninguna referencia a SignWell/DocuSign/HelloSign/Documenso en el código, solo en este documento).

**Opciones de proveedor:** SignWell, Dropbox Sign (HelloSign), Documenso (self-host), DocuSign — ver comparación completa más abajo en el historial de este documento si hace falta retomarla. **Notificación:** email solo, o email + SMS (Twilio). **Flujo:** generar PDF al crear la póliza → proveedor notifica al cliente → cliente firma en hosted signing page → webhook nuestro descarga el PDF firmado y lo asocia a la `Policy` (nuevo campo de estado de consentimiento + ubicación del PDF).

Implementación pausada hasta que el responsable elija proveedor (y quién lo paga).

### 4.2 Botón de WhatsApp para agentes — ✅ Hecho
Click-to-chat: botón 💬 en cada fila de la tabla de Policies, abre `https://wa.me/<telefono>?text=...` con el `Phone` del Customer titular.

---

## 5. Refactorizaciones

### 5.1 Cliente API centralizado + variable de entorno + manejo de 401 — ✅ Hecho
`VITE_API_URL`, `src/api.js` con `apiFetch`, refresh automático deduplicado en 401, logout deduplicado.

### 5.2 Mover DTOs de Customer/Policy a archivos separados — ✅ Hecho
`DTOs/Customers/`, `DTOs/Policies/`, mismo patrón que `DTOs/Auth/` y `DTOs/Users/`.

### 5.3 Mensajes de error del backend no llegaban al usuario — ✅ Hecho
Verificando `InsuranceCompanies` en el navegador se encontró que los errores de negocio (ej. "Ya existe una aseguradora con ese nombre") siempre mostraban un mensaje genérico en vez del motivo real. Causa: `BadRequest(string)` devuelve `Content-Type: text/plain`, pero el frontend siempre asumía JSON (`res.json().catch(() => null)`) — fallaba en silencio y caía al fallback genérico. No era un bug puntual de `InsuranceCompanies`: el mismo patrón estaba repetido en 5 call sites de 4 páginas (`Customers.jsx`, `Agentes.jsx`, `InsuranceCompanies.jsx`, `Policies.jsx` x2) contra 19 `BadRequest(string)` en 4 controllers (`PoliciesController`, `AuthController`, `InsuranceCompaniesController`, `CustomersController`).
- **Fix centralizado en `apiFetch` (`src/api.js`)**: ahora detecta el `Content-Type` de la respuesta de error y lee el mensaje correctamente sea texto plano o JSON, adjuntándolo como `res.errorMessage`. Las 5 páginas ya no tienen lógica propia de parseo — solo leen `res.errorMessage ?? <fallback traducido>`.
- **Los 19 `BadRequest(string)` del backend pasaron a `BadRequest(new ProblemDetails { Title = "..." })`** — mismo campo `title` que ya usan automáticamente los fallos de validación de DataAnnotations, converge en una sola convención. Cambio mecánico, sin tocar ningún otro comportamiento.
- **Detalle no obvio**: `ProblemDetails` serializa con `Content-Type: application/problem+json`, no `application/json` a secas — el chequeo en `apiFetch` busca la substring `"json"` en general, no `"application/json"` exacto, para cubrir ambos casos.
- Verificado con curl (los 3 controllers devuelven JSON con `title` en vez de texto plano) y con Playwright (nombre duplicado en `/insurance-companies` y email duplicado en `/agentes` muestran el mensaje real del backend, no el genérico).

**Mejora futura, no urgente, encontrada pero fuera de este alcance**: los fallos automáticos de `[ApiController]` por DataAnnotations (ej. `[AllowedValues]` inválido) ya devuelven JSON con `.title`, pero ese título es siempre el genérico "One or more validation errors occurred." — el mensaje específico por campo vive en `.errors` (dictionary), que ni `apiFetch` ni ninguna página leen hoy. Es la misma clase de problema (mensaje específico oculto al usuario) pero en un código distinto (factory de validación automática de ASP.NET Core, no `BadRequest` manual) — se dejó documentado para un posible fix aparte, no entró en este batch.

### 5.4 Middleware global de excepciones no controladas — ✅ Hecho
Como contraparte de §5.3 (que estandarizó los errores 400 esperados): si algo lanza una excepción no controlada (bug, falla de conexión a la base, etc.), antes se dejaba pasar cruda. `Middlewares/GlobalExceptionMiddleware.cs` (nuevo — ocupa la carpeta `Middlewares/` que existía vacía) la captura y devuelve `ProblemDetails` consistente con el resto de la API (mismo `Content-Type: application/problem+json`).
- Primera línea del pipeline en `Program.cs` (`app.UseMiddleware<GlobalExceptionMiddleware>()`, antes de `UseForwardedHeaders`/`UseCors`/`UseAuthentication`/`UseAuthorization`/`MapControllers`), para envolver todo el pipeline de requests.
- `Title` siempre genérico ("Ocurrió un error inesperado."), nunca lleva `ex.Message` ni en dev ni en prod. `Detail` (mensaje real + stack trace vía `ex.ToString()`) solo se llena si `IHostEnvironment.IsDevelopment()` — en producción el cliente no ve ni un fragmento del error real.
- Se loguea siempre server-side vía `ILogger<GlobalExceptionMiddleware>`, sin importar el ambiente — necesario para poder debuggear en Test/Prod donde la respuesta al cliente es genérica a propósito.
- Se sacó `<Folder Include="Middlewares\" />` del `.csproj`, ya no hace falta con la carpeta poblada.
- **Detalle no obvio, mismo tipo de bug que en §5.3**: `HttpResponse.WriteAsJsonAsync` pisa cualquier `Content-Type` seteado antes si no se le pasa explícito — sin pasarlo, mandaba `application/json` en vez de `application/problem+json`. Corregido pasándolo como parámetro (`contentType: "application/problem+json"`).
- Verificado con un endpoint de prueba temporal (forzaba una excepción, ya removido del código): en Development, 500 con `detail` completo (mensaje + stack trace); corriendo con `ASPNETCORE_ENVIRONMENT=Production` (publish real + env vars, mismo método que §8.1), 500 con el mismo título genérico pero **sin** `detail`; log server-side confirmado en ambos casos; confirmado que un `BadRequest(new ProblemDetails{...})` normal sigue funcionando igual, sin interferencia del middleware.

**Fuera de alcance, mencionado pero no tocado**: no se registró `builder.Services.AddProblemDetails()` (el servicio de .NET 8+ que además estandariza automáticamente los 404/405/415/etc. de routing) — lo implementado es específicamente para excepciones no controladas (500). Si se quiere el mismo formato para esos otros status codes, es un cambio aparte.

---

## 6. Dashboard y UX general

### 6.1 Dashboard — ver §9
✅ Hecho — ver §9.

### 6.2 Selector de idioma (Español/Inglés) en el Header — ✅ Hecho, confirmado
`react-i18next` con diccionarios por namespace. `translateEnum()` desacopla el valor guardado en la DB (español) del texto mostrado. `User.PreferredLanguage` (default `"en"`) persistido vía `PUT /users/me/language`. Sin cambios desde la última revisión — sigue sin nada pendiente en este punto.

---

## 7. Migración de datos del sistema anterior — ✅ Hecho (script implementado y corrido con `--commit` el 2026-07-23)

Se solicitó al responsable del proyecto el archivo de export **completo** (todas las pólizas, todos los tipos en un solo archivo, no separado por tipo). Las 4 preguntas originales quedaron **todas resueltas** por el análisis del archivo real más la respuesta del responsable sobre `Contract identification` (ver §7.2):
1. ~~Si la columna "Members" trae solo cantidad o el detalle completo de cada dependiente.~~ ✅ Resuelta — ver §7.1.
2. ~~Si existe un ID interno para Agentes/Agencias además del nombre.~~ ✅ Resuelta — ver §7.2.
3. ~~Si el export incluye solo pólizas activas o también históricas/canceladas.~~ ✅ Resuelta — ver §7.2.
4. ~~Diccionario de datos para: Reference, Marketplace ID, Contract identification, Renewal status, Confirmed consent.~~ ✅ Resuelta — ver §7.2 (`Contract identification` confirmado por el responsable: texto libre, se migra tal cual).

**✅ Hecho**: script implementado en `WholeCareInsurance.Migration/` (consola .NET, `ProjectReference` a `WholeCareInsurance.api`, EF Core directo sin pasar por la API HTTP). Modos `--dry-run` (simula todo, no persiste, genera reporte) y `--commit --confirm` (real, una transacción por Policy consolidada para poder reintentar sin reprocesar lo ya migrado). Corrido con éxito contra los 4 archivos reales el 2026-07-23: 1185 Health Insurance (ACA) + 7 Medicare + 2 Life Insurance + 16 Supplemental Plans, 0 filas no procesables. Backup previo en `D:\backups\WholeCareInsuranceDb_pre_migracion.bak`. Reporte completo (incluye nombre de Agente original por fila, para el pendiente de abajo) en `WholeCareInsurance.Migration/migration-report-20260723-132722.json`.

**Pendiente no bloqueante**: 2490 filas migradas quedaron con `Customer.AgentId` apuntando al fallback (primer User con Rol=Admin) porque esta base todavía no tiene cargados los ~23 agentes reales del CSV como `User`. Cuando estén cargados, armar un script de reasignación que matchee `Customer.AgentId` por el nombre de agente original (ya está en el reporte JSON, campo `AgentFallbacks`) contra `User.Nombre` — no hace falta re-correr la migración completa para esto.

**✅ Resuelto — formato del export**: confirmado por el responsable que la migración usará **4 archivos separados, uno por tipo de póliza** (Obamacare, Medicare, Life Insurance, Supplemental Plans), no un único archivo combinado con todos los tipos. El archivo ya analizado en §7.1 (1258 filas) es específicamente el de Health Insurance/Obamacare. Los otros 3 (Life, Medicare, Supplemental) ya fueron relevados por estructura de formulario + muestra chica de datos reales (§12), pero **todavía no se analizaron a fondo como el de Obamacare** (que tuvo el análisis completo de 1258 filas) — al diseñar el script, cada archivo probablemente necesite su propia lógica de mapeo/parseo dado que son exports independientes, no necesariamente con las mismas columnas entre sí.

**Columnas detectadas** en la pantalla de export del sistema anterior (referencia para el futuro mapeo): Reference, Agency, Agent, Full name, First/Middle/Last name, DOB, Gender, Email, Phone, Legal Status, SSN, Green card, Work permit, Estado civil, Address 1/2, City, State/Province, Zip code, County, Employer name, Company Phone, Position/Occupation, Annual income, Policy number, Marketplace ID, Contract identification, Number of applicants, Effective date, Company, Insurance plan, Type of plan, Tax Credit/Subsidy, Monthly premium amount, Status, Tags, Period, Confirmed consent, Registration date, Update date, Renewal status, Members.

> Nota: buena parte de estas columnas ya mapean directo a los campos nuevos de Customer (§3.2) y Policy (`Period`/`Number of applicants`, §1.8/§1.9, y ahora también `InsuranceCompany`/`Type of plan`/`Insurance plan`/`Effective date`/`Tax Credit-Subsidy`/`Monthly premium amount`, §1.5/§1.11) — todos cerrados. Ya no hay campos pendientes de agregar antes de diseñar el script de migración.

### 7.1 Hallazgos del análisis del archivo real (Health Insurance/Obamacare, 1258 filas)

- **Detalle completo de dependientes confirmado**: la columna "Members" (y las columnas asociadas por dependiente) sí traen el detalle completo, hasta 8 dependientes por póliza — no solo el conteo. Corrige el supuesto anterior (se pensaba que quizás solo venía la cantidad).
- **`Policy number`, `Marketplace ID` y `Contract identification` no sirven como clave de vinculación**: 90%, 86% y 99% de las filas respectivamente tienen esas columnas vacías. No se puede confiar en ninguna de las tres para vincular el historial de una misma póliza a través de sus duplicados.
- **La reconstrucción de historial va a necesitar una heurística de matching, no un match 100% automático**: SSN + Aseguradora + fecha efectiva cercana, con revisión manual de los casos ambiguos. Caso real confirmado en el archivo: un cliente con 4 registros duplicados, cada uno con un `Reference` distinto, mismo `Effective date`, sin `Policy number` en ninguno de los 4 — nada permite decidir automáticamente si son 4 versiones de la misma póliza o 4 pólizas distintas.
- **SSN tampoco puede ser la única clave**: vacío en ~7% de las filas. No sirve como único criterio ni para detectar duplicados de historial ni para relacionar dependientes ya existentes en `Customer`.
- **Mapeo de "Dependency type"**: "Parent" (sin distinción de género) y "Dependent" (genérico) del archivo de origen mapean ambos a `"Otro"` en `RelacionConPrincipal` — ya cubierto por el enum actual (§1.6), no hace falta agregar valores nuevos.

Estos hallazgos no destraban el bloqueo del todo (ver estado actualizado más abajo), pero ya dejan clara la estrategia de matching a diseñar cuando se arme el script real: heurística + cola de revisión manual, no un mapeo directo por clave única.

### 7.2 Agentes/Agencias, historial completo y diccionario de datos — resuelve las preguntas 2, 3 y 4

**Pregunta 2 — ID interno de Agentes/Agencias (RESUELTA)**: no existe. Tanto `Agent` como `Agency` son campos de texto libre en el archivo.
- `Agency`: solo 2 valores en las 1258 filas — "Preventive Health Insurance" (894 filas) y "Whole Care Insurance Group llC" (364 filas).
- `Agent`: 22 nombres únicos en total.

Bajo riesgo de colisión por nombre dado el volumen chico (22 agentes) — igual conviene revisar manualmente antes de mapear, para detectar posibles duplicados o errores de tipeo, pero esto no bloquea la migración.

**Pregunta 3 — ¿solo activas o también históricas/canceladas? (RESUELTA)**: el archivo incluye **todo el historial**, no solo pólizas activas. Distribución real de `Status` en las 1258 filas: `Processed` (1016), `Updated` (79), `Canceled` (75), `Draft` (61), `To be processed` (21), `In Process` (3), `Agent change` (2), `Pending` (1).

**Pregunta 4 — diccionario de datos (RESUELTA)**:
- `Reference`: identificador único por **registro** (formato "P" + fecha + secuencial, ej. `P15072026018434`) — identifica la versión/registro puntual, no la póliza a través del tiempo. Por eso cada duplicado del historial de una misma póliza tiene un `Reference` distinto (consistente con el hallazgo de §7.1 sobre el caso de las 4 versiones duplicadas).
- `Marketplace ID`: formato consistente con identificadores oficiales del Marketplace de ACA (Plan Year + Estado + código de plan, ej. `PY26 TN SBC 23552TN0020052-06`, a veces solo numérico). Es un dato externo — no se puede validar sin confirmación del responsable, pero el formato observado es coherente con lo esperado.
- `Contract identification`: ✅ **resuelta, confirmado por el responsable** — es un campo de **texto libre** que los agentes completan manualmente, y no siempre lo llenan. Eso explica el formato inconsistente observado (a veces código de plan, ej. `23552TN0020005`; a veces nombre del plan, ej. `Connect Silver-2 3000 Indiv Med Deductible - EPO`; a veces vacío). No tiene estructura fija ni fuente de validación automática detrás. **Decisión para el script de migración**: se migra tal cual viene (texto libre, tolerando vacíos), sin parsearlo ni normalizarlo, y no se usa como clave de matching para nada (el matching de historial ya se resolvió con la heurística de SSN + Aseguradora + fecha efectiva, §7.1).
- `Renewal status` y `Confirmed consent`: sin hallazgos nuevos todavía, quedan para cuando se revise el resto del diccionario.

**Estado actualizado de las 4 preguntas originales**: las 4 quedan resueltas por el análisis del archivo real más la respuesta del responsable sobre `Contract identification`.

**Ya no hay bloqueo activo.** Se migraron los 4 tipos de póliza (Obamacare, Medicare, Life Insurance, Supplemental Plans) desde 4 archivos separados — ver "✅ Hecho" arriba.

**Punto abierto no bloqueante:** cada dependiente en el sistema anterior tiene un campo "Policy number" individual — no está claro su propósito, aclarar con el responsable más adelante (no urgente).

---

## 8. Hosting y despliegue (VPS)

### 8.1 Infraestructura — ✅ Hecho (Dockerfiles + compose + README; despliegue real al VPS sigue pendiente)
VPS ya comprado y corriendo: Ubuntu 24.04, KVM2 (2 CPU, 8GB RAM, 100GB disco), con **EasyPanel** preinstalado.

**Decisiones tomadas:**
- SQL Server como **contenedor** Docker (`mcr.microsoft.com/mssql/server`) — no instalación nativa, por incompatibilidad de Ubuntu 24.04 con SQL Server nativo.
- Un solo contenedor de SQL Server compartido entre test y producción, con 2 bases de datos separadas (`WholeCareInsuranceDb_Test` y `WholeCareInsuranceDb_Prod`) — por limitación de RAM (8GB totales).
- Frontend: `VITE_API_URL` se resuelve vía build-arg por ambiente (no runtime) — cada ambiente reconstruye su propia imagen.
- Migraciones EF Core: auto-migrate al iniciar el contenedor de la API (`dbContext.Database.MigrateAsync()` en `Program.cs`, solo fuera de `Development`).
- Variables de entorno mapeadas: `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenMinutes`, `Cors__AllowedOrigin`, `ASPNETCORE_ENVIRONMENT`, `Brevo__ApiKey`, `Brevo__SenderEmail`, `Frontend__BaseUrl`, `Admin__FirstName`, `Admin__LastName`, `Admin__Email`, `Admin__InitialPassword` (sin `Brevo__ApiKey` seteado, el backend cae a un servicio que solo loguea el email en vez de enviarlo, así que hay que setearlo en Test/Prod para que el flujo de "olvidé mi contraseña" funcione de verdad; sin las 4 de `Admin__*`, `AdminUserSeeder` sigue cayendo al admin default documentado en §10.5 — hay que setearlas antes de un despliegue real).

**Cambios de código implementados:**
- `app.UseHttpsRedirection()` eliminado fuera de Development (evita el redirect loop detrás del proxy de EasyPanel, que termina TLS ahí) + `app.UseForwardedHeaders(...)` agregado (`XForwardedFor` + `XForwardedProto`) para que la app conozca el esquema real de la request original.
- CORS `AllowedOrigin` movido a `Cors:AllowedOrigin` (config/env var), con default `http://localhost:5173` para dev local.
- `Jwt:Key` y `ConnectionStrings:DefaultConnection` vaciados en `appsettings.json` (tracked); los valores reales de dev local se movieron a `appsettings.Development.json` (gitignored, sin cambio de comportamiento en local) — en Test/Prod se inyectan por variable de entorno.
- Auto-migrate agregado en `Program.cs` (`db.Database.MigrateAsync()`, fuera de Development).

**Artefactos nuevos:**
- `WholeCareInsurance.api/Dockerfile` (+ `.dockerignore`) — build multi-stage SDK → `aspnet:9.0`, escucha en `8080`.
- `wholecare-admin-vs/Dockerfile` (+ `.dockerignore`, `nginx.conf`) — build multi-stage Node → `nginx:alpine`, con fallback SPA para React Router.
- `docker-compose.yml` en la raíz — referencia de la topología completa (sqlserver + api-test + api-prod + frontend-test + frontend-prod), con placeholders para contraseñas/dominios/claves.
- `README.md` — nueva sección "Despliegue (VPS / EasyPanel)" con la arquitectura, la tabla de variables de entorno y el detalle del volumen persistente para `App_Data/PolicyDocuments`.

**Verificado sin Docker instalado en esta máquina** (no se pudo levantar los contenedores acá — validar de verdad en el VPS): `npm run build` con `VITE_API_URL` seteado por variable de entorno confirma que el valor queda inlineado en el bundle final (mismo mecanismo que usa el build-arg de Docker); `dotnet publish -c Release` compila sin errores; el binario publicado, corrido con variables de entorno estilo producción (`ASPNETCORE_ENVIRONMENT=Production`, `Jwt__Key`, `ConnectionStrings__DefaultConnection`, `Cors__AllowedOrigin`, sin `Development`), arranca, corre el auto-migrate contra la base de dev sin aplicar nada (ya estaba al día), expone Swagger en 404 (deshabilitado fuera de Development) y responde `Access-Control-Allow-Origin` solo para el origin configurado por env var.

**Pendiente real:** ejecutar esto en el VPS (dar de alta los servicios en EasyPanel, reemplazar los placeholders del compose por secretos reales, configurar dominios/DNS). El build de las imágenes y el arranque real con Docker ya se probaron localmente (ver §8.1.1) — falta específicamente la ejecución en el VPS.

### 8.1.1 Bug de arranque en frío (Error 4060) — healthcheck de SQL Server + orden de migración/seed — ✅ Hecho (2026-07-17)

Primera vez que se levantó el `docker-compose.yml` con Docker de verdad (§8.1 se había verificado sin Docker instalado) — apareció un bug de arranque en frío que no era visible por lectura de código ni por build exitoso, solo corriendo los contenedores reales:

- **Síntoma 1**: el contenedor `api` quedaba en loop de restart — arrancaba antes de que SQL Server terminara de inicializar (SQL Server acepta el puerto TCP antes de estar listo para autenticar conexiones).
- **Fix 1**: `healthcheck` agregado al servicio `sqlserver` (`sqlcmd -Q 'SELECT 1'`, `interval: 10s`, `timeout: 5s`, `retries: 10`, `start_period: 30s`) + `depends_on` de `api-test`/`api-prod` cambiado de la forma corta a `condition: service_healthy`.
- **Síntoma 2** (destapado por el fix 1): con el contenedor `api` ya esperando a que SQL Server esté sano, seguía fallando al arrancar con **Error 4060** ("Cannot open database requested by the login") en un volumen de `sqlserver-data` nuevo (sin bases creadas todavía).
- **Causa**: en `Program.cs`, `AdminUserSeeder.Seed()` corría **antes** que `dbContext.Database.MigrateAsync()` — el seeder consultaba una base que la migración todavía no había creado (`Migrate()` es quien ejecuta `CREATE DATABASE` la primera vez).
- **Fix 2**: se invirtió el orden en `Program.cs` — la migración corre primero, el seeder después. Sin cambios en la condición `!IsDevelopment()` que ya envolvía a la migración (dev local sigue usando `dotnet ef database update` a mano).
- **Verificado con Docker real** (los 5 contenedores del compose — `sqlserver`, `api-test`, `api-prod`, `frontend-test`, `frontend-prod` — levantados sobre un volumen `sqlserver-data` nuevo, arranque en frío): sin loop de restart, `sqlserver` healthy, ambos `api-test`/`api-prod` arriba. Smoke test end-to-end con curl contra los dos ambientes dockerizados: login con el admin seedeado (`200` en ambos, `refreshToken` distinto en cada uno confirma bases separadas), y CRUD completo de Customer contra `api-test` (`POST` → `201`, `GET` por id y por listado → `200`, `DELETE` → `204`, listado post-delete vacío).

### 8.2 Ver §7 para la migración de datos (antes en esta sección, movida para agrupar con el resto de la migración).

---

## 9. Dashboard — ✅ Hecho (2026-07-27)

Las 3 decisiones abiertas de §9.4 (filtros combinables por fecha y agente, Reminders afuera de alcance, sin rol intermedio) quedaron confirmadas por el responsable y son la base del diseño final.

**Hallazgo importante durante el diseño**: no existía ningún scoping por agente en ningún listado de la API (`CustomersController.GetAll()`/`PoliciesController.Search()` devuelven todo sin filtrar por rol — el único uso de `Rol`/`AgentId` hasta este punto era validar qué agente puede asignarse a sí mismo al crear/editar, no filtrar qué puede ver). El Dashboard es el primer lugar del sistema que filtra datos por agente — no había ningún patrón previo que reusar, se diseñó desde cero.

### 9.1 Paleta de colores general — ✅ Hecho
Botones semánticos aplicados en toda la app (no solo Dashboard): Submit/Guardar → verde (`#16a34a`, mismo valor que ya usaban los botones de crear dependiente/beneficiario en `Policies.jsx` antes de esta sesión — se generalizó ese valor en vez de inventar uno nuevo), Editar → amarillo (`#eab308` con texto oscuro `#1f2937` para contraste), Eliminar → rojo (`#dc2626`, ya lo usaba `Customers.jsx`, sin cambios). Aplicado en los formularios de Customers/Agentes/InsuranceCompanies/Policies, los botones de editar de esas mismas 3 páginas, y los 4 submits de las pantallas de auth (Login/ForgotPassword/ResetPassword/ChangePasswordForced) que no tenían color propio. Los botones de icono (🔍✏️🗑 en la tabla de Policies) quedaron sin cambios — son emoji con color fijo, no hay fondo que recolorear.

### 9.2 Referencia visual del Dashboard — ✅ Hecho
KPIs, tarjetas por status y los 2 gráficos de torta (Tipo/Status) implementados. "Recordatorios" se sacó por completo de la fila de KPIs (decisión confirmada: fuera de alcance, sin placeholder).

### 9.3 Estadísticas adicionales — ✅ Hecho (sin "por Cliente")
Por Compañía aseguradora, por Condado, por Ciudad — como listas rankeadas top 10 (condado/ciudad tienen 40-80 valores distintos en los datos reales). "Por Cliente" se sacó del alcance (decisión confirmada, ambiguo y no prioritario).

### 9.4 Decisiones (ya no pendientes)
- Filtros: por rango de fechas (`from`/`to` contra `EffectiveDate`, fuente de verdad de §1.11) **y** por agente, combinables entre sí.
- "Reminders": afuera del alcance, sin implementar ni dejar placeholder.
- Roles: confirmado que solo existen `Admin`/`Agente`, sin rol intermedio — el modelo de permisos de §9.5 se diseñó asumiendo únicamente estos dos.

### 9.5 Alcance de datos según rol — ✅ Hecho
6 endpoints nuevos bajo `api/dashboard/*` (`summary`, `by-status`, `by-type`, `stats`, `latest-policies`, `upcoming-65`). Un único helper (`DashboardController.ResolveEffectiveAgentId`) centraliza el scoping: Admin puede pasar `agentId` (o ninguno = vista global) para "pararse en los zapatos" de un agente puntual; Agente **siempre** recibe su propio `CurrentUserId()`, ignorando cualquier `agentId` que mande en el query string — no se confía en el frontend para esto.
- KPIs "Agencias"/"Agentes" (`DashboardSummaryDto.AgenciesCount`/`AgentsCount`) son `null` en cualquier vista scopeada a un agente (no tienen sentido escalados a una sola persona) — el frontend los oculta cuando vienen en `null`.
- **Verificado el scoping con 2 agentes reales de §15.3** (Ana Ayala Marin #3013 con 386 pólizas/798 miembros, SANDRA AGUILAR #3041 con 133/242, números confirmados por SQL directo independiente del código): un Agente logueado pasando `agentId` de otro agente real, del Admin, o inexistente en la URL a mano siempre recibe sus propios números en los 6 endpoints — el intento de fuga de datos se ignora en el 100% de los casos probados. Admin "parado en los zapatos" de un agente puntual reproduce exactamente los mismos números que ese agente ve logueado directamente.

### 9.6 Widget "Últimas pólizas" — ✅ Hecho
`Policy.UpdatedAt` (`DateTime`, no nullable) nuevo — migración `AddPolicyUpdatedAt` con backfill (`COALESCE(EffectiveDate, GETUTCDATE())` para las 1211 pólizas ya migradas, ninguna quedó en el default de EF). Se setea en `Create` **y** en cada `Update` de `PoliciesController` (no solo en `Update` — una póliza recién creada tiene que aparecer arriba en el widget, no quedar con el default hasta la primera edición). **No se usó `PolicyHistory.ChangedAt`** (solo trackea cambios de `Status`, §13) ni orden por `Id` — `UpdatedAt` es la única fuente de verdad real para "última actualización".

### 9.7 Widget "Próximos/recientes a cumplir 65 años" — ✅ Hecho
Ventana ±4 meses del cumpleaños 65, calculada al vuelo desde `Customer.DateOfBirth` (pre-filtro angosto por año de nacimiento en SQL, chequeo exacto de la ventana en memoria — `AddYears`/`AddMonths` no traduce de forma confiable a SQL vía EF Core). Mismo scoping por rol que el resto (§9.5).

**Verificado**: `dotnet build`/`npm run build`/`npm run lint` limpios (mismos warnings/errores preexistentes de siempre, ninguno nuevo salvo el mismo patrón ya roto de `react-hooks/set-state-in-effect` que ya tenían `Policies.jsx`/`InsuranceCompanies.jsx`). Backend probado extensivamente con curl/node (funcionalidad + los 6 endpoints con el intento de fuga de datos descripto en §9.5). Frontend revisado en el navegador por el responsable directamente (sin extensión de Chrome conectada en esta sesión para captura automática) — confirmado "perfecto" para los 4 escenarios: Admin vista global, Agente logueado, Admin parado en los zapatos de un agente, y filtros de fecha + agente combinados.

**Fuera de alcance, decisión explícita durante el diseño**: el filtro de fecha del Dashboard es independiente del selector global de "Período" del Header (no se integraron) — no estaba pedido explícitamente, se puede sumar después si se necesita.

---

## 10. Gestión de contraseñas — ✅ Hecho

Tres flujos nuevos, ninguno existía antes de esta sesión (no había forced-change, self-service change, ni recuperación por email en el sistema).

### 10.1 Cambio forzado en el primer login
`User.MustChangePassword` (bool) nuevo — se pone en `true` cuando un Admin crea un agente vía `POST /auth/register` (`Agentes.jsx`), y también para el admin seedeado (`AdminUserSeeder`, más una migración de datos que lo fuerza en bases ya existentes, ya que `Admin123!` es una credencial default documentada en este mismo archivo). El login (`AuthResponseDto`) devuelve el flag; el frontend lo persiste en `localStorage` y redirige a `/change-password` (ruta nueva, sin `AppLayout`) antes de dejar entrar a cualquier otra pantalla. `AppLayout` también revisa el flag en su reconciliación de fondo contra `GET /users/me` (mismo mecanismo que ya usaba para el idioma), para cubrir el caso de un Admin que fuerza el cambio sobre una sesión ya activa.
- **✅ Gap cerrado (2026-07-27)**: el gating ya no es solo de frontend. `Middlewares/MustChangePasswordMiddleware.cs` nuevo — consulta `MustChangePassword` contra la base (no contra el JWT, que no lleva ese claim a propósito, ver arriba) en cada request autenticado, y corta con `403` (`ProblemDetails`) antes de llegar al controller si está en `true`. Registrado en `Program.cs` después de `UseAuthorization()` y antes de `MapControllers()`.
  - Exceptuados: `POST /auth/change-password` (el propio endpoint que permite salir del estado), `POST /auth/logout` (para no dejar a alguien sin poder cerrar sesión), `POST /auth/refresh` (no requiere `[Authorize]`, no debe cortarse la renovación del token), y `GET /users/me` — este último crítico: es el mismo endpoint que `AppLayout.jsx` ya usa para detectar el flag en una sesión activa y redirigir a `/change-password`; bloquearlo habría roto ese mecanismo en vez de reforzarlo.
  - Verificado con curl/node contra un usuario real con el flag forzado a `true` temporalmente (restaurado al terminar): `GET /api/policies` → `403`; los 4 exceptuados llegan al controller sin interceptar (`200`/`400`/`401`/`204` según el caso, nunca `403`).

### 10.2 Cambio de contraseña desde el perfil
Un solo endpoint (`POST /auth/change-password`, contraseña actual + nueva) sirve tanto al cambio forzado como al cambio voluntario — nueva página `/profile` (dentro de `AppLayout`), enlazada desde el ítem "Profile" del menú del Header, que antes era un `<div>` sin `onClick` (dead link). Al cambiar la contraseña se limpia el refresh token guardado, forzando el re-login en cualquier otra sesión activa.

### 10.3 Recuperación por email ("Olvidé mi contraseña")
`POST /auth/forgot-password` (público) → `POST /auth/reset-password` (público). Mismo patrón que los refresh tokens (hash SHA-256 + expiración en el propio `User`, sin tabla nueva): `PasswordResetTokenHash`/`PasswordResetTokenExpiresAt`, expiran en 1 hora, de un solo uso.
- **Anti-enumeración**: `forgot-password` devuelve siempre el mismo mensaje genérico, exista o no el email — verificado que ambos casos responden idéntico.
- **Mitigación anti-spam liviana** (sin rate-limiting real, diferido a pedido): si ya hay un token vigente (no vencido), no se genera uno nuevo ni se reenvía el email — verificado que una segunda solicitud dentro de la hora no dispara un segundo email.
- **Envío de emails**: `IEmailService` nuevo, con `BrevoEmailService` (API REST, sin SMTP — evita el bloqueo de puertos SMTP saliente típico de VPS) como implementación real, y `ConsoleEmailService` como fallback automático cuando `Brevo:ApiKey` no está configurado (solo loguea el contenido, usado en dev). Pasar a envío real en Test/Prod es una variable de entorno, no un cambio de código — ver env vars nuevas en §8.1.

### 10.4 Side-fixes incluidos en el mismo batch (pedidos explícitamente)
- El claim `ClaimTypes.NameIdentifier` ahora se emite explícito en el JWT (antes solo se emitía `sub`; funcionaba igual gracias al mapeo por default de `JwtSecurityTokenHandler`, verificado empíricamente con curl antes de tocar nada — no era un bug activo, pero quedaba dependiendo de un comportamiento implícito del framework).
- Los tokens (refresh y reset) ahora se generan con `RandomNumberGenerator` (antes `Guid.NewGuid()` para el refresh token, no es un generador criptográficamente aleatorio).

Verificado íntegramente con curl (wrong/correct current password, refresh token invalidado tras cambiar contraseña, forgot-password con email existente/inexistente devolviendo la misma respuesta, mitigación anti-spam, reset-password con token inválido/válido/reusado, Register fuerza `MustChangePassword`) y con Playwright (cambio forzado end-to-end con redirect, cambio desde perfil, alta de agente nuevo por el Admin → forced change en su primer login, flujo completo de "olvidé mi contraseña" con el link real extraído del log de `ConsoleEmailService`, sin errores de consola salvo los 400 esperados de los casos de error probados a propósito).

### 10.5 AdminUserSeeder generalizado — admin real por ambiente vía variables de entorno — ✅ Hecho
El admin seedeado (§10.1) estaba 100% hardcodeado (`admin@wholecare.com` / `Admin123!` / Nombre "Administrador"). Para Test/Prod hace falta poder seedear un admin real (nombre y email de la persona real, con su propia password inicial) sin tocar código por ambiente.
- `AdminUserSeeder.Seed()` ahora lee `Admin__FirstName`, `Admin__LastName`, `Admin__Email`, `Admin__InitialPassword` (env vars, ver tabla de §8.1). Cada una cae de forma **independiente** al valor hardcodeado de siempre si no está seteada o está en blanco — así el flujo de dev local no cambia si nadie configura nada, pero Test/Prod pueden pisar solo lo que necesiten. `FirstName`/`LastName` se combinan en `Nombre` (`User.cs` no tiene esos campos separados, solo `Nombre`).
- Sigue siendo idempotente por email (no duplica si ese email ya existe) y sigue forzando `MustChangePassword = true` sin importar el origen de la password (default o configurada) — confirmado que este comportamiento no cambió con la generalización.
- Logging nuevo (`ILogger<AdminUserSeeder>`, mismo patrón que `ConsoleEmailService`): `LogWarning` si se usó el fallback completo (nadie configuró nada — útil para detectar en los logs de Test/Prod si alguien se olvidó de setear las variables reales), `LogInformation` si se usó configuración real.
- `appsettings.json` tiene una sección `Admin` nueva con las 4 claves vacías (mismo criterio que `Brevo`). `docker-compose.yml` usa placeholders **genéricos** para estas 4 variables (`admin@tudominio.com`, "Nombre"/"Apellido", password de ejemplo) — los datos reales de cada ambiente van solo en las env vars reales del servicio, nunca en un archivo versionado.
- Verificado contra una base LocalDB descartable (sin tocar la base de dev real): seed default sin config (con el `LogWarning` visible), seed de admin real con config nueva (con `LogInformation`, login funcionando con la password configurada), e idempotencia al reiniciar con la misma config (sin duplicar).

---

## 11. Agentes — campos nuevos en el formulario de creación/edición — ✅ Hecho

Los 18 campos agregados a `Models/User.cs`, `AuthRegisterDto`/`UserUpdateDto`/`UserResponseDto`, migración `20260715133955_AddAgentProfileFields`, y formulario/tarjeta de `Agentes.jsx`:
- `MiddleName` (texto, opcional)
- `Gender` (dropdown Masculino/Femenino, mismo criterio que Customer §3.2 — reusa `GENDERS`/grupo `gender` de `translateEnum`)
- `Address1`/`Address2`, `City`, `ZipCode` (texto — inicialmente opcional, mismo patrón que Customer; ver §11.2, pasaron a obligatorios salvo `Address2`)
- `State`/`County` (dropdowns EE.UU.-only, reusan directamente `src/data/usStates.js` y `usCounties.json` — **decisión confirmada con el responsable**: Country es siempre EE.UU., no se agregó como campo editable; "State/Province" del pedido original es el mismo `State` de 2 letras que ya usa Customer, condado dependiente del estado igual que en Customer)
- `Licensed` (bool, dropdown Sí/No) + `LicenseNumber` (texto, **condicional**: solo visible/habilitado si Licensed = Sí — confirmado con el responsable)
- `NpnNumber` (texto) + `NpnOverride` (bool, checkbox)
- `HasCompanyContract` (bool, dropdown Sí/No "¿Tiene contrato con una compañía?") + `ContractNumber`/`CompanyName` (texto, **condicionales**: solo visibles/habilitados si HasCompanyContract = Sí — confirmado con el responsable)
- `ContractsWanted` (texto, comma-separated — checkboxes múltiples: Medicare, Obamacare, Supplemental Plans, Life Insurance; sin tabla nueva, mismo criterio liviano que `Tags` de Customer; traducido vía grupo `contractInterest` de `translateEnum`)
- `AdditionalInformation` (texto libre/notas, textarea)
- `TermsAccepted` (bool) + `TermsAcceptedAt` (fecha/hora) — **confirmado con el responsable**: obligatorio para guardar (checkbox `required` nativo del navegador, mismo form para alta y edición) y se persiste el timestamp de cuándo se aceptó. Validado también en el backend (`AuthController.Register` rechaza con 400 si `TermsAccepted` no es `true`); en edición (`UsersController.Update`) no se re-exige — si ya era `true` se mantiene, y solo se pisa `TermsAcceptedAt` si pasa de `false` a `true`.

Verificado con curl (alta con los 18 campos, rechazo de registro sin `TermsAccepted`, edición con limpieza de campos condicionales) y con Playwright en español (35/35 checks: los campos nuevos renderizan, License Number/Contract Number/Company Name aparecen y desaparecen según sus dropdowns condicionales, Condado deshabilitado hasta elegir Estado, el submit se bloquea sin marcar el checkbox de términos, alta y edición end-to-end con persistencia correcta, tarjeta de la lista muestra los campos nuevos, sin errores de consola).

### 11.1 Hallazgos de auditoría (2026-07-17) — deuda técnica, no bloqueante — ✅ Resuelto (3/3)

Auditoría puntual del feature de Agentes (§3.1, §3.4, §11) contra el código real, a pedido del responsable. El feature en sí está completo y probado — estos eran gaps menores encontrados en la revisión, no capturados hasta ese momento en este documento:

1. ~~**Falta validación server-side de los pares condicionales.**~~ ✅ Hecho — ver §11.3.
2. ~~**`GET /users` no tiene paginación ni búsqueda**~~ ✅ Hecho — ver §11.4 (búsqueda agregada; paginación real descartada por volumen, ver detalle).
3. ~~**Sin rate limiting en la API**~~ ✅ Hecho — ver §11.4.

### 11.2 Address1/City/ZipCode/State/County pasan a obligatorios + Country fijo — ✅ Hecho (2026-07-17)

Ajuste sobre §11 (no era un gap de la auditoría de §11.1, sino un pedido nuevo del responsable): estos 5 campos ya existían pero eran opcionales — ahora `Address1`, `City`, `ZipCode`, `State` y `County` son obligatorios (`Address2` sigue opcional).

- `Models/User.cs`: los 5 campos pasaron de `string?` a `string` (no nullable).
- Migración `20260717155844_MakeAgentAddressFieldsRequired`: backfill (`UPDATE ... SET <campo> = COALESCE(<campo>, '')`) antes del `ALTER COLUMN ... NOT NULL` — necesario porque ya podía haber agentes reales con estos campos en `NULL` (el feature de §11 estaba en producción desde antes).
- `AuthRegisterDto`/`UserUpdateDto`: `[Required]` agregado a los 5 campos. `UserResponseDto` alineado a no-nullable por consistencia.
- **Country**: campo nuevo, **solo UI, sin persistir** (decisión confirmada con el responsable — el sistema es EE.UU.-only, no aporta información real guardar una constante). Se muestra en el formulario como texto fijo, no editable, traducido (`"Estados Unidos"`/`"United States"` según el idioma activo) — no viaja en el body de `POST /auth/register` ni `PUT /users/{id}`.
- Verificado con curl contra la base de dev real: `POST /auth/register` sin los 5 campos → `400` con los 5 errores de validación; con todos → `200`, alta correcta; `PUT /users/{id}` sin `State` → `400`. Migración aplicada y agente de prueba limpiado de la base al terminar.
- **Verificado en navegador** por el responsable (`npm run dev`, sesión posterior): Country se ve fijo y deshabilitado, traducido correctamente al cambiar de idioma, y los 5 campos bloquean el submit nativo del navegador si están vacíos — cierra la verificación visual que había quedado pendiente.

### 11.3 Validación cruzada server-side de Licensed/HasCompanyContract — ✅ Hecho (2026-07-17)

Cierra el punto 1 de §11.1. `Utils/AgentFieldValidation.cs` (nuevo, mismo patrón que `FileValidationHelper.cs` ya existente en esa carpeta): valida que `Licensed=true` venga con `LicenseNumber`, y que `HasCompanyContract=true` venga con `ContractNumber` y `CompanyName`, devolviendo `400` (`ProblemDetails`, mismo estilo que el chequeo existente de `TermsAccepted`) si falta alguno.

- Llamado desde `AuthController.Register` y `UsersController.Update`, antes de tocar la base.
- Además de validar, **normaliza**: si el flag correspondiente está en `false`, `LicenseNumber`/`ContractNumber`/`CompanyName` se fuerzan a `null` antes de persistir — así un `PUT` que apaga el flag pero no limpia el campo asociado (o un call directo a la API que nunca pasó por la limpieza del frontend) no deja un valor suelto en la base.
- Verificado con curl contra la base de dev real: `Licensed=true` sin `LicenseNumber` → `400`; `HasCompanyContract=true` sin `ContractNumber`/`CompanyName` → `400`; `Licensed=false` con `LicenseNumber` cargado → `200` y se persiste como `null`; alta con ambos pares completos → `200`; edición que apaga `HasCompanyContract` con `ContractNumber`/`CompanyName` todavía en el body → `200` y ambos quedan en `null`. Registros de prueba borrados de la base de dev al terminar.

### 11.4 Búsqueda en `/users` + rate limiting en endpoints sensibles de auth — ✅ Hecho (2026-07-17)

Cierra los puntos 2 y 3 de §11.1. **Decisión confirmada con el responsable** para el punto 2: solo búsqueda, sin paginación real — el volumen actual de agentes no la justifica, y evita romper el contrato de `GET /users` (sigue devolviendo un array plano). Para el punto 3: rate limiting solo en los endpoints públicos/sensibles de `AuthController`, no global — el resto de la API ya está protegida por JWT.

**Búsqueda:**
- `UsersController.GetAll` acepta `?search=` (filtra por `Nombre` o `Email`, case-insensitive, combinable con `?role=` ya existente). Filtra en memoria sobre la lista ya materializada por `UsersService.GetAll()` — sin cambios en la capa de datos.
- `Agentes.jsx`: input de búsqueda + botones Buscar/Limpiar, mismo patrón que los filtros de `Policies.jsx` (`§1.3`) para consistencia visual y de código (`loadUsers(searchOverride)` evita el problema de closure obsoleto al limpiar, igual que `loadData(filterOverrides)` en Policies).

**Rate limiting:**
- Middleware nativo de ASP.NET Core (`Microsoft.AspNetCore.RateLimiting`, sin dependencias nuevas), particionado por IP — `RemoteIpAddress`, que ya viene resuelto vía `X-Forwarded-For` gracias a que `UseRateLimiter()` corre después de `UseForwardedHeaders()` en el pipeline (necesario para que funcione bien detrás del proxy de EasyPanel en producción, §8.1).
- Dos políticas: `LoginPolicy` (10 intentos/min por IP) en `/auth/login`, el blanco clásico de fuerza bruta; `AuthSensitivePolicy` (20/min por IP) en `/auth/register`, `/auth/forgot-password` y `/auth/reset-password`. El resto de `AuthController` (`refresh`, `logout`, `change-password`, ya todos autenticados salvo `refresh`) queda sin límite.
- Respuesta `429` con el mismo formato `ProblemDetails` que el resto de la API (`OnRejected` personalizado en `Program.cs`).
- Verificado con curl real: 12 `POST /auth/login` seguidos (con las credenciales de prueba de esta sesión ya contando contra el límite) → los primeros permitidos hasta completar 10 en la ventana de 1 minuto, el resto `429`; 22 `POST /auth/forgot-password` seguidos → mismo patrón con el límite de 20. `GET /users?search=` sin límite (fuera de las políticas de auth), confirmado indirectamente al no recibir `429` durante las pruebas de búsqueda.

§11.1 queda completamente resuelto (3/3).

---

## 12. Campos específicos por Tipo de Póliza — ✅ Documentado e implementado (Medicare/Life Insurance/Supplemental Plans)

Decisión: dado el bajo volumen actual (Life: 2 registros, Medicare: 7, Supplemental: 16 — vs. 1258 de Obamacare), se documenta el detalle completo de cada tipo para referencia futura, pero SOLO SE IMPLEMENTA cuando el volumen de uso lo justifique. No es trabajo pendiente activo.

### 12.1 Comparativa de campos por tipo (según export CSV)

| Campo | Obamacare (ACA) | Medicare | Life | Supplemental |
|---|---|---|---|---|
| Datos base (nombre, DOB, dirección, condado, etc.) | ✓ | ✓ | ✓ | ✓ |
| Legal Status / Green card / Work permit / Estado civil | ✓ | ✓ | ✗ | ✗ |
| Employer name / Company Phone / Occupation / Annual income | ✓ | ✗ | ✗ | ✗ |
| Policy number / Marketplace ID / Contract identification | ✓ | ✗ | ✗ | ✗ |
| Number of applicants + Dependientes (hasta 8) | ✓ | ✗ | ✗ | ✗ |
| Insurance plan (texto) | ✓ | ✓ (usado) | ✗ | ✓ (usado) |
| Type of plan (Bronze/Silver/etc.) | ✓ (usado) | presente pero siempre vacío en los datos | ✗ | ✗ |
| Tax Credit/Subsidy / Monthly premium amount | ✓ | ✗ (pero SÍ en el formulario real, ver 12.10) | ✗ | ✗ (pero SÍ en el formulario real, ver 12.9) |
| Tags / Confirmed consent / Renewal status | ✓ | ✗ | ✗ | ✗ |

Conclusión: Obamacare es, por lejos, el tipo con más campos y complejidad (subsidios, dependientes, consentimiento) en el CSV. Pero ver 12.11 — el CSV no refleja el formulario completo real de ningún tipo.

### 12.2 HALLAZGO CRÍTICO — El CSV no captura todos los campos del formulario real

Al revisar los formularios reales de creación de Life, Supplemental y Medicare (capturas del sistema anterior), se encontraron campos y secciones completas que NO existen en ninguna columna del export CSV correspondiente.

**Implicancia para la migración:** si se migra solo desde el CSV, esta información se pierde — no está disponible para migrar automáticamente. Si es información valiosa, habrá que preguntarle al responsable si existe otro export/backup que la incluya, o aceptar que se pierde y se recarga manualmente si hace falta a futuro. Esto aplica a los 3 tipos no-ACA (ver 12.11).

### 12.3 Campos adicionales de Customer específicos de Life Insurance — ✅ Hecho
- Age (numérico, campo separado de Date of birth)
- Country of Birth (texto libre — sin catálogo de países en el sistema, mismo criterio que MedicalCorporation en §12.10; se puede migrar a dropdown si se releva la lista real más adelante)
- Height, Weight (texto libre, admite formatos tipo `5'8"`/`180 lb`)
- Checkbox: "Back date to save age?" (`BackDateToSaveAge`)
- Checkbox: "¿Pasó más de 4 meses fuera de EE.UU. en los últimos 12 meses consecutivos?" (`SpentMoreThan4MonthsAbroad`)
- Checkbox: "¿Es miembro de organización militar o pretende serlo?" (`MilitaryOrganizationMember`)
- Are you currently employed? (`CurrentlyEmployed`, bool?, dropdown Sí/No)
- Driver's license (`HasDriverLicense`, checkbox) + License number (`DriverLicenseNumber`, condicional)
- Net Worth, Household income, Household net worth (`NetWorth`/`HouseholdIncome`/`HouseholdNetWorth`, además de Annual income, que ya existe)

Todos opcionales — `Customer` no tiene noción de `Type` de póliza (un mismo Customer puede tener pólizas de varios tipos), así que la condicionalidad por `Type = Life Insurance` se resuelve enteramente en el frontend, no en el modelo. Dos lugares de edición en `Policies.jsx`:
- **"crear dependiente nuevo"**: los 13 campos se agregan a `CustomerFormFields` (componente compartido con `Customers.jsx`) detrás de un prop `showLifeInsuranceFields`, que solo se pasa en `true` desde el flujo de dependientes de Policies.jsx cuando `Type = Life Insurance`. `Customers.jsx` nunca los muestra.
- **"Datos Life Insurance del titular"** (nueva, decisión de la sesión): el titular de la póliza se elige por dropdown de Customers existentes, no se crea/edita inline como los dependientes — se agregó una sección propia en `Policies.jsx`, visible cuando `Type = Life Insurance` y hay un titular seleccionado, que precarga los 13 campos del Customer elegido y los guarda con un botón propio vía `PUT /api/customers/{id}` (reenviando el objeto completo, igual criterio que `Customers.jsx`, ya que `CustomerUpdateDto` no admite parcial).

Los 13 campos, en ambos lugares, se renderizan desde un componente compartido nuevo `LifeInsuranceFields.jsx` (extraído para no duplicar el JSX entre `CustomerFormFields` y la sección del titular).

### 12.4 Aseguradoras específicas de Life Insurance detectadas en el dropdown del formulario
Aetna, AIG, American Amicable, Americo, Columbian Financial Group, Fidelity, Forester, Great Western, Mutual Of Omaha, Mutual Trust Life, National Life Group, Prosperity, Senior Life, Transamerica — PENDIENTE de confirmar si la lista está completa (se cortó por el scroll de la captura, no confirmado si sigue después de Transamerica).

### 12.5 Aseguradoras nuevas a agregar al catálogo InsuranceCompanies (confirmadas por datos reales de los 3 CSV, no específicas de un tipo — agregar cuando se decida sembrar estas también)
- Medicare: Devoted, Health Sun
- Life: AIG, National Life Group
(Aetna y United ya estaban en el catálogo de 31 sembrado con Obamacare)

### 12.6 Campos de Life Insurance no capturados en el CSV, solo vistos en el formulario real — ✅ Hecho
- **Beneficiarios**: entidad nueva `PolicyBeneficiary` (FK a Policy, sin vínculo con `Customer` — a diferencia de Dependientes, son datos propios del beneficiario, no requieren que exista como cliente del sistema). Campos: `TypeOfRelationship`, `FirstName`, `LastName`, `DateOfBirth`, `Gender`, `Phone`, `Email`, `SocialSecurityNumber`. Endpoints `GET/POST /api/policies/{id}/beneficiaries`, `DELETE /api/policies/{id}/beneficiaries/{beneficiaryId}` (sin PUT, solo alta/baja — mismo criterio que `PolicyDocument`). Sección "Beneficiarios" en `Policies.jsx`, visible con `Type = Life Insurance`, mismo patrón visual que Dependientes ("+ New"/"Remove").
- Coverage: `AdditionalOrAlternatePolicy` (bool?) + `AdditionalOrAlternatePolicyDetail` (texto, condicional), `UnderwritingRequirements` (texto), `NeedsMedicalRequirements` (bool?, checkbox con nota de intérprete/traductor)
- Premium Information: `BillingType`, `PremiumFrequency`, `PlannedPeriodicModalPremium`, `SourceOfFunds`
- Existing Insurance - Primary Insured: `HasExistingLifeInsurance`, `IsReplacingExistingPolicy`, `UsingFundsFromInforcePolicy`, `ProvideComparativeInfoForm` (4 checkboxes)
- Notice and Consent - Primary Insured: `PhysicianName`, `PhysicianAddress`
- Extras: `AdditionalInformation` (textarea simple, no hay editor rich-text en el proyecto — se verificó que no existía ya en `Policy`, solo en `User`/Agente), `ConsentSigned` (checkbox simple, sin relación con firma digital real — el ítem 19 del roadmap, firma digital de consentimiento, sigue bloqueado hasta elegir proveedor)

Todos opcionales — `Type` (§1.1) también cubre Obama Care/Medicare/Auto/Otro, que no los usan. Se agregó `Life Insurance` a los `AllowedValues` de `Type` (antes: `Obama Care`, `Medicare`, `Auto`, `Otro`). Migración `AddLifeInsuranceFields` (mismo diff que §12.3, EF Core no permite separar en dos migraciones cuando ambos cambios de modelo ya están hechos). Los 16 campos se muestran en `Policies.jsx` solo cuando `Type = Life Insurance`, mismo mecanismo condicional ya usado para Medicare (§12.10).

Verificado con curl (alta/edición de Customer con los 13 campos de Life Insurance, alta de Policy Life Insurance con los 16 campos, alta/listado/baja de beneficiarios) y `dotnet build`/`npm run build`/`npm run lint` sin errores nuevos.

### 12.7 HALLAZGO — Supplemental tiene subtipos/productos específicos por aseguradora
El menú de creación de "Policies Supplemental Plans" no es un tipo homogéneo — ofrece productos concretos: Cigna Dental, Cigna Accidental, Cigna Cancer Stroke, Cigna Choice Hospital, Sure Bridge. Pendiente confirmar con el responsable si cada producto tiene variantes de formulario entre sí, o si el formulario relevado (Cigna Dental) es representativo de todos.

### 12.8 Principio de diseño confirmado — una sola forma de cargar aplicantes/dependientes
Se confirma (no es una decisión nueva, ya estaba definida en §2) que la forma de agregar un aplicante/dependiente a una póliza debe ser ÚNICA en el sistema nuevo, sin importar el Type de póliza. El sistema anterior varía esto por tipo de póliza (ej. en Supplemental el formulario de aplicante es distinto al de Obamacare); en el sistema nuevo NO se debe replicar esa inconsistencia — siempre el mismo flujo (buscar Customer existente o crear uno nuevo con ficha completa).

### 12.9 Campos específicos de Supplemental Plans — ✅ Hecho
- Insurance: Effective date, Company, Insurance plan, Monthly premium amount → ya existían en `Policy` (§1.5/§1.11), reusados tal cual, sin duplicar.
- Cobertura anterior: `HasExistingDentalCoverage`, `EligibleForMedicare`, `IsReplacingDentalCoverage` (3 checkboxes, bool?).
- Datos bancarios: `InsuredPaysThePremium` (checkbox), `BankAccountType` (dropdown Cheque/Ahorros), `RoutingNumber`, `AccountNumber`, `InsuredIsAccountHolder` (checkbox), `AuthorizedAutomaticPayment` (checkbox), `AutoPaymentDay` (dropdown 1-28, evita 29-31 por meses cortos).
- HIPAA y Autorización de Mercadeo: `AuthorizeMarketingInfo` (checkbox), `RepresentativeName`, `RepresentativeRelationship`.

**⚠️ Decisión explícita de riesgo — `RoutingNumber`/`AccountNumber` SIN cifrado en reposo.** Se evaluó y se decidió NO implementar cifrado a nivel de base de datos para estos dos campos (quedan como `nvarchar` planos en `Policies`). Riesgo aceptado explícitamente por el responsable, documentado acá para que quede como decisión registrada, no como un descuido. Mitigador aplicado (no reemplaza cifrado real): en el frontend, ambos campos usan `MaskedInput` — arrancan ocultos (`type="password"`) con un botón de mostrar/ocultar, igual que el resto de los campos sensibles del sistema (ver retrofit de SSN más abajo). Reconsiderar cifrado en reposo si este tipo de póliza crece en volumen o si se audita cumplimiento (PCI/HIPAA).

**Retrofit de masking a SSN (no estaba documentado antes, se agrega ahora):** al no existir ningún patrón previo de ocultar/mostrar en el sistema (ni siquiera en los campos de password), se diseñaron dos componentes nuevos — `MaskedInput.jsx` (input editable) y `MaskedText.jsx` (solo lectura), con un helper compartido `maskValue()` (últimos 4 caracteres visibles). Se aplicaron a `RoutingNumber`/`AccountNumber` (nuevo) y se retrofiteó a `SocialSecurityNumber` en: `CustomerFormFields.jsx` (input), tarjeta de `Customers.jsx`, detalle de titular y listado de dependientes en `Policies.jsx`, y el input de SSN del formulario de alta de beneficiario. **Excepción, limitación de HTML**: el SSN que aparece dentro del `<option>` del dropdown nativo de titular no admite componentes interactivos — se resolvió con enmascarado estático (últimos 4 dígitos visibles siempre, sin botón), usando el mismo helper `maskValue()`.

### 12.10 Campos específicos de Medicare — ✅ Hecho
No capturados en el CSV export:
- Monthly premium amount (reusa el campo ya existente en `Policy`, §1.11 — no se duplicó)
- Do you have Medicaid? (`HasMedicaid`, bool?, dropdown Sí/No)
- Medicaid level (`MedicaidLevel`, texto)
- Referred to medical corporation? (`ReferredToMedicalCorporation`, bool?, dropdown Sí/No)
- Medical corporation (`MedicalCorporation`, texto libre — no existe catálogo de "medical corporations" en el sistema, a diferencia de `InsuranceCompany`; si se releva la lista real más adelante se puede migrar a dropdown/tabla propia igual que se hizo con aseguradoras)

Todos opcionales a nivel de modelo (`Type` también cubre Obama Care/Auto/Otro, que no los usan). En el formulario de `Policies.jsx` se muestran solo cuando `Type = Medicare` (primer caso de un campo condicionado por `Type`; mismo criterio a futuro para Life/Supplemental si se implementan). Migración `AddMedicarePolicyFieldsAndRenameSaludToMedicare` (misma migración que el rename de §1.1).

Sin sección de dependientes/beneficiarios — consistente con el CSV.

### 12.11 Confirmado — patrón repetido en los 3 tipos: el CSV no refleja el formulario completo
Se confirmó el mismo gap en los 3 tipos relevados (Life, Supplemental, Medicare): cada uno tiene campos reales en el formulario de creación que NO aparecen en ninguna columna del export CSV correspondiente. Antes de diseñar el script de migración, hay que asumir que estos campos adicionales (financieros, de elegibilidad, bancarios, consentimiento) se van a perder en la migración automática salvo que el responsable confirme que existe otra fuente de datos que sí los incluya.

### 12.12 Relevamiento de formularios por tipo — COMPLETO
Los 4 tipos de póliza fueron relevados (Obamacare a fondo con datos reales de 1258 filas; Medicare, Life y Supplemental por estructura de formulario + muestra chica de datos reales). Auto: sin datos cargados, sin formulario relevado todavía — pendiente si en algún momento se necesita.

~~Todo lo de §12 sigue en estado "documentado, no implementar" hasta que el volumen de uso de estos 3 tipos lo justifique~~ — **actualización**: el responsable pidió adelantar los 3 (decisión tomada), y quedaron implementados (§12.3, §12.6, §12.9, §12.10).

---

## 13. Historial/Auditoría de Pólizas — ✅ Hecho

Prerequisito para el script de migración (§7): necesita poder insertar snapshots históricos de una póliza reconstruidos desde los registros duplicados del sistema viejo, sin pasar por el flujo normal de "usuario logueado hace un cambio".

- Entidad nueva `PolicyHistory`: `PolicyId` (FK, cascade), `FieldChanged`, `OldValue`/`NewValue` (nullable), `ChangedAt`, `ChangedByUserId` (FK a `User`, **nullable a propósito** — la carga desde migración no tiene usuario real), `Source` (`"Sistema"` | `"Migración"`).
- **Alcance del tracking automático: solo `Status`** (no genérico para todos los campos de `Policy` — decisión explícita, más simple de auditar/revisar). Se registra en dos momentos:
  - **Alta** (`POST /api/policies`): una entrada con `OldValue = null`, `NewValue = <status inicial>`.
  - **Edición** (`PUT /api/policies/{id}`): una entrada solo si `Status` cambió (comparación antes/después de aplicar el DTO); si no cambió, no se agrega nada.
  - En ambos casos, `ChangedByUserId` = usuario logueado (`CurrentUserId()`, mismo patrón que `CustomersController`), `Source = "Sistema"`.
- Endpoint `GET /api/policies/{id}/history` — línea de tiempo completa, ordenada por `ChangedAt` descendente (más reciente primero).
- Servicio nuevo `IPolicyHistoryService`/`PolicyHistoryService` (separado de `IPolicyService`), con método `AddBulk(IEnumerable<PolicyHistory>)` **sin endpoint HTTP** — pensado para que el futuro script de migración lo invoque directamente. Verificado con un insert directo por SQL simulando ese caso (`ChangedByUserId = NULL`, `Source = 'Migración'`): el schema lo acepta sin problemas de FK, y el endpoint `GET /history` lo devuelve correctamente (`changedByUserName: null`).
- Migración `AddPolicyHistory`.
- Frontend: sección "Historial" en el modal de detalle de `Policies.jsx` (junto a Dependientes/Beneficiarios/Documentos), lista cronológica descendente con fecha, campo (traducido reusando `form.fields.*` de `policies.json` cuando existe la clave, con fallback al nombre del campo), valor anterior → valor nuevo (traducidos vía `translateEnum("policyStatus", ...)` para `Status`), y quién (nombre del usuario, o "Migración" si `Source === "Migración"`).

Verificado con curl: alta genera 1 entrada, edición con cambio de `Status` genera una 2da entrada, edición sin cambio de `Status` no agrega nada, y borrar la póliza borra en cascada su historial.

---

## 14. Paginado en Policies — ✅ Hecho (2026-07-27)

Con el volumen real post-migración (1211 pólizas), la vista de Policies necesitaba paginado — cargaba todo sin límite, generando scroll interminable.

- `GET /api/policies` ahora devuelve `PagedResponseDto<PolicyResponseDto>` (`Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`) en vez de un array plano — **cambio incompatible de forma de respuesta**, actualizado en el mismo cambio en `Policies.jsx` (único consumidor, confirmado por grep antes de implementar).
- `PolicyService.Search` acepta `page`/`pageSize`, ordena por `Id DESC`, clampea `pageSize` a un máximo de 100 server-side. `PoliciesController.GetAll` acepta `?page=`, `pageSize` fijo en `DefaultPageSize=20` (constante, no expuesto al usuario todavía).
- `IPolicyService.GetAll()` (usado por `CustomersController.GetPoliciesForCustomer`) no se tocó, como estaba previsto.
- Frontend: estado `page`/`totalCount`/`totalPages`, controles Anterior/Siguiente + "Página P de N" + "Mostrando X-Y de Z", reset a página 1 en `handleSearch`/`handleClearFilters`/cambio de `period` (los demás call sites de `loadData()` sin argumentos preservan la página actual por diseño). i18n nuevo (`pagination.previous/next/pageInfo/showing`, es/en).
- Verificado con curl real contra la base de dev: `totalCount: 1211`, `totalPages: 61`, orden `Id DESC` sin gaps entre páginas, filtro combinado (`type=Life Insurance`) trae 2 de 2 en 1 sola página, `page=0` clampeado a 1, página fuera de rango devuelve array vacío sin error. No se pudo verificar el click de los botones en un navegador real (sin extensión de browser conectada en esta sesión).

---

## 15. Agentes reales del sistema anterior — Agency + importación — ✅ Hecho (2026-07-27)

### 15.1 Campo Agency en Agente — ✅ Hecho

`User.Agency` (`string?`, `nvarchar(150)`), migración `AddUserAgency` aplicada, configurado en `UserConfiguration.cs` (`HasMaxLength(150)`, mismo patrón que el resto de los campos de perfil de Agente).

- **Tipo de dato confirmado con el responsable**: `[AllowedValues]` con los 2 valores reales del archivo (`"Whole Care Insurance Group llC"`, `"Preventive Health Insurance"`), verificados sin variantes de tipeo (dump directo del `.xlsx`, 41 filas). Patrón nuevo en este código: como el campo es opcional, se incluyó `null` explícito en la lista de valores permitidos (`[AllowedValues(null, "Whole Care Insurance Group llC", "Preventive Health Insurance")]`) — los campos opcionales existentes (`Gender`, etc.) evitaban `AllowedValues` por completo porque la implementación de .NET rechaza `null` si no está en la lista; acá se probó explícitamente que incluirlo funciona.
- Frontend: dropdown en `Agentes.jsx` (junto a Género), mostrado también en la tarjeta de listado. i18n es/en completo (`form.fields.agency`, `card.agency`, grupo `agency` en `enums.json`).
- Verificado con curl: valor válido → `201`; sin `Agency` → `201` con `null`; valor inventado → `400` (`"Agency":["Agencia inválida."]`).

### 15.2 Importar los 41 agentes reales como Users — ✅ Hecho

`AgentImporter.cs` nuevo (standalone, sin `ImportPipeline`/`EntityMatcher` — no hay Customer/Policy involucrados, mapeo directo fila → `User`). `Program.cs` extendido: `FindFile(sourceDir, "agent")` detecta `report-agent-agent-index-kUiLdU.xlsx`, se corre **antes** que las pólizas en el mismo `--dry-run`/`--commit` (para que un futuro run combinado ya resuelva agentes reales en vez de fallback).

- Nombre = First name + Last name. Rol="Admin" para `admin@wholecareinsurancellc.com` (Alejandra Díaz Cortez) y `alexfinancial22@gmail.com` (Alexander Centeno) — confirmado. Resto (39) → Rol="Agente".
- Address1/City/ZipCode/State/County en `""` (placeholder vacío, mismo criterio que el backfill de §11.2). MiddleName/Gender/Licensed/NpnNumber/HasCompanyContract/ContractsWanted/AdditionalInformation/IsEncargado: null/false.
- **TermsAccepted = `false`, sin fecha** (decisión confirmada): no hay evidencia de que estos 41 agentes hayan aceptado los términos nuevos del sistema — asentar `true` habría sido incorrecto. No bloquea nada (el login no chequea este flag). `MustChangePassword = true`.
- Password temporal única generada con `RandomNumberGenerator` (mismo criterio que refresh/reset tokens, §10.4), mostrada una sola vez en consola/reporte, nunca persistida en un archivo versionado en git. Idempotente por Email.
- Verificado con `--dry-run` y `--commit --confirm` reales contra la base de dev: 41/41 creados, 0 colisiones de email o nombre contra los Users existentes, 2 Admin + 39 Agente correctos, `Agency` poblada 20/21 como el archivo real, `MustChangePassword=1` solo en los 41 nuevos, pólizas sin tocar (1211 antes y después).

### 15.3 Reasignación de agentes en pólizas ya migradas — ✅ Hecho

Nuevo modo `--reassign-agents` en `WholeCareInsurance.Migration`, combinable con `--dry-run`/`--commit --confirm`.

- Reutiliza `EntityMatcher` con **dos métodos nuevos de solo lectura** (`TryFindExistingCustomerId`, `TryFindExistingAgentId`) que nunca crean nada — a diferencia de los métodos originales de la migración inicial (`ResolveCustomerAsync` sí crea un Customer nuevo si no matchea). Evita el riesgo de crear Customers fantasma al releer los 4 `.xlsx` solo para reasignar.
- **Corrección sobre el plan original**: el reporte `migration-report-20260723-132722.json` (campo `AgentFallbacks`) **no alcanza solo** para la reasignación — sus entradas guardan `SourceFile`/`SourceRow`/`OriginalAgentName` pero no el `CustomerId` resuelto (la resolución del Customer pasa *después* de resolver el agente en el pipeline original). Por eso hizo falta releer los 4 archivos y re-resolver el Customer con el mismo criterio determinístico (SSN, luego Nombre+Apellido+FechaNacimiento), no solo cruzar el JSON contra `User.Nombre`.
- Criterio de "agente vigente" por Customer: la fila con `Update date` más reciente entre todas sus apariciones en los 4 archivos combinados (simplificación consciente frente a `PolicyGrouper` — `AgentId` vive en `Customer`, no en `Policy`, no hace falta reconstruir la identidad exacta de cada póliza).
- Salvaguarda: solo reasigna si `Customer.AgentId` sigue siendo el fallback Admin al momento de correr (no pisa reasignaciones manuales previas).
- Verificado con `--dry-run` y `--commit --confirm` reales, resultados idénticos entre ambos: **1178 de 1179 Customers reasignados** (99.92%), 0 sin match de agente, 1 caso residual (`Customer #21386`, no matcheó en la re-lectura aunque sí lo había hecho la migración original — no representa riesgo, queda igual que antes, sin dato incorrecto), pólizas sin tocar.

---

## 16. Modal/Dialog reutilizable para crear/editar — ✅ Hecho (2026-07-27)

### 16.1 Problema reportado

El formulario de crear/editar se monta **inline sin backdrop**, empujando el listado hacia abajo en vez de superponerse limpiamente — reportado primero en `Policies.jsx` al usar "Editar", visualmente confuso (da la impresión de que quedan dos cosas montadas una debajo de la otra).

**Confirmado el mismo patrón exacto en 4 pantallas**: un botón "+ Nuevo X" togglea el estado `showForm`, y `{showForm && <div>...}` se renderiza en el flujo normal de la página (sin `position: fixed` ni backdrop). El mismo bloque de formulario se reusa para crear y editar (`editingId ? titleEdit : titleCreate`).
- `Policies.jsx` — confirmado, la pantalla más usada.
- `Customers.jsx` — mismo patrón exacto.
- `Agentes.jsx` — mismo patrón exacto.
- `InsuranceCompanies.jsx` — mismo patrón exacto.

No aplica a `Profile.jsx`, `Dashboard.jsx` ni las pantallas de auth (`Login`/`ForgotPassword`/`ResetPassword`/`ChangePasswordForced`) — no tienen flujo de listado+formulario.

### 16.2 Ya existe un modal real, pero aislado a una sola vista

`Policies.jsx` tiene un modal real ya implementado — el de "Ver detalle" (🔍): `position: fixed`, backdrop `rgba(0,0,0,0.5)`, centrado con flexbox. **No se reusó para crear/editar** — quedó como implementación aislada, inline en esa misma página, no como componente aparte.

### 16.3 No existe ningún componente Modal/Dialog reutilizable

Buscado en todo `src/` y en `package.json`: sin shadcn/ui, Radix, Headless UI ni ninguna librería de UI de terceros. Los componentes compartidos existentes (`CustomerFormFields.jsx`, `LifeInsuranceFields.jsx`, `MaskedInput.jsx`, `MaskedText.jsx`) son todos campos de formulario, ninguno de layout/overlay.

### 16.4 Approach aprobado

Crear `src/components/Modal.jsx` genérico, basado en el estilo visual del modal de detalle ya existente en `Policies.jsx` (mismos valores de backdrop/posicionamiento, para no introducir un estilo visual nuevo), sumando lo que hoy falta:
- Backdrop con click-outside para cerrar (el modal de detalle ya lo tiene).
- **Nuevo**: cierre con Escape.
- **Nuevo**: scroll lock del `body` mientras está abierto.
- **Nuevo**: foco atrapado dentro del modal (Tab/Shift+Tab no se escapan) + foco inicial al primer elemento enfocable + restauración del foco al elemento que abrió el modal, al cerrar.
- `role="dialog"` + `aria-modal="true"`.

**API mínima**: `<Modal open={showForm} onClose={() => setShowForm(false)}>{/* contenido actual del formulario */}</Modal>` — cada pantalla solo cambia el wrapper, no la lógica interna del formulario (estado, validación, submit siguen igual).

### 16.5 Orden de migración aprobado

1. Crear `Modal.jsx`.
2. Migrar `Policies.jsx` (crear/editar) — el caso confirmado, pantalla con más tráfico.
3. Migrar `Customers.jsx`.
4. Migrar `Agentes.jsx`.
5. Migrar `InsuranceCompanies.jsx`.
6. Refactorizar el modal de detalle ya existente de `Policies.jsx` (el de la lupa 🔍) para que también use el `Modal` compartido en vez de su propia implementación duplicada — una sola fuente de verdad, y de paso hereda Escape/foco atrapado que hoy no tiene.

### 16.6 Verificación pendiente al implementar

Probar **crear y editar** en cada pantalla recién migrada antes de pasar a la siguiente (no migrar las 4 y probar todo junto al final). Al terminar las 6, correr `dotnet build` (si aplica) + `npm run build` + `npm run lint` una vez más antes de dar el trabajo por cerrado.

### 16.7 Implementación — las 6 migraciones del plan, tal como se aprobaron

`src/components/Modal.jsx` nuevo: backdrop `rgba(0,0,0,0.5)` fixed+centrado (mismo estilo que el modal de detalle viejo), click-outside, Escape, scroll lock del `body`, foco atrapado (Tab/Shift+Tab no se escapan del modal), foco inicial al primer elemento enfocable y restauración al elemento que abrió el modal al cerrar, `role="dialog"`/`aria-modal="true"`. API: `<Modal open onClose maxWidth>{children}</Modal>`.

Las 6 migraciones se hicieron en el orden aprobado (§16.5), probando crear y editar en cada pantalla antes de pasar a la siguiente: `Policies.jsx` (form crear/editar), `Customers.jsx`, `Agentes.jsx`, `InsuranceCompanies.jsx`, y por último el modal de detalle de `Policies.jsx` (🔍) refactorizado para usar el mismo `Modal` compartido en vez de su implementación duplicada — verificado por el responsable en las 5 pantallas.

**Dos bugs encontrados y corregidos en el camino (ninguno bloqueó el plan, pero valen como hallazgo):**
- **Bug propio del `Modal`, encontrado antes de reportar terminado**: `onClose` se pasa como arrow function inline (`() => setShowForm(false)`), con identidad nueva en cada render del padre. Estaba en el array de dependencias del `useEffect` de foco, así que ese efecto se re-ejecutaba en cada tecleo dentro del formulario — robando el foco de vuelta al primer campo del modal en cada cambio de estado. Fix: `onClose` se guarda en un `ref` (actualizado en un `useEffect` sin deps, no durante el render, para no violar la regla de lint de no mutar refs en render) y el efecto de foco pasa a depender solo de `[open]`.
- **Bug preexistente, no relacionado al Modal, encontrado por el responsable al probar la edición**: `handleSubmit` de `Policies.jsx` validaba campos requeridos con `!premium`, que es `true` cuando `Premium` vale `0` — un valor legítimo y frecuente en pólizas migradas (281 pólizas reales con `Premium=0`, documentado en §1.11). Cualquier edición de esas pólizas disparaba un falso "todos los campos son obligatorios" sin importar qué campo se cambiara. Fix: la validación pasó a comparar explícitamente contra `"" `/`null`/`undefined` en vez de usar falsy.

**Ajuste visual pedido por el responsable tras probar el modal de detalle**: el contenido (lista de pares etiqueta:valor apilados) se sentía apretado y poco jerárquico a 500px de ancho. Se mantuvo el ancho (opción elegida explícitamente sobre la alternativa de grid a 2 columnas más ancho) y se sumó más aire entre líneas (`detailRowStyle`, `margin: "7px 0"`, antes `"2px 0"`) y encabezados de sección con más jerarquía visual (`sectionHeaderStyle`: uppercase, letter-spacing, borde inferior, color gris) — aplicado de forma mecánica con `replace_all` sobre los 51 `<p>` y 7 `<h4>` de sección del modal de detalle (patrones idénticos, únicos en ese bloque del archivo), sin tocar el formulario de crear/editar.

Verificado: `dotnet build` (0 warnings/0 errors) + `npm run build` + `npm run lint` (mismos 6 problemas preexistentes de siempre — `react-hooks/set-state-in-effect` en `Policies/Customers/Agentes/InsuranceCompanies/Dashboard`, ninguno nuevo) tras cada paso. Probado en navegador por el responsable: crear/editar en las 4 pantallas, Escape/click-afuera/✕ en el modal de detalle, y el fix de `Premium=0` guardando cambios en una póliza real migrada con ese valor.

---

## 17. Unificación de listados (Customers/Agentes) al estilo tabla de Policies + paginado en los 3 — ✅ Hecho (2026-07-27)

Con el Modal compartido ya migrado (§16), el responsable pidió unificar el estilo visual de Customers.jsx/Agentes.jsx (tarjetas con 8-20 campos apilados) al de Policies.jsx (tabla con columnas fijas, acciones en íconos).

### 17.1 Aclaraciones previas a implementar

- **El ícono 💬 de Policies es WhatsApp click-to-chat** (§4.2 ya documentado) — patrón genérico, extendido a Customers.
- **"Compañía asociada" en Agentes eran 2 campos distintos** (`Agency`, agencia interna de 2 valores fijos; `CompanyName`, contrato externo condicional a `HasCompanyContract`) — se muestran como 2 columnas separadas, decisión del responsable.
- **`User` no tenía campo `Phone`** — se descartó agregarlo por ahora, columna de Teléfono fuera del alcance en Agentes.
- **Agentes.jsx solo tenía "Editar"** — no había "Eliminar" (sin endpoint `DELETE` en el backend) ni "Detalle". El responsable pidió agregar las 3 acciones.
- **Hallazgo de diseño clave**: `User` tiene 3 FKs `Restrict` apuntando a él (`Customer.AgentId`/`AssistantAgentId`/`RecordAgentId`) más `PolicyHistory.ChangedByUserId` — un `DELETE` real fallaría siempre contra agentes reales (todos con `PolicyHistory`/`Customer` asociado). Se implementó **baja lógica** (`User.IsActive`, mismo patrón que `InsuranceCompany.IsActive`, §1.5) en vez de `DELETE`.

### 17.2 Backend — `User.IsActive`

`User.IsActive` (bool, default `true`) + migración `AddUserIsActive` (`defaultValue: true`, ajustado a mano porque EF Core lo generó en `false` por defecto — hubiera desactivado a los 41 agentes reales ya migrados). Persistido vía el `PUT /users/{id}` existente (`UserUpdateDto`/`UserResponseDto` con `IsActive`) — sin endpoint nuevo. **Detalle no obvio**: `Agentes.jsx` ahora envía `isActive` explícito en cada `PUT` de edición (antes no lo mandaba) — si no se manda, `UserUpdateDto.IsActive` cae a su default `true` y reactivaría por accidente a un agente desactivado con cualquier edición no relacionada.

### 17.3 Backend — paginado en Customers/Users, `pageSize=10` en los 3

**Problema de diseño**: a diferencia de `GET /api/policies` (§14, un solo consumidor), `GET /api/customers` y `GET /users` tienen múltiples consumidores — las pantallas de administración (que ahora quieren paginado) y varios dropdowns que necesitan la lista completa sin paginar (selector de dependientes/titular en Policies.jsx, selector de Agente en Customers.jsx/Policies.jsx/Dashboard.jsx). Cambiar la forma de la respuesta sin condición habría roto esos dropdowns.

**Solución**: `page` query param **opcional** en ambos endpoints — sin `page` (comportamiento de siempre) devuelven el array plano completo; con `page`, devuelven `PagedResponseDto<T>` (reusado de `DTOs/Policies/`, es genérico pese al namespace). `ICustomerService.Search(page, pageSize)` nuevo (`GetAll()` intacto). `UsersController.GetAll` pagina en memoria sobre la lista ya filtrada por `role`/`search` (mismo criterio que ya tenía el filtro de búsqueda de §11.4) — actualiza la decisión de §11.1/§11.4 que había descartado paginar por bajo volumen, ahora el responsable lo pidió explícitamente. Orden en ambos: `Id` descendente (más reciente primero, sin campo `CreatedAt` en ninguna de las dos entidades).

`PoliciesController.DefaultPageSize` bajó de `20` a `10` (pedido explícito: "últimas 10" en los 3 listados).

Verificado con curl: `GET /api/customers?page=1`/`GET /users?page=1` devuelven `PagedResponseDto` con `pageSize:10`; `GET /api/customers`/`GET /users?role=Agente` sin `page` siguen devolviendo array plano (confirmado que los dropdowns no se rompieron); paginado combinado con el buscador existente de Agentes; página 2 trae los siguientes 10 registros sin solapar.

### 17.4 Frontend — Customers.jsx

Tabla: Nombre completo, Tipo de residencia (`migrationStatus`), Teléfono, Email. Acciones en una línea: ✏️ Editar, 🔍 Detalle (modal nuevo con **todos** los campos que antes estaban en la tarjeta, agrupados en secciones: Datos personales, Contacto y dirección, Datos laborales, Otros, Agentes, Pólizas), 💬 WhatsApp (directo, sin búsqueda — a diferencia de Policies no hace falta resolver el teléfono vía otra entidad), 🗑 Eliminar (ya existía). Paginado con los mismos controles Anterior/Siguiente/Página P de N que Policies (§14), `pageSize=10`.

### 17.5 Frontend — Agentes.jsx

Tabla: Nombre completo (+ badge Activo/Inactivo, mismo estilo de pill que `InsuranceCompanies`), Email, Agencia, Compañía, Nro de licencia. Acciones: ✏️ Editar, 🔍 Detalle (modal nuevo con todos los campos, agrupado en secciones: Contacto, Dirección, Licencia y NPN, Agencia y contrato, Otros), 🗑/♻️ Activar-Desactivar (toggle de `isActive` vía el `PUT` existente, con confirmación — reenvía el objeto completo del agente con el flag invertido, mismo criterio que el resto de los `PUT` de este proyecto que no admiten parcial). Paginado combinable con el buscador ya existente (reset a página 1 en buscar/limpiar, mismo criterio que los filtros de Policies).

### 17.6 Compartido entre las 3 pantallas

Estilos extraídos a `src/utils/` para que las 3 pantallas usen exactamente los mismos valores (antes solo vivían inline en `Policies.jsx`):
- `detailModalStyles.js` — `detailSectionHeaderStyle`/`detailRowStyle` (headers de sección uppercase con borde, filas con más aire), ya usado en el modal de detalle de Policies desde §16.7.
- `tableStyles.js` — `tableHeaderRowStyle`/`tableCellStyle`/`actionsCellStyle` (el fix del wrap de acciones: contenedor flex `nowrap`, antes los botones solo tenían `marginRight`/`marginLeft` sueltos y se envolvían en 2 líneas)/`actionButtonStyle`/`actionLinkStyle`.
- `whatsapp.js` — `buildWhatsAppUrl(phone, message)`, extraído de Policies.jsx para reusarlo en Customers.jsx.

Verificado por el responsable en navegador: las 3 pantallas con tabla consistente, acciones en una sola línea sin wrap (incluido el fix retroactivo en Policies), modales de detalle nuevos en Customers/Agentes, WhatsApp en Customers, toggle activo/inactivo en Agentes. `dotnet build` (0 warnings/0 errors) + `npm run build` + `npm run lint` (mismos 6 problemas preexistentes) limpios en cada paso.

---

## 18. Rediseño de la tabla de Policies — nuevas columnas + scroll horizontal — ⏸ Pendiente de implementar (plan aprobado, análisis completo)

Pedido: acercar la tabla de `Policies.jsx` a una referencia visual (3 capturas del responsable — **no llegaron a esta sesión**, el plan se documentó solo a partir de la lista de columnas en texto). Antes de implementar, se auditó contra el código real qué campos ya existen y cuáles hacen falta agregar.

### 18.1 Columnas objetivo, en orden, y estado real de cada una

| # | Columna | Fuente | Estado |
|---|---|---|---|
| 1 | Customer | `Customer.FirstName`/`LastName` | ✅ Existe. **Sin avatar/iniciales, sin subtítulo de fecha** (decisión confirmada — se descartó la idea inicial de mostrar una fecha de creación del Customer debajo del nombre; no se agrega `Customer.CreatedAt`). |
| 2 | Contact | `Customer.Email` + `Customer.Phone` | ✅ Existen, ya vienen en `CustomerResponseDto` — `Policies.jsx` ya carga el listado completo de customers (`/api/customers`), no hace falta ningún endpoint nuevo. Mismo estilo visual que Contact ya usa en otras pantallas. |
| 3 | Plan | `Policy.InsurancePlan` | ✅ Existe en `Policy`/`PolicyResponseDto` (§1.11), pero **hoy no se muestra en la tabla** (la tabla actual muestra `Type`, no `Plan`). Columna nueva en el listado. |
| 4 | Type | `Policy.Type` | ✅ Ya se muestra hoy. **Se mantiene junto a Plan** — decisión confirmada, no se reemplaza una por otra, van las dos. |
| 5 | Applicants | `Policy.NumberOfApplicants` | ✅ Existe (§1.9), no se muestra en la tabla hoy. Columna nueva. |
| 6 | Status (badge) | `Policy.Status` | ⚠️ El campo existe, pero **hoy se renderiza como texto plano** (`Policies.jsx:2058`, `{translateEnum("policyStatus", p.status)}`), no como badge. `Dashboard.jsx` ya tiene un array `POLICY_STATUSES` (8 valores) + `CATEGORICAL_COLORS` para los gráficos de torta — se puede extraer a un util compartido y reusar como badge de tabla (mismo criterio que se hizo con `agencyStyle.js` para el badge de Agency en Agentes, §17). Cambio de frontend únicamente. |
| 7 | Effective date | `Policy.EffectiveDate` | ✅ Existe (nullable), sin mostrar en tabla hoy. Sin hora. |
| 8 | Agency (badge) | `Customer.Agent.Agency` (`User.Agency`) | ❌ No existe como campo propio de `Policy` ni `Customer`. **Decisión confirmada: derivar en vivo**, no agregar campo nuevo — sumar `AgentAgency` a `CustomerResponseDto` (mapeo directo desde `Customer.Agent.Agency` en el controller, mismo patrón que ya tiene `AgentName`). Sin migración. **Trade-off aceptado explícitamente**: esto refleja la agencia ACTUAL del agente, no la agencia que tenía al momento de escribir esa póliza puntual (que sí existe sin usar en el xlsx de origen, columna `"Agency"` presente en los 4 archivos de pólizas — se descarta usarla). Mismo estilo de badge que ya usamos en Agentes (`src/utils/agencyStyle.js`, reusable tal cual). |
| 9 | Agent (solo texto) | `CustomerResponseDto.AgentName` | ✅ Ya resuelto server-side, sin avatar — mismo criterio ya aplicado en Agentes (§17.5). |
| 10 | State/Province | `Customer.State` | ✅ Existe, ya en `CustomerResponseDto`. Confirmado que la columna real del xlsx de origen se llama literalmente `"State / Province"` en los 4 archivos de pólizas (`CommonFieldsExtractor.cs:38`). |
| 11 | Registration date | `Policy.CreatedAt` (**nuevo**) | ❌ No existe (`Policy` solo tiene `UpdatedAt`, §9.6). La columna `"Registration date"` del xlsx de origen **ya se lee hoy** (`CommonFieldsExtractor.cs:58`) pero solo se usa para setear el `ChangedAt` de la primera fila de `PolicyHistory` de cada póliza — tanto para las migradas como para las creadas en vivo (`PoliciesController.Create` siempre inserta un `PolicyHistory` inicial vía `RecordStatusChange`). **Backfill limpio, sin re-leer ningún xlsx**: `UPDATE Policies SET CreatedAt = (SELECT MIN(ChangedAt) FROM PolicyHistory WHERE PolicyId = Policies.Id)`. Sin hora. |
| 12 | Renewal status | `Policy.RenewalStatus` (**nuevo**) | ❌ No existe. La columna `"Renewal status"` existe **solo en el xlsx de Health/Obamacare** (1258 de ~1283 filas migradas) — Medicare/Life/Supplemental no tienen esta columna en absoluto, quedarán siempre en `null` para esos tipos (esperado, no es un bug). Backfill parcial: re-leer el xlsx de Health y matchear contra `Policy.PolicyNumber` (que sí tiene índice único en la base — `PolicyConfiguration.cs:18`). **Riesgo, a diferencia de los backfills anteriores** (User/Email tenía clave única confiable al 100%): `PolicyNumberRaw` es nullable por fila en el origen, puede haber huecos de match. **Obligatorio correr `--dry-run` primero y revisar cuántas pólizas matchean vs. quedan sin match antes de aplicar `--commit`.** |

### 18.2 Explícitamente fuera de alcance por ahora: Tags

`Customer.Tags` ya existe de punta a punta (modelo, DTOs, formulario de alta/edición de Customer, §3.2) — no hace falta ningún cambio de schema para mostrarlo. Pero nunca se hizo backfill histórico: la columna `"Tags"` del xlsx de origen existe **solo en el archivo de Health** (1258 filas) y el importer nunca la lee — hoy prácticamente todos los customers migrados tienen `Tags = null`; solo tendrían valor los que alguien cargó/editó a mano después de la migración. **Se descartó agregar esta columna en esta ronda** — queda para revisar después si hace falta.

### 18.3 Mantener sin cambios

Íconos/botones/estilos actuales de acciones (🔍 lupa, ✏️ lápiz, 🗑 tacho, 💬 WhatsApp) — no se reemplazan por otro estilo aunque la referencia visual muestre algo distinto.

### 18.4 Layout: scroll horizontal, no "todo sin scroll"

A diferencia de Agentes (§17.5 — 6 columnas, se logró eliminar el scroll achicando avatares/fecha), acá con 12 columnas de datos + acciones se decidió que **sí** va a haber scroll horizontal dentro de la tabla (mismo contenedor `overflowX:auto` que ya usamos), igual que muestra la referencia. El fix del bug de `#root` en `index.css` (encontrado y corregido durante el rediseño de Agentes, §17) es lo que permite que ese scroll funcione bien — antes de ese fix, el contenido se desbordaba sin generar una barra de scroll usable.

### 18.5 Fechas sin hora

Effective date y Registration date se muestran ambas como `dd/mm/aaaa`, sin hora — mismo criterio que ya se aplicó a Registration date en Agentes (§17).

### 18.6 Orden de implementación sugerido cuando se retome

1. **`Policy.CreatedAt`** — migración + backfill vía `MIN(PolicyHistory.ChangedAt)`. Bajo riesgo, dato ya disponible en la base, sin re-leer ningún xlsx.
2. **`Policy.RenewalStatus`** — migración + backfill parcial vía `PolicyNumber` contra el xlsx de Health/Obamacare. **`--dry-run` obligatorio antes de `--commit`**, revisar cobertura real (cuántas matchean vs. quedan `null`) antes de aplicar en serio.
3. **`CustomerResponseDto.AgentAgency`** — una línea de mapeo en el controller, sin migración.
4. **Frontend**: las 12 columnas en el orden definido, Status como badge (extrayendo la paleta de `Dashboard.jsx`), Agency/Agent sin avatar (mismo criterio que Agentes), scroll horizontal, fechas sin hora.
5. Build + lint + verificación en navegador (mismo checklist que las sesiones anteriores), antes de dar el trabajo por cerrado.

---

## 19. Orden sugerido de trabajo

1. ~~Tipo en Policy~~ ✅ Hecho
2. ~~Dependientes (vínculo con Customers existentes)~~ ✅ Hecho
3. ~~Botón de WhatsApp~~ ✅ Hecho
4. ~~Buscador/filtro de pólizas~~ ✅ Hecho
5. ~~Modal de detalle de póliza~~ ✅ Hecho (contenido base, ver §1.4)
6. ~~Refactorizaciones (API client, variable de entorno, refresh automático)~~ ✅ Hecho
7. ~~Mover DTOs de Customer/Policy a archivos separados~~ ✅ Hecho
8. ~~Compañía aseguradora en Policy~~ ✅ Hecho (rediseñada a tabla propia tras el análisis del archivo real, §1.5)
9. ~~Relación con el principal + Es aplicante~~ ✅ Hecho
10. ~~Documentos de póliza~~ ✅ Hecho
11. ~~Agentes (Agente/Asistente/Record) + datos demográficos en Customer~~ ✅ Hecho
12. ~~Selector de idioma ES/EN~~ ✅ Hecho
13. ~~Definir y cerrar el enum de Status de Policy~~ ✅ Hecho (§1.10, corregido tras el análisis del archivo real: "Actualizado" no "En corrección")
14. ~~Campos nuevos de Customer + renombrado "Legal Status"~~ ✅ Hecho (§3.2, §3.3)
15. ~~Period + Number of applicants en Policy~~ ✅ Hecho (§1.8, §1.9)
16. ~~Crear Customer nuevo desde Members/Dependientes de la póliza~~ ✅ Hecho (§2)
17. ~~Gestión de contraseñas (cambio forzado, cambio desde perfil, recuperación por email)~~ ✅ Hecho (§10)
18. ~~Campos nuevos de Agente~~ ✅ Hecho (§11)
19. Firma digital de consentimiento — bloqueado hasta que el responsable elija proveedor (§4.1)
20. ~~Infraestructura de hosting (VPS) — Dockerfiles/compose/README~~ ✅ Hecho (§8.1); falta el despliegue real al VPS
21. ~~Campos de plan (ACA) y financieros en Policy~~ ✅ Hecho (§1.11)
22. ~~Migración de datos del sistema anterior~~ ✅ Hecho (§7): script implementado y corrido con `--commit`; queda pendiente no bloqueante reasignar `Customer.AgentId` de las filas con fallback cuando los agentes reales estén cargados (ver §7)
23. ~~Mensajes de error del backend no llegaban al usuario~~ ✅ Hecho (§5.3, encontrado verificando InsuranceCompanies en el navegador)
24. ~~Middleware global de excepciones no controladas~~ ✅ Hecho (§5.4)
25. ~~AdminUserSeeder generalizado (admin real por ambiente vía env vars)~~ ✅ Hecho (§10.5)
26. ~~Dashboard~~ ✅ Hecho (§9, ver también el ítem 40 de esta misma lista)
27. ~~Relevamiento de campos específicos por Tipo de Póliza (Life/Medicare/Supplemental)~~ ✅ Documentado (§12) e ✅ implementado en su totalidad: Medicare (§12.10), Life Insurance (§12.3/§12.6) y Supplemental Plans (§12.9) — decisión del responsable de adelantarlos pese al bajo volumen relevado
28. ~~Bug de arranque en frío con Docker real (Error 4060) — healthcheck de SQL Server + orden de migración/seed~~ ✅ Hecho (§8.1.1)
29. ~~Hallazgos de auditoría del feature de Agentes (validación cruzada server-side, búsqueda en `/users`, rate limiting)~~ ✅ Resuelto 3/3, ver §11.1
30. ~~Address1/City/ZipCode/State/County obligatorios en Agente + Country fijo~~ ✅ Hecho (§11.2), verificado en navegador
31. ~~Validación cruzada server-side de Licensed/HasCompanyContract~~ ✅ Hecho (§11.3) — cierra el punto 1 de §11.1
32. ~~Búsqueda en `/users` + rate limiting en endpoints sensibles de auth~~ ✅ Hecho (§11.4) — cierra los puntos 2 y 3 de §11.1
33. ~~Historial/Auditoría de Pólizas (PolicyHistory, tracking de Status, prerequisito para el script de migración)~~ ✅ Hecho (§13)
34. ~~Paginado en Policies~~ ✅ Hecho (§14)
35. ~~Campo Agency en Agente~~ ✅ Hecho (§15.1)
36. ~~Importar los 41 agentes reales como Users~~ ✅ Hecho (§15.2)
37. ~~Reasignación de agentes en pólizas ya migradas~~ ✅ Hecho (§15.3), 1178/1179 (99.92%)
38. Cierre del gap de seguridad de §10.1 (MustChangePassword solo de frontend) — ✅ Hecho, middleware de backend agregado
39. Reconciliación de campos §1.11 (StartDate/EndDate/Premium vs EffectiveDate/Period/MonthlyPremiumAmount) — ✅ Analizado y resuelto, se dejan como están (decisión del responsable, documentado en §1.11)
40. ~~Dashboard (KPIs, gráficos por Tipo/Status, estadísticas, últimas pólizas, próximos a cumplir 65, scoping por rol)~~ ✅ Hecho (§9), incluye `Policy.UpdatedAt` nuevo (§9.6) y paleta de colores semántica en toda la app (§9.1)
41. ~~Modal/Dialog reutilizable para crear/editar (Policies/Customers/Agentes/InsuranceCompanies)~~ ✅ Hecho (§16), incluye 2 bugs encontrados/corregidos en el camino (foco del Modal, falso required por Premium=0) y ajuste visual del modal de detalle
42. ~~Unificación de listados de Customers/Agentes al estilo tabla de Policies + paginado en los 3 (pageSize=10)~~ ✅ Hecho (§17), incluye `User.IsActive` nuevo (baja lógica de agentes, reemplaza el DELETE que hubiera fallado siempre por FKs Restrict) y 2 modales de detalle nuevos
43. Rediseño de la tabla de Policies (12 columnas nuevas, scroll horizontal, `Policy.CreatedAt`/`Policy.RenewalStatus` nuevos) — plan completo documentado y aprobado, implementación pendiente (§18)
