# Pendientes — WholeCareInsurance

> Auditado contra código real el 2026-07-13 (modelos, DTOs, migraciones aplicadas y componentes de frontend). Donde el pedido del responsable no coincidía con lo implementado, se priorizó lo verificado en código — ver notas "⚠️ Discrepancia" en los puntos afectados.

---

## 1. Policies — campos y funcionalidades

### 1.1 Campo Tipo (dropdown) — ✅ Hecho
`Type` en `Policy` restringido a `Obama Care`, `Salud`, `Auto`, `Otro` vía `[AllowedValues]` en `PolicyCreateDto`. `<select>` en el formulario, igual patrón que el resto de los enums.

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

**Nota abierta, no bloqueante**: `Policy` ya tenía `StartDate`/`EndDate` y `Premium` — hay superposición conceptual con `EffectiveDate` y `MonthlyPremiumAmount` que no se resolvió a propósito (se pidió que fueran campos nuevos, sin tocar el script de migración todavía). Reconciliar cuando se diseñe el script real.

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

---

## 6. Dashboard y UX general

### 6.1 Dashboard — ver §9
Placeholder, bloqueado hasta tener la data migrada (§7).

### 6.2 Selector de idioma (Español/Inglés) en el Header — ✅ Hecho, confirmado
`react-i18next` con diccionarios por namespace. `translateEnum()` desacopla el valor guardado en la DB (español) del texto mostrado. `User.PreferredLanguage` (default `"en"`) persistido vía `PUT /users/me/language`. Sin cambios desde la última revisión — sigue sin nada pendiente en este punto.

---

## 7. Migración de datos del sistema anterior — ⏸ Bloqueado, en espera de respuesta

Se solicitó al responsable del proyecto el archivo de export **completo** (todas las pólizas, todos los tipos en un solo archivo, no separado por tipo). De las 4 preguntas originales, **3 quedaron resueltas por el análisis del archivo real** y solo queda una duda puntual dentro de la 4ª (ver §7.2):
1. ~~Si la columna "Members" trae solo cantidad o el detalle completo de cada dependiente.~~ ✅ Resuelta — ver §7.1.
2. ~~Si existe un ID interno para Agentes/Agencias además del nombre.~~ ✅ Resuelta — ver §7.2.
3. ~~Si el export incluye solo pólizas activas o también históricas/canceladas.~~ ✅ Resuelta — ver §7.2.
4. Diccionario de datos para: Reference, Marketplace ID, Contract identification, Renewal status, Confirmed consent. 🔶 Parcialmente resuelta — ver §7.2, solo `Contract identification` sigue en duda (pregunta puntual enviada, en espera de respuesta).

**Columnas detectadas** en la pantalla de export del sistema anterior (referencia para el futuro mapeo): Reference, Agency, Agent, Full name, First/Middle/Last name, DOB, Gender, Email, Phone, Legal Status, SSN, Green card, Work permit, Estado civil, Address 1/2, City, State/Province, Zip code, County, Employer name, Company Phone, Position/Occupation, Annual income, Policy number, Marketplace ID, Contract identification, Number of applicants, Effective date, Company, Insurance plan, Type of plan, Tax Credit/Subsidy, Monthly premium amount, Status, Tags, Period, Confirmed consent, Registration date, Update date, Renewal status, Members.

> Nota: buena parte de estas columnas ya mapean directo a los campos nuevos de Customer (§3.2) y Policy (`Period`/`Number of applicants`, §1.8/§1.9, y ahora también `InsuranceCompany`/`Type of plan`/`Insurance plan`/`Effective date`/`Tax Credit-Subsidy`/`Monthly premium amount`, §1.5/§1.11) — todos cerrados. Ya no hay campos pendientes de agregar antes de diseñar el script de migración; solo falta la respuesta sobre `Contract identification` (§7.2) y confirmar si el archivo ya analizado es el export completo pedido o solo el recorte de Obamacare.

### 7.1 Hallazgos del análisis del archivo real (Health Insurance/Obamacare, 1258 filas)

- **Detalle completo de dependientes confirmado**: la columna "Members" (y las columnas asociadas por dependiente) sí traen el detalle completo, hasta 8 dependientes por póliza — no solo el conteo. Corrige el supuesto anterior (se pensaba que quizás solo venía la cantidad).
- **`Policy number`, `Marketplace ID` y `Contract identification` no sirven como clave de vinculación**: 90%, 86% y 99% de las filas respectivamente tienen esas columnas vacías. No se puede confiar en ninguna de las tres para vincular el historial de una misma póliza a través de sus duplicados.
- **La reconstrucción de historial va a necesitar una heurística de matching, no un match 100% automático**: SSN + Aseguradora + fecha efectiva cercana, con revisión manual de los casos ambiguos. Caso real confirmado en el archivo: un cliente con 4 registros duplicados, cada uno con un `Reference` distinto, mismo `Effective date`, sin `Policy number` en ninguno de los 4 — nada permite decidir automáticamente si son 4 versiones de la misma póliza o 4 pólizas distintas.
- **SSN tampoco puede ser la única clave**: vacío en ~7% de las filas. No sirve como único criterio ni para detectar duplicados de historial ni para relacionar dependientes ya existentes en `Customer`.
- **Mapeo de "Dependency type"**: "Parent" (sin distinción de género) y "Dependent" (genérico) del archivo de origen mapean ambos a `"Otro"` en `RelacionConPrincipal` — ya cubierto por el enum actual (§1.6), no hace falta agregar valores nuevos.

Estos hallazgos no destraban el bloqueo del todo (ver estado actualizado más abajo), pero ya dejan clara la estrategia de matching a diseñar cuando se arme el script real: heurística + cola de revisión manual, no un mapeo directo por clave única.

### 7.2 Agentes/Agencias, historial completo y diccionario de datos — resuelve las preguntas 2, 3 y parte de la 4

**Pregunta 2 — ID interno de Agentes/Agencias (RESUELTA)**: no existe. Tanto `Agent` como `Agency` son campos de texto libre en el archivo.
- `Agency`: solo 2 valores en las 1258 filas — "Preventive Health Insurance" (894 filas) y "Whole Care Insurance Group llC" (364 filas).
- `Agent`: 22 nombres únicos en total.

Bajo riesgo de colisión por nombre dado el volumen chico (22 agentes) — igual conviene revisar manualmente antes de mapear, para detectar posibles duplicados o errores de tipeo, pero esto no bloquea la migración.

**Pregunta 3 — ¿solo activas o también históricas/canceladas? (RESUELTA)**: el archivo incluye **todo el historial**, no solo pólizas activas. Distribución real de `Status` en las 1258 filas: `Processed` (1016), `Updated` (79), `Canceled` (75), `Draft` (61), `To be processed` (21), `In Process` (3), `Agent change` (2), `Pending` (1).

**Pregunta 4 — diccionario de datos (PARCIALMENTE RESUELTA)**:
- `Reference`: identificador único por **registro** (formato "P" + fecha + secuencial, ej. `P15072026018434`) — identifica la versión/registro puntual, no la póliza a través del tiempo. Por eso cada duplicado del historial de una misma póliza tiene un `Reference` distinto (consistente con el hallazgo de §7.1 sobre el caso de las 4 versiones duplicadas).
- `Marketplace ID`: formato consistente con identificadores oficiales del Marketplace de ACA (Plan Year + Estado + código de plan, ej. `PY26 TN SBC 23552TN0020052-06`, a veces solo numérico). Es un dato externo — no se puede validar sin confirmación del responsable, pero el formato observado es coherente con lo esperado.
- `Contract identification`: ⚠️ **sigue abierta** — formato inconsistente en los datos reales: a veces parece un código de plan (ej. `23552TN0020005`) y otras veces directamente el nombre del plan (ej. `Connect Silver-2 3000 Indiv Med Deductible - EPO`). No está claro si es un dato confiable para usar tal cual. Se envió una pregunta puntual al responsable sobre este campo específico, en espera de respuesta.
- `Renewal status` y `Confirmed consent`: sin hallazgos nuevos todavía, quedan para cuando se revise el resto del diccionario.

**Estado actualizado de las 4 preguntas originales**: 1, 2 y 3 resueltas por el análisis del archivo real; la 4 (diccionario de datos) resuelta en su mayor parte, con `Contract identification` como única duda puntual pendiente de respuesta.

**BLOQUEADO se mantiene únicamente por**: la respuesta del responsable sobre `Contract identification`, y por recibir los archivos de los demás tipos de póliza si se decide migrarlos además de Obamacare (el archivo analizado hasta ahora es específicamente Health Insurance/Obamacare — no está confirmado si es el export "completo, todos los tipos en un solo archivo" que se había pedido originalmente, o solo el recorte de este tipo). No diseñar el script de migración hasta resolver esto. Se probará primero contra la base del ambiente de test (§8.1), nunca directo contra producción.

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
- Variables de entorno mapeadas: `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenMinutes`, `Cors__AllowedOrigin`, `ASPNETCORE_ENVIRONMENT`, `Brevo__ApiKey`, `Brevo__SenderEmail`, `Frontend__BaseUrl` (sin `Brevo__ApiKey` seteado, el backend cae a un servicio que solo loguea el email en vez de enviarlo, así que hay que setearlo en Test/Prod para que el flujo de "olvidé mi contraseña" funcione de verdad).

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

**Pendiente real:** ejecutar esto en el VPS (dar de alta los servicios en EasyPanel, reemplazar los placeholders del compose por secretos reales, probar el build de las imágenes con Docker de verdad, configurar dominios/DNS). Se decidió posponer para una próxima sesión dedicada al VPS en sí.

### 8.2 Ver §7 para la migración de datos (antes en esta sección, movida para agrupar con el resto de la migración).

---

## 9. Dashboard — ⏸ Bloqueado hasta tener la data migrada

No implementar hasta que la migración de datos del sistema anterior (§7) esté completa.

### 9.1 Paleta de colores general (aplica a toda la app, no solo Dashboard)
- Colores a resaltar: verde, blanco y azul.
- Botones semánticos: Submit/Guardar → verde, Eliminar → rojo, Editar → amarillo.

### 9.2 Referencia visual del Dashboard
Fila de tarjetas KPI: Agencias, Agentes, Pólizas (+ miembros), Recordatorios.
Fila de tarjetas por estado de póliza (cantidad + miembros por cada una): `Draft`, `Pendiente`, `Cancelado`, `Por procesar`, `En proceso`, `Actualizado`, `Procesado`, `Cambio de agente` — actualizado según el enum final de §1.10 (corregido tras el análisis del archivo real: "Actualizado" sí es el 8vo valor real, reemplaza a "En corrección").
✅ Enum de Status ya resuelto (§1.10) — este punto ya no está bloqueado por eso, solo sigue bloqueado por la migración de datos (§7, ver encabezado de §9).
Gráficos: torta "Pólizas por Tipo" (campo `Type`, ya existe), torta "Pólizas por Status".

### 9.3 Estadísticas adicionales solicitadas
Cantidad de pólizas/clientes por Compañía aseguradora (ya existe, §1.5), por Cliente, por Miembros (dependientes + titulares), por Condado, por Ciudad.
⚠️ Condado y Ciudad ya existen en `Customer` (§3.1) — este punto ya no está bloqueado por falta del campo, pero sigue bloqueado por falta de datos migrados (§7).

### 9.4 Pendiente de definir antes de implementar
- ¿Filtros por rango de fechas o por agente, o siempre total general?
- ¿"Reminders" es un concepto ya existente o una funcionalidad nueva a definir aparte? (auditado: no existe ningún modelo/tabla de recordatorios hoy en el backend)
- ¿Existe algún rol intermedio (ej. supervisor de agencia)? Hoy el sistema solo maneja `Admin`/`Agente` (`User.Rol`, sin otros valores en uso).

### 9.5 Alcance de datos según rol
Admin ve todo; Agente ve solo lo propio (mismo criterio de scoping que ya usa el resto de la API vía `AgentId`). Los endpoints de estadísticas deben aplicar el mismo filtro — no exponer un endpoint que devuelva el total global sin control de acceso.

### 9.6 Widget "Últimas pólizas" (Latest policies)
Últimas 10 pólizas por fecha de actualización. Columnas: Cliente (link), teléfono/email, Status (badge), fecha/hora de última actualización. Mismo scoping por rol (§9.5).

### 9.7 Widget "Próximos/recientes a cumplir 65 años" (elegibilidad Medicare)
Ventana: 4 meses antes/después del cumpleaños 65. Columnas: nombre (link), fecha de nacimiento, edad. Sin job ni campo persistido — calculado al vuelo desde `DateOfBirth` en la query. Mismo scoping por rol (§9.5).

---

## 10. Gestión de contraseñas — ✅ Hecho

Tres flujos nuevos, ninguno existía antes de esta sesión (no había forced-change, self-service change, ni recuperación por email en el sistema).

### 10.1 Cambio forzado en el primer login
`User.MustChangePassword` (bool) nuevo — se pone en `true` cuando un Admin crea un agente vía `POST /auth/register` (`Agentes.jsx`), y también para el admin seedeado (`AdminUserSeeder`, más una migración de datos que lo fuerza en bases ya existentes, ya que `Admin123!` es una credencial default documentada en este mismo archivo). El login (`AuthResponseDto`) devuelve el flag; el frontend lo persiste en `localStorage` y redirige a `/change-password` (ruta nueva, sin `AppLayout`) antes de dejar entrar a cualquier otra pantalla. `AppLayout` también revisa el flag en su reconciliación de fondo contra `GET /users/me` (mismo mecanismo que ya usaba para el idioma), para cubrir el caso de un Admin que fuerza el cambio sobre una sesión ya activa.
- **Gap conocido, no cerrado a propósito**: el gating es solo de frontend (redirect de rutas). Alguien con el access token podría seguir llamando otros endpoints de la API directamente mientras `MustChangePassword=true`. Cerrar esto del todo requeriría un filtro/middleware de backend — no se implementó porque no estaba en el plan original aprobado; queda como mejora pendiente si se necesita.

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

---

## 11. Agentes — campos nuevos en el formulario de creación/edición — ✅ Hecho

Los 18 campos agregados a `Models/User.cs`, `AuthRegisterDto`/`UserUpdateDto`/`UserResponseDto`, migración `20260715133955_AddAgentProfileFields`, y formulario/tarjeta de `Agentes.jsx`:
- `MiddleName` (texto, opcional)
- `Gender` (dropdown Masculino/Femenino, mismo criterio que Customer §3.2 — reusa `GENDERS`/grupo `gender` de `translateEnum`)
- `Address1`/`Address2`, `City`, `ZipCode` (texto, opcional, mismo patrón que Customer)
- `State`/`County` (dropdowns EE.UU.-only, reusan directamente `src/data/usStates.js` y `usCounties.json` — **decisión confirmada con el responsable**: Country es siempre EE.UU., no se agregó como campo editable; "State/Province" del pedido original es el mismo `State` de 2 letras que ya usa Customer, condado dependiente del estado igual que en Customer)
- `Licensed` (bool, dropdown Sí/No) + `LicenseNumber` (texto, **condicional**: solo visible/habilitado si Licensed = Sí — confirmado con el responsable)
- `NpnNumber` (texto) + `NpnOverride` (bool, checkbox)
- `HasCompanyContract` (bool, dropdown Sí/No "¿Tiene contrato con una compañía?") + `ContractNumber`/`CompanyName` (texto, **condicionales**: solo visibles/habilitados si HasCompanyContract = Sí — confirmado con el responsable)
- `ContractsWanted` (texto, comma-separated — checkboxes múltiples: Medicare, Obamacare, Supplemental Plans, Life Insurance; sin tabla nueva, mismo criterio liviano que `Tags` de Customer; traducido vía grupo `contractInterest` de `translateEnum`)
- `AdditionalInformation` (texto libre/notas, textarea)
- `TermsAccepted` (bool) + `TermsAcceptedAt` (fecha/hora) — **confirmado con el responsable**: obligatorio para guardar (checkbox `required` nativo del navegador, mismo form para alta y edición) y se persiste el timestamp de cuándo se aceptó. Validado también en el backend (`AuthController.Register` rechaza con 400 si `TermsAccepted` no es `true`); en edición (`UsersController.Update`) no se re-exige — si ya era `true` se mantiene, y solo se pisa `TermsAcceptedAt` si pasa de `false` a `true`.

Verificado con curl (alta con los 18 campos, rechazo de registro sin `TermsAccepted`, edición con limpieza de campos condicionales) y con Playwright en español (35/35 checks: los campos nuevos renderizan, License Number/Contract Number/Company Name aparecen y desaparecen según sus dropdowns condicionales, Condado deshabilitado hasta elegir Estado, el submit se bloquea sin marcar el checkbox de términos, alta y edición end-to-end con persistencia correcta, tarjeta de la lista muestra los campos nuevos, sin errores de consola).

---

## 12. Orden sugerido de trabajo

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
22. Migración de datos del sistema anterior — 3 de las 4 preguntas originales resueltas por el análisis del archivo real (§7.1, §7.2); bloqueado solo por la respuesta sobre `Contract identification` y por los archivos de otros tipos de póliza si corresponde
23. ~~Mensajes de error del backend no llegaban al usuario~~ ✅ Hecho (§5.3, encontrado verificando InsuranceCompanies en el navegador)
24. Dashboard — bloqueado hasta tener la data migrada (§9)
