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

### 1.12 Restringir formatos de archivo permitidos en upload de documentos — ✅ Hecho (2026-08-05)

**Estado actual verificado en el código (antes de implementar):** el upload ya tiene validación completa en ambos lados, no es una implementación desde cero. Frontend (`Policies.jsx:25`): `ALLOWED_DOCUMENT_EXTENSIONS = [".pdf", ".docx", ".jpg", ".jpeg"]`, usado en el `accept` del `<input type="file">` y en `handleUploadDocument` (rechaza antes de subir, mensaje `t("documents.invalidExtension")`). Backend (`Utils/FileValidationHelper.cs`): misma lista de extensiones + verificación de magic bytes por tipo (`MatchesContentAsync` — firma `%PDF`, firma JPEG `FF D8 FF`, o estructura ZIP/OOXML real para `.docx`), devuelve `BadRequest(ProblemDetails)` con mensaje explícito que ya llega al usuario sin caer en el error genérico (§5.3). **No hay `.png` soportado hoy en ningún lado** (ni upload ni preview).

**Cambio implementado:** lista permitida achicada a PDF + imágenes (JPG, JPEG, PNG), sacando `.docx`:
1. Backend `FileValidationHelper.cs`: `.docx` quitado de `AllowedExtensions` y de `MatchesContentAsync` (se eliminó `IsValidDocxAsync` y el `using System.IO.Compression` que ya no se usaba); `.png` agregado con verificación de magic bytes (firma `89 50 4E 47 0D 0A 1A 0A`).
2. Frontend `Policies.jsx`: `ALLOWED_DOCUMENT_EXTENSIONS` → `[".pdf", ".jpg", ".jpeg", ".png"]` (ajusta automáticamente el `accept` del input y la validación de `handleUploadDocument`).
3. Mensaje de error actualizado en backend (`PoliciesController.cs:350`) y traducción `documents.invalidExtension` (ES/EN) reflejan la lista nueva.
4. `PREVIEWABLE_DOCUMENT_TYPES` (frontend) y `PreviewableContentTypes` (backend, `PoliciesController.cs`) actualizados para incluir `image/png` — el PNG es previsualizable inline, no solo descargable, mismo criterio que PDF/JPEG.

**Mensaje de error al usuario:** confirmado funcionando end-to-end, no fue necesario construir nada nuevo — solo se actualizó el texto. Verificado con curl: subir un `.docx` devuelve `400` con `{"title":"Tipo de archivo no permitido. Se aceptan: .pdf, .jpg, .jpeg, .png."}`; subir un `.png` válido devuelve `201` con `contentType: image/png` y el preview inline (`?inline=true`) responde `Content-Disposition: inline`. Verificado también en la app corriendo: el `<input type="file">` real tiene `accept=".pdf,.jpg,.jpeg,.png"`. `dotnet build` y `npm run build`/`npm run lint` limpios (sin errores ni warnings nuevos).

**Relación con §27.2 (preview de `.docx`):** ya no se pueden subir nuevos `.docx` al sistema. **Cierra §27.2 (2026-08-05)**: verificado con SQL directo (`SELECT ... FROM PolicyDocuments WHERE OriginalFileName LIKE '%.docx' OR ContentType LIKE '%wordprocessingml%' OR ContentType LIKE '%msword%'`) que no hay ningún `.docx` guardado — la tabla `PolicyDocuments` tiene 3 filas en total (2 `application/pdf` + 1 `image/jpeg`), 0 `.docx`. No había nada que borrar ni que migrar. Ver cierre en §27.2.

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

### 4.1 Firma digital de consentimiento de póliza — 📋 Proveedor definido (Dropbox Sign) — implementación técnica no iniciada
Investigación de proveedores completada (2026-08-05). **Proveedor elegido por el responsable (2026-08-05): Dropbox Sign (HelloSign)** — ver detalle abajo. Sigue sin implementar (no hay ninguna referencia a Dropbox Sign en el código, solo en este documento). El canal de notificación ya estaba definido (ver abajo); el costo de la suscripción también está confirmado — lo asume el responsable directamente.

**Estado de infraestructura (2026-08-05):** cuenta de Twilio ya creada por el responsable (dominio wholecareinsurancellc.com) — infraestructura lista para empezar integración técnica en paralelo a la decisión de proveedor de firma.

**Opciones de proveedor (finalistas tras la investigación) — DECISIÓN FINAL (2026-08-05): Dropbox Sign (HelloSign).** DocuSign queda descartado. Comparación completa dejada documentada a continuación:
- **DocuSign — ❌ Descartado**: mayor reconocimiento y compliance en la industria de seguros en EE.UU., mejor para auditorías/regulación. Más caro — plan Business Pro ~$40-45/user/mes, con riesgo de overage si se excede el límite de envelopes.
- **Dropbox Sign — ✅ Elegido**: más económico — plan Standard ~$25/user/mes (mínimo 2 seats), firmas ilimitadas sin cargos extra, integración de API más rápida de implementar. Menos estándar en el sector seguros, pero legalmente igual de válido.
- **Criterio decisivo:** volumen mensual esperado de pólizas (confirmado por el responsable, ver abajo). No surgieron requisitos de compliance/regulación estatal que empujaran específicamente hacia DocuSign.
- **Volumen de pólizas confirmado por el responsable (2026-08-05):** ~100 pólizas/mes en promedio, con picos de 1,000 a 2,000 pólizas en 2 meses seguidos (temporada de renovación).
  - **Con DocuSign:** en meses pico, si el plan no cubre ese volumen, el overage ($3-8/envelope excedido) podría representar entre ~$4,200 y $11,200 extra en un solo mes de 1,500 firmas. Evitarlo requeriría negociar un plan enterprise con cupo alto, lo que probablemente sube el costo base todo el año, no solo en los meses pico.
  - **Con Dropbox Sign:** al ser firmas ilimitadas en el plan Standard, el costo se mantiene estable todo el año (~$50/mes con 2 seats) sin importar el pico estacional.
  - **Conclusión — decisión final confirmada por el responsable (2026-08-05):** el patrón de picos estacionales de este negocio favorece a Dropbox Sign en términos de costo; no surgió ningún requisito de compliance que obligara a usar DocuSign. Dropbox Sign es el proveedor elegido.

**Descartadas en la etapa de investigación:** SignWell y Documenso (self-host) quedaron fuera de las opciones activas — no es la recomendación actual, pero no está bloqueado retomarlas si el responsable lo pide explícitamente.

**Notificación (decidido, 2026-08-05):** email + SMS vía Twilio, ambos canales (no uno u otro). Ya no es parte de la decisión pendiente.

**Nota — proceso actual de consentimiento (relevado 2026-08-05):** el responsable describe un mecanismo ya en uso hoy — nombre completo + huella + registro de IP/ubicación/fecha-hora — pero desconoce el detalle técnico de su implementación interna. Esto no es equivalente a una firma electrónica certificada por un proveedor (DocuSign/Dropbox Sign), y no está claro si cumple de forma robusta con el ESIGN Act. Verificado en el código de este repo: el único rastro relacionado es `Policy.ConsentSigned` (`bool?`, checkbox simple, sin lógica de IP/ubicación/huella asociada — ver §2) — el mecanismo descrito por el responsable no parece estar implementado en este CRM, lo que sugiere que es externo o manual, pero falta confirmarlo. Pendiente confirmar con el responsable: (a) si este mecanismo está implementado dentro del código actual del CRM o es un proceso externo/manual, y (b) si la intención es reemplazarlo por completo con el proveedor que se elija, o mantenerlo en paralelo. Mientras no se aclare esto, no asumir que la migración a DocuSign/Dropbox Sign es un reemplazo limpio — podría haber lógica o datos existentes atados a este mecanismo actual que haya que migrar o conciliar.

**Flujo:** generar PDF al crear la póliza → proveedor notifica al cliente → cliente firma en hosted signing page → webhook nuestro descarga el PDF firmado y lo asocia a la `Policy` (nuevo campo de estado de consentimiento + ubicación del PDF).

Proveedor ya definido (Dropbox Sign) — implementación técnica no iniciada todavía. Costo de la suscripción confirmado: lo asume el responsable directamente. No queda ningún punto de decisión pendiente sobre proveedor/pago — solo falta la implementación técnica.

### 4.2 Botón de WhatsApp para agentes — ✅ Hecho
Click-to-chat: botón 💬 en cada fila de la tabla de Policies, abre `https://wa.me/<telefono>?text=...` con el `Phone` del Customer titular.

---

### 4.3 Evaluar Twilio (SendGrid) para email transaccional de agentes — ⏸ Pendiente de investigación/decisión
Ya se decidió usar Twilio para SMS + email en el flujo de firma de consentimiento (§4.1). Falta evaluar si conviene usar también el servicio de email de Twilio (SendGrid) para otros flujos transaccionales del sistema, puntualmente el envío de emails a agentes para cambio/recuperación de contraseña, en vez de mantener un proveedor de email separado. Verificado en el código: hoy ese flujo usa `BrevoEmailService` (Brevo) en producción y `ConsoleEmailService` en desarrollo, vía la interfaz `IEmailService` (`Program.cs:31,33`).

**Estado de infraestructura (2026-08-05):** cuenta de Twilio ya creada por el responsable (dominio wholecareinsurancellc.com) — infraestructura lista para empezar integración técnica en paralelo a la decisión de proveedor de firma.

**Acción propuesta:** confirmar con el responsable si hay razones para migrar de Brevo a SendGrid, y evaluar dos caminos posibles: consolidar todo el email transaccional (consentimiento + password reset + otras notificaciones futuras) bajo SendGrid en un solo proveedor, o mantener Brevo para lo existente y sumar SendGrid solo para el flujo nuevo de consentimiento. La decisión entre migración completa o coexistencia de ambos proveedores queda a criterio del responsable.

**Bloqueo:** ninguno técnico, es una decisión de arquitectura/costos a confirmar.

---

### 4.4 Plan de implementación — Integración Twilio (SMS + SendGrid) — 📋 Documentado, no iniciado

**Prerrequisitos a confirmar con el responsable antes de empezar a codear:**
- Registro A2P 10DLC (obligatorio en EE.UU. para envío de SMS transaccional/aplicación-a-persona) — confirmar si ya se inició este proceso en la cuenta de Twilio.
- Verificación de dominio en SendGrid (registros DNS: SPF y DKIM) para `wholecareinsurancellc.com`, necesario para buena entregabilidad de emails.
- Generación de credenciales: Account SID + API Key (recomendado usar API Key en vez de Auth Token por seguridad) desde el dashboard de Twilio.
- Definir dónde se van a guardar esas credenciales en el proyecto. Verificado en el código: hoy `BrevoEmailService` sigue el patrón `appsettings.json` (sección `Brevo`, claves vacías en el repo) + variables de entorno `Brevo__ApiKey`/`Brevo__SenderEmail`/`Brevo__SenderName` seteadas solo en Test/Prod, nunca en archivos versionados (ver README, sección Brevo) — replicar el mismo patrón para Twilio/SendGrid.

**Estado de la cuenta de Twilio (relevado 2026-08-05):** la cuenta actual del responsable está en modo Trial (29 días restantes al momento de la revisión), con límites de 50 SMS/día y 100 emails/día. Esto es suficiente para desarrollo y pruebas internas, pero NO alcanza para producción con clientes reales — en meses de renovación (1,000-2,000 pólizas/2 meses, ver §4.1) se necesitarían hasta ~65 SMS/día, superando el límite del trial. Además, cuentas trial solo permiten enviar a números verificados manualmente, lo cual bloquea el envío a clientes reales sin upgrade.

**Criterio de avance definido:** no se le va a pedir al responsable el upgrade de la cuenta (pasar de Trial a plan pago) todavía. Se arranca la integración técnica base (§4.4, pasos 1-4) usando la cuenta trial actual, probando con números/emails verificados del equipo. El pedido de upgrade de cuenta se hace recién en la etapa previa a subir a producción — junto con la resolución del registro A2P 10DLC (ya documentado como prerrequisito más arriba en esta sección), ya que Twilio suele exigir ese registro como parte del proceso de habilitar envío de SMS a volumen real en EE.UU.

**Checklist de pasos previos a producción (a confirmar con el responsable cuando el flujo esté validado en desarrollo):**
1. Upgrade de la cuenta Twilio de Trial a plan pago.
2. Completar el registro A2P 10DLC (ya documentado como prerrequisito).
3. Verificación de dominio en SendGrid (SPF/DKIM) — ya documentado como prerrequisito, confirmar si ya se hizo.
4. Validar que los límites del plan pago elegido cubran el pico estacional de hasta ~65 SMS/día y volumen equivalente de emails.
5. Remover cualquier restricción de números verificados / prefijo de cuenta trial en los mensajes salientes.

**Plan de implementación (pasos, a ejecutar una vez estén los prerrequisitos):**
1. Crear una nueva implementación de `IEmailService` para SendGrid (ej: `SendGridEmailService`), siguiendo el mismo patrón que `BrevoEmailService`, sin reemplazarlo todavía (dejarlo coexistiendo, activable por configuración).
2. Crear un nuevo servicio `ISmsService` (o similar) con implementación `TwilioSmsService` — verificado en el código que no existe ninguna abstracción de SMS en el proyecto actualmente, se crea desde cero.
3. Agregar configuración en `appsettings.json` (Account SID, API Key, número de origen de Twilio) siguiendo el mismo patrón ya usado para Brevo (sección propia + `Twilio__...` como env vars en Test/Prod).
4. Crear un endpoint o servicio de prueba (ej: enviar SMS/email de test) para validar que las credenciales y el envío funcionan antes de integrarlo al flujo real de consentimiento.
5. Recién en este punto, una vez validada la integración base (pasos 1-4), conectar con el flujo real de consentimiento de §4.1 — ya no bloqueado por elección de proveedor (Dropbox Sign confirmado, 2026-08-05, ver §4.1); el disparo de la notificación puede implementarse contra la API de Dropbox Sign en cuanto la integración base de Twilio esté validada.

**Aclaración:** este plan es solo de documentación/planificación en esta iteración. No se implementó código todavía — el objetivo fue dejar el plan por escrito para poder ejecutarlo paso a paso en próximas iteraciones, empezando por confirmar los prerrequisitos con el responsable.

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

**✅ Resuelto (2026-07-27, §15.3)**: las 2490 filas migradas que habían quedado con `Customer.AgentId` en el fallback (primer User con Rol=Admin) se reasignaron una vez cargados los 41 agentes reales como `User` (§15.2), matcheando por nombre contra el reporte JSON original (campo `AgentFallbacks`) — 1178/1179 pólizas reasignadas (99.92%). Queda 1 caso puntual sin resolver: la póliza 381835 de Mariana Salvador Cruz, con `AgentId` todavía en fallback (ver §23.2, ítem 46) — no bloqueante.

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

**Punto abierto no bloqueante (2026-07-30) — "Is this member an applicant?" del titular en Health/ACA, nunca capturado:** investigado a raíz de una duda sobre la clasificación de Arnaldo Acosta (Customer `21380`, titular de una póliza Medicare) — el legacy le muestra "Is this member an applicant? Yes", lo que en un primer momento pareció contradecir que esté migrado como titular y no como dependiente. **Descartado: no hay ningún problema de clasificación.**
- Confirmado abriendo los xlsx reales (descomprimidos como zip, sin librería) que "Is this member an applicant?" es una pregunta genérica por persona, no un marcador exclusivo de rol de dependiente: en el archivo de Health/ACA (`report-policy-healthinsurance-index-800KC6.xlsx`) la columna se repite **9 veces** con el mismo texto de header — una para el titular (sin sufijo) y una por cada uno de los 8 bloques de dependiente (`ExcelReader.cs:24-29` ya documentaba este patrón de headers repetidos, igual que "First name"). Que el titular tenga "Yes" es esperable y no contradice nada.
- El archivo de **Medicare** (`report-policy-medicareinsurance-index-wdMz1Q.xlsx`, el que migró a Arnaldo) **no tiene esa columna en absoluto** — tampoco tiene "Number of applicants" ni ninguna columna de miembros/dependientes. Coincide con que `MedicareImporter.cs` no le pasa el delegate `extractDependents` a `ImportPipeline.PrepareAsync` (a diferencia de `HealthInsuranceImporter`). Lo que muestra el legacy para Arnaldo es un dato que existe en su pantalla en vivo pero que **no estaba en el export que usamos para migrar** — nunca tuvimos ese dato disponible, no es algo que se haya leído mal.
- **Hallazgo aparte, menor, sin prioridad**: para Health/ACA (el único de los 4 archivos que sí trae la columna repetida para todos), el valor del **titular** específicamente se pierde hoy en la migración — `CommonFieldsExtractor.ExtractCustomer` (usado para el titular, `suffix=""`) no lee `"Is this member an applicant?"` en ningún campo; solo `HealthDependentsExtractor.cs:52` la lee, y únicamente para los sufijos `_1`..`_8` (los bloques de dependiente) que alimentan `PolicyDependent.IsAplicante` (§1.6). No es un bug de clasificación — `IsAplicante` está modelado a propósito solo sobre dependientes, la tabla `PolicyDependent` no tiene fila para el titular — pero si algún día se quiere capturar también el "es aplicante" del titular, este es el punto exacto de origen del dato (columna sin sufijo del xlsx de Health/ACA). Sin implementar, solo registrado para no perder el hallazgo.

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

## 18. Rediseño de la tabla de Policies — nuevas columnas + scroll horizontal — ✅ Hecho (2026-07-29)

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
| 8 | Agency (badge) | `Customer.Agent.Agency` (`User.Agency`) | ✅ Hecho (2026-07-29) — mapeo en §18.9, badge visual en §18.10. |
| 9 | Agent (solo texto) | `CustomerResponseDto.AgentName` | ✅ Ya resuelto server-side, sin avatar — mismo criterio ya aplicado en Agentes (§17.5). |
| 10 | State/Province | `Customer.State` | ✅ Existe, ya en `CustomerResponseDto`. Confirmado que la columna real del xlsx de origen se llama literalmente `"State / Province"` en los 4 archivos de pólizas (`CommonFieldsExtractor.cs:38`). |
| 11 | Registration date | `Policy.CreatedAt` (**nuevo**) | ✅ Hecho (2026-07-29) — ver §18.7. |
| 12 | Renewal status | `Policy.RenewalStatus` (**nuevo**) | ✅ Hecho (2026-07-29) — ver §18.8. |

### 18.2 Explícitamente fuera de alcance por ahora: Tags

`Customer.Tags` ya existe de punta a punta (modelo, DTOs, formulario de alta/edición de Customer, §3.2) — no hace falta ningún cambio de schema para mostrarlo. Pero nunca se hizo backfill histórico: la columna `"Tags"` del xlsx de origen existe **solo en el archivo de Health** (1258 filas) y el importer nunca la lee — hoy prácticamente todos los customers migrados tienen `Tags = null`; solo tendrían valor los que alguien cargó/editó a mano después de la migración. **Se descartó agregar esta columna en esta ronda** — queda para revisar después si hace falta.

### 18.3 Mantener sin cambios

Íconos/botones/estilos actuales de acciones (🔍 lupa, ✏️ lápiz, 🗑 tacho, 💬 WhatsApp) — no se reemplazan por otro estilo aunque la referencia visual muestre algo distinto.

### 18.4 Layout: scroll horizontal, no "todo sin scroll"

A diferencia de Agentes (§17.5 — 6 columnas, se logró eliminar el scroll achicando avatares/fecha), acá con 12 columnas de datos + acciones se decidió que **sí** va a haber scroll horizontal dentro de la tabla (mismo contenedor `overflowX:auto` que ya usamos), igual que muestra la referencia. El fix del bug de `#root` en `index.css` (encontrado y corregido durante el rediseño de Agentes, §17) es lo que permite que ese scroll funcione bien — antes de ese fix, el contenido se desbordaba sin generar una barra de scroll usable.

### 18.5 Fechas sin hora

Effective date y Registration date se muestran ambas como `dd/mm/aaaa`, sin hora — mismo criterio que ya se aplicó a Registration date en Agentes (§17).

### 18.6 Orden de implementación sugerido cuando se retome

1. ~~**`Policy.CreatedAt`**~~ ✅ Hecho — migración + backfill vía `MIN(PolicyHistory.ChangedAt)`. Ver §18.7.
2. ~~**`Policy.RenewalStatus`**~~ ✅ Hecho — migración + backfill vía `--backfill-renewal-status`. Ver §18.8.
3. ~~**`CustomerResponseDto.AgentAgency`**~~ ✅ Hecho — mapeo directo en el controller, sin migración. Ver §18.9.
4. ~~**Frontend**~~ ✅ Hecho — las 12 columnas en el orden definido, Status como badge, Agency/Agent sin avatar, scroll horizontal, fechas sin hora. Ver §18.10.
5. ~~Build + lint + verificación en navegador~~ ✅ Hecho, ver §18.10/§18.11 — más 2 hallazgos de esta misma sesión (menú de acciones ⋮ y un bug de layout preexistente) documentados abajo.

### 18.7 `Policy.CreatedAt` implementado — ✅ Hecho (2026-07-29)

Primer paso del orden sugerido en §18.6. Migración `20260729153240_AddPolicyCreatedAt`: columna `datetime2 NOT NULL`, sin nullable — sentinel `0001-01-01` solo transitorio durante el `ALTER TABLE`, reemplazado por el backfill dentro de la misma migración.

- Backfill vía `UPDATE Policies SET CreatedAt = MIN(PolicyHistories.ChangedAt) WHERE PolicyId = Policies.Id` (join contra la tabla real `PolicyHistories`, plural — el nombre de tabla sigue el `DbSet<PolicyHistory> PolicyHistories` de `AppDbContext`, no el nombre de la clase singular; primer intento de la migración falló con "Invalid object name 'PolicyHistory'" hasta corregirlo). Sin re-leer ningún xlsx.
- Red de seguridad agregada por las dudas (no disparó en la práctica): `CreatedAt = UpdatedAt` para cualquier póliza sin ningún `PolicyHistory` — no aplicó a ninguna fila real, las 1211 pólizas ya tenían su historial inicial.
- `PoliciesController.Create` ahora también setea `CreatedAt = DateTime.UtcNow` (mismo criterio que `UpdatedAt`, que ya se seteaba ahí) — una póliza nueva no depende del backfill. `Update` no lo toca (solo se setea al crear).
- `PolicyResponseDto.CreatedAt` agregado, mapeado en `PoliciesController.ToResponse` y en `CustomersController.GetPoliciesForCustomer` (comparten el mismo DTO, §5.2).
- Verificado con sqlcmd (1211/1211 pólizas con `CreatedAt` real, 0 en sentinel, rango `2025-09-24` a `2026-07-22`) y con curl (`GET /api/policies` devuelve `createdAt` con fecha real distinta de `updatedAt`; `POST /api/policies` de prueba confirma `createdAt` = fecha actual del alta, registro de prueba borrado después). `dotnet build` limpio (0 warnings/0 errors).

### 18.8 `Policy.RenewalStatus` implementado — ✅ Hecho (2026-07-29)

Segundo paso del orden sugerido en §18.6. Migración `20260729154446_AddPolicyRenewalStatus`: columna `nvarchar(100) NULL`, sin backfill embebido en la migración (a diferencia de `CreatedAt`, acá el dato vive en un xlsx externo, no en la base).

- **Riesgo de matching resuelto sin heurística nueva**: en vez de comparar el texto crudo de `"Policy number"` contra `Policy.PolicyNumber` (que hubiera fallado para +90% de las filas, dado que ese campo llega vacío/basura en el origen — ver `PolicyNumberResolver`), el runner `--backfill-renewal-status` (`RenewalStatusBackfillRunner.cs`, nuevo en `WholeCareInsurance.Migration`) reutiliza el mismo `HealthInsuranceImporter`/`ImportPipeline` que ya migró los datos reales (§7): vuelve a preparar los mismos grupos consolidados (misma resolución de Customer/Aseguradora, dentro de una transacción que se revierte siempre — nunca escribe nada por sí sola) y toma el `PolicyNumber` + `"Renewal status"` ya resueltos de la fila vigente de cada grupo. El match contra la `Policy` real en la base termina siendo exacto por construcción, no por heurística propia.
- `HealthInsuranceImporter.cs` ahora también popula `policy.RenewalStatus = row.GetString("Renewal status")` — cubre tanto el backfill de este runner como cualquier futura re-importación del archivo de Health.
- `PolicyResponseDto.RenewalStatus` agregado, mapeado en `PoliciesController.ToResponse` y `CustomersController.GetPoliciesForCustomer` (mismo criterio que `CreatedAt`, §18.7).
- **`--dry-run` corrido primero** (obligatorio, según lo acordado): 1185/1185 pólizas de Health re-preparadas matchearon 100% contra la base, sin huecos — mejor cobertura de la esperada (el riesgo documentado en el plan original no se materializó). Distribución real de valores: 1183 `Pending` + 2 `Renewed` (verificado independientemente contra el XML crudo del `.xlsx`, no es un artefacto del código).
- **`--commit --confirm` aplicado** tras confirmación explícita del responsable. Verificado con sqlcmd post-commit:

  | Type | Total | NULL | Pending | Renewed |
  |---|---|---|---|---|
  | Health Insurance (ACA) | 1185 | 0 | 1183 | 2 |
  | Life Insurance | 2 | 2 | 0 | 0 |
  | Medicare | 8 | 8 | 0 | 0 |
  | Supplemental Plans | 16 | 16 | 0 | 0 |

  1185/1185 pólizas de Health quedaron con `RenewalStatus` no nulo; las 26 pólizas de Medicare/Life/Supplemental (que no tienen esta columna en su xlsx de origen) confirmadas sin tocar, siguen en `NULL` como es esperado. `dotnet build` limpio (0 warnings/0 errors) en `WholeCareInsurance.api` y `WholeCareInsurance.Migration`.

### 18.9 `CustomerResponseDto.AgentAgency` implementado — ✅ Hecho (2026-07-29)

Tercer paso del orden sugerido en §18.6. Sin migración — mapeo directo, mismo patrón que `AgentName`.

- `CustomerResponseDto.AgentAgency` (nuevo, nullable) agregado junto a `AgentName`.
- `CustomersController.ToResponse`: `AgentAgency = c.Agent?.Agency` — `Agent` ya viene cargado vía `.Include(c => c.Agent)` en `CustomerService` (mismo `Include` que ya sostiene `AgentName`), sin necesidad de tocar ninguna query.
- Es el único call site que construye `CustomerResponseDto` con datos de `Agent` (confirmado por búsqueda en el controller).
- Verificado con curl (`GET /api/customers`): `agentAgency` coincide con `agentAgency` real del agente asignado (ej. "Preventive Health Insurance", "Whole Care Insurance Group llC") y viene `null` en los customers sin agente asignado, igual que `agentName`. `dotnet build` limpio (0 warnings/0 errors).

### 18.10 Frontend — las 12 columnas + badges + scroll horizontal — ✅ Hecho (2026-07-29)

Cuarto paso del orden sugerido en §18.6. `Policies.jsx` reemplaza las 8 columnas anteriores (Policy Number, Type, Insurance Company, Status, Period, Premium, Customer, Actions) por las 12 definidas en §18.1, en ese orden: Customer, Contact, Plan, Type, Applicants, Status (badge), Effective date, Agency (badge), Agent, State/Province, Registration date, Renewal status — más Actions al final (fuera del conteo de "12").

- **Status como badge**: nuevo `src/utils/policyStatusStyle.js`, con `POLICY_STATUSES` extraído de `Dashboard.jsx` (que ahora lo importa desde ahí en vez de tener su propia copia — single source of truth para el orden categórico) y una paleta pastel (par fondo claro + texto oscuro) derivada del mismo orden que ya usan los gráficos de torta del Dashboard, mismo criterio que `agencyStyle.js` (§17).
- **Agency como badge**: reusa `agencyStyle.js` tal cual, sin cambios — mismo estilo visual que ya tiene Agentes.
- **Fechas sin hora**: nuevo `src/utils/formatDate.js` (`formatDateOnly`, `dd/mm/aaaa`) extraído de `Agentes.jsx` (que ahora también lo importa en vez de tener su propia copia) — usado en Effective date y Registration date.
- **Agency/Agent/State sin avatar**: derivados de `getCustomer(p.customerId)` (ya cargado por `Policies.jsx` vía `/api/customers`, sin endpoint nuevo) — `customer.agentAgency`/`customer.agentName`/`customer.state`, con `"-"` cuando no hay dato.
- **Traducción de Renewal status**: nuevo grupo `policyRenewalStatus` en `enums.json` (es/en) — los 2 valores reales migrados (`Pending`→"Pendiente", `Renewed`→"Renovado"), con fallback al valor crudo si aparece uno no contemplado (mismo mecanismo que el resto de `translateEnum`).
- **Scroll horizontal**: sin cambios de fondo — seguía usando el mismo contenedor `overflowX:auto` que ya tenía la tabla; ver §18.11 para el bug de layout que este mismo rediseño expuso (no introdujo).
- Traducciones nuevas en `en/policies.json`/`es/policies.json` (namespace `table`): `customer`, `contact`, `plan`, `applicants`, `effectiveDate`, `agency`, `agent`, `stateProvince`, `registrationDate`, `renewalStatus` — se sacaron `policy`, `insuranceCompany`, `period`, `premium` (columnas que salieron de la tabla; los datos siguen disponibles en el modal de detalle).
- Verificado con `npm run build`/`npm run lint` (mismos 6 problemas preexistentes de §20, ninguno nuevo) y por el responsable directamente en el navegador (sin extensión de Chrome conectada en esta sesión para captura automática).

### 18.11 Hallazgos fuera del plan original, mismo día — ✅ Hecho (2026-07-29)

Dos piezas de trabajo que no estaban en el plan aprobado de §18, pedidas/encontradas durante la verificación en navegador del punto anterior:

**Menú de acciones (⋮) reutilizable** — a pedido del responsable, para reemplazar los íconos de acciones sueltos (🔍✏️🗑💬) por un menú desplegable en las 3 pantallas con tabla real (Policies, Customers, Agentes — `InsuranceCompanies` quedó afuera a propósito: no es una tabla sino una grilla de tarjetas, y tiene una sola acción, "Editar", por lo que un menú de 3 puntitos ahí solo agregaría un click sin beneficio, decisión confirmada con el responsable).
- `src/components/ActionsMenu.jsx` (nuevo, mismo criterio que `Modal.jsx`: un solo componente, las 3 pantallas lo consumen). API: `items={[{ icon, label, onClick? , href?, external?, disabled? }]}`.
- **Primer uso de `createPortal` en este código**: el dropdown se renderiza como hijo directo de `document.body` (no anidado en el `<td>`), posicionado con `position: fixed` calculado desde `getBoundingClientRect()` del botón `⋮` — así el contenedor `overflowX:auto` de la tabla nunca lo recorta ni lo desalinea, sea cual sea el scroll horizontal activo. Se mide el tamaño real del menú antes de mostrarlo (renderiza off-screen, mide en un `useLayoutEffect`, reposiciona) en vez de asumir un ancho/alto fijo, y se flipea a la izquierda/arriba si no entra cerca de un borde de la pantalla.
- Cierre por click afuera (`mousedown`), `Escape`, scroll (capturado en fase de captura para detectar también el scroll horizontal interno de la tabla) y al ejecutar cualquier acción.
- Acciones por pantalla (mismas de antes, solo cambia la presentación — íconos + texto, no solo íconos con tooltip, porque una vez abierto el menú el texto es más legible que forzar un hover): Policies (🔍 Detalle, ✏️ Editar, 🗑 Eliminar, 💬 WhatsApp condicional), Customers (✏️ Editar, 🔍 Detalle, 💬 WhatsApp condicional, 🗑 Eliminar), Agentes (✏️ Editar, 🔍 Detalle, 🗑/♻️ Activar-Desactivar, respeta el `disabled` mientras hay un toggle en curso).
- Verificado con `npm run build`/`npm run lint` (mismos 6 problemas preexistentes) y por el responsable en las 3 pantallas.

**Bug de layout preexistente en `AppLayout.jsx`, expuesto (no causado) por el rediseño de 12 columnas** — el responsable reportó que el Header (selector de Período/idioma/perfil) se veía cortado en Policies después de agregar el `ActionsMenu`; se investigó antes de tocar nada (a pedido explícito) y se descartó el portal como causa (solo monta con el menú abierto, el bug se reproducía en frío) — la causa real era anterior y ortogonal al `ActionsMenu`.
- **Causa**: el `<div style={{ flex: 1, display: "flex", flexDirection: "column" }}>` de `AppLayout.jsx` (envuelve Header + `main`) no tenía `min-width` fijado. Los flex items tienen `min-width: auto` por default — su ancho mínimo se calcula en base al `min-content` de su contenido más ancho, salvo que se pise explícitamente. Al no haber ningún `min-width:0`/`overflow` en la cadena hasta el wrapper `overflowX:auto` de la tabla, el `min-content` de la tabla de 12 columnas (la más ancha de las 4 pantallas) forzaba a crecer a **todo el contenedor**, Header incluido (es hermano de `main` dentro del mismo flex item). El `overflowX:auto` de la tabla nunca llegaba a activarse como límite local porque su contenedor ya venía sobredimensionado — el scroll que se generaba era de página completa, arrastrando al Header con él.
- Bug latente desde antes de esta sesión (no introducido por el rediseño ni por `ActionsMenu`) — nunca se había manifestado porque ninguna tabla anterior (Agentes/Customers) llegaba a superar el ancho del viewport.
- **Fix**: una línea, `minWidth: 0` en ese mismo `div` de `AppLayout.jsx` — deja que se achique al ancho realmente disponible (`100vw` menos el ancho del Sidebar), con lo que el `overflowX:auto` de la tabla pasa a ser el único límite real de scroll horizontal.
- Fix en un archivo de layout compartido por las 4 pantallas — no debería cambiar nada visualmente en Customers/Agentes/InsuranceCompanies (sus tablas no llegan a superar el viewport), pero de paso las blinda contra el mismo problema si alguna crece en el futuro.
- Verificado con `npm run build`/`npm run lint` limpios y por el responsable en el navegador: Header fijo al scrollear Policies horizontalmente, sin cambios en las otras 3 pantallas.

---

## 20. Deuda técnica — ESLint `react-hooks/set-state-in-effect` — ✅ Hecho (2026-07-29): 6/6 casos resueltos (5 de fetching vía `queueMicrotask` + 1 de estado derivado vía extracción de subcomponente, §20.3) — sin ningún caso pendiente

Encontrados originalmente por el responsable en el Error List de Visual Studio (`Dashboard.jsx:189`, marcado como "Error", no "Warning"). Confirmado en su momento que no era un problema de configuración editor/terminal — mismo error real en ambos lados (`eslint.config.js` hereda `reactHooks.configs.flat.recommended` del plugin `eslint-plugin-react-hooks@7.1.1` tal cual, que trae la regla en `"error"`). `npm run build` nunca se vio afectado — `vite build` no ejecuta ESLint.

**Origen: preexistente desde `390c3e0`** ("feat: frontend del Dashboard (§9)", 2026-07-27), confirmado con `git blame`.

### 20.1 Diagnóstico al retomar (2026-07-29): no eran 5 casos iguales, eran 6, y 2 patrones distintos

Al retomar, se le pidió al asistente confirmar el patrón exacto de cada archivo antes de tocar nada (en vez de asumir que los 5 eran iguales):

- **5 (de los 4 reportados + 1 silencioso) comparten el mismo patrón de fetching**: una función `async loadXxx()` reutilizable cuya primera línea (antes de cualquier `await`) es `setLoading(true)`, invocada directo desde un `useEffect(() => { loadXxx(); }, [])` de solo-montaje:
  - `Dashboard.jsx:189` (`loadDashboard`)
  - `Customers.jsx:67` (`loadCustomers`)
  - `Agentes.jsx:70` (`loadUsers`)
  - `InsuranceCompanies.jsx:34` (`loadCompanies`)
  - **`Policies.jsx:748` (`loadData`) — 6to caso real, no reportado por el linter.** Mismo patrón exacto (`setLoading(true)` antes del primer `await`, llamado desde un `useEffect` de `[period]`), verificado armando reproducciones aisladas del patrón (con parámetros default y con el mismo guard `if (!token) return;` que tiene el efecto real) — ambas SÍ fueron marcadas por el linter en aislamiento, así que no es el guard ni los defaults lo que lo esconde. No se pudo aislar la causa exacta de por qué el linter no lo reporta en el archivo real (2500+ líneas) — sospecha: algún límite/bail-out del analizador del compilador de React ante un componente tan grande, sin confirmar. Se corrigió igual, por ser el mismo defecto conceptual.
- **`Policies.jsx:194` (el 5to error originalmente reportado) es un patrón DISTINTO** — no es fetching, es el efecto que sincroniza `titularLifeForm` con el Customer titular seleccionado (`useEffect(() => { const c = getCustomer(customerId); setTitularLifeForm({...}); }, [customerId, customers])`). Es el caso de libro de ["adjusting state when a prop changes"](https://react.dev/learn/you-might-not-need-an-effect) — el fix recomendado no es diferir la llamada, es darle `key={customerId}` a esa sección del formulario para que React la remonte con estado fresco, lo que requiere extraerla a un subcomponente propio (ver §20.3, ✅ hecho).

### 20.2 Fix aplicado a los 5 casos de fetching — ✅ Hecho (2026-07-29)

Dos opciones evaluadas: (A) sacar `setLoading(true)` de la función compartida y ponerlo explícito en cada handler que dispara una recarga manual (arquitectónicamente más prolijo, pero toca ~15 call-sites entre los 4 archivos, con riesgo de que a alguno se le olvide el `setLoading(true)` y deje de mostrar el spinner ahí); (B) diferir la llamada un microtask (`queueMicrotask`), que rompe la cadena de reachability síncrona que chequea el linter sin cambiar ningún comportamiento perceptible. **Decisión del responsable: opción B** — menor riesgo, no depende de verificar cada call-site en navegador uno por uno. La opción A queda para el día que se refactorice alguna de esas pantallas por otro motivo.

- Cambio de una línea en cada uno de los 5 `useEffect` de montaje: `loadXxx()` → `queueMicrotask(loadXxx)` (o `queueMicrotask(() => loadXxx(...))` donde hace falta pasar argumentos o llamar más de una función).
- Verificado con `npm run build` (limpio) y `npm run lint`: bajó de `✖ 6 problems (5 errors, 1 warning)` a `✖ 2 problems (1 error, 1 warning)` — el único error remanente es `Policies.jsx:194` (titularLifeForm, ver §20.3), el warning es el de `exhaustive-deps` en `Agentes.jsx:72` (preexistente, sin relación con esta regla).

### 20.3 Extraer sección "Datos Life Insurance del titular" a subcomponente — ✅ Hecho (2026-07-29)

Cierra el último caso real de `react-hooks/set-state-in-effect` (`Policies.jsx:192-211`, el efecto que sincronizaba `titularLifeForm`). La sección "Datos Life Insurance del titular" (~130 líneas de JSX + su estado: `titularLifeForm`, `titularLifeError`, `savingTitularLife`, `handleTitularLifeField`, `handleSaveTitularLife`) se extrajo a un subcomponente propio nuevo, `src/components/TitularLifeSection.jsx`, montado con `key={customerId}` — React resetea su estado automáticamente al cambiar de titular, sin necesitar un efecto que lo haga a mano. El `useEffect` que sincronizaba `titularLifeForm` a mano se eliminó por completo (no se diferió, a diferencia del fix de los 5 casos de fetching de §20.2).

**De paso** (investigado y confirmado con el responsable durante la misma sesión): el dropdown de Customer titular no tenía ninguna razón funcional para estar habilitado en modo edición — era el mismo campo del formulario de creación, reusado sin deshabilitar. Cambiarlo a mitad de edición dejaba inconsistente el filtro de "excluir al titular de la lista de dependientes". Se deshabilitó el dropdown de Customer al editar una póliza ya guardada (el backend sí permite reasignar el `CustomerId` de una póliza existente, pero no hay ningún flujo de UI que lo necesite).

Implementado en el commit `93ef200` ("fix: extraer TitularLifeSection + deshabilitar Customer en edición de póliza (§20.3)"). **Verificado con `npm run lint`: `✖ 1 problem (0 errors, 1 warning)`** — el único error real de `react-hooks/set-state-in-effect` quedó resuelto; el warning remanente es el preexistente de `exhaustive-deps` en `Agentes.jsx:72`, sin relación con esta regla. §20 queda 100% cerrado, sin ningún caso pendiente.

---

## 22. Dashboard — "Additional statistics": bug de normalización de mayúsculas + plan de gráficos tipo torta — ✅ Hecho (2026-07-30): backfill de datos (§22.4), freno al input de City (§22.5 Parte A) y gráficos tipo torta (§22.5 Parte B)

Encontrado por el responsable en el widget "Additional statistics" (By Insurance Company / By County / By City, §9.3): "Nashville" (114) y "NASHVILLE" (30) aparecen como entradas separadas cuando deberían ser una sola ciudad — mismo problema visible en Antioch/ANTIOCH y Smyrna/SMYRNA. Se pidió investigar el alcance real (no asumir que son solo esos 3 casos) antes de proponer una solución, y documentar además un plan de gráficos tipo torta/dona para reemplazar las 3 listas actuales — sin implementar nada de ninguna de las dos partes todavía.

### 22.1 Parte 1 — Origen del dato y alcance real del bug de case

**Origen del dato — confirmado por código, no es corrupción de ningún paso de la migración**:
- `Customer.City`/`Customer.County` vienen del xlsx de origen tal cual, sin normalizar (`CommonFieldsExtractor.cs:39-40`, `row.GetString("City")` — `ExcelRow.GetString` solo hace `.Trim()`, ningún cambio de case).
- En el frontend, `City` es un `<input>` de texto libre (`CustomerFormFields.jsx:121`, sin dropdown ni normalización) — **riesgo activo, no solo histórico**: cualquier alta/edición nueva puede introducir otra variante de case. `County`, en cambio, es un `<select>` constreñido al dataset fijo `usCounties.json` (`CustomerFormFields.jsx:126`).
- **Causa técnica exacta de por qué el Dashboard los separa**: la base tiene collation `SQL_Latin1_General_CP1_CI_AS` (**Case-Insensitive**) tanto a nivel de base como en las columnas `City`/`County`/`State` — confirmado con `sys.columns` + `DATABASEPROPERTYEX(..., 'Collation')`. Bajo esa collation, SQL Server trata `'Nashville' = 'NASHVILLE'` como verdadero para comparaciones/`GROUP BY`/`DISTINCT` (aunque el texto se guarda tal cual se escribió, sin normalizar el byte real). Pero `DashboardService.GetStats` (`DashboardService.cs:114-140`) primero hace `.ToListAsync()` (trae los strings crudos a memoria) y **recién ahí** aplica `.GroupBy(p => p.City)` — LINQ-to-Objects en memoria usa comparación **ordinal (case-sensitive)** por default, a diferencia de SQL. Ese desfasaje de collation (DB case-insensitive vs. agrupación en memoria case-sensitive) es la causa raíz exacta: no es un bug de un solo campo, es estructural a cómo está armado ese endpoint.

**Alcance real, medido contra la base completa (no solo el top 10 visible)**:

| Campo | Grupos canónicos con duplicado por case | Filas afectadas | Total con dato |
|---|---|---|---|
| `Customer.City` | **57** (de 304 valores distintos) | **914 / 1179 clientes con City (77.5%)** | 1179 |
| `Customer.County` | 0 | — | 1179 |
| `InsuranceCompany.Name` | 0 | — | — |
| `Customer.State` | 0 | — | — |
| `User.Agency` | 0 | — | — |
| `Policy.RenewalStatus` | 0 | — | — |
| `Customer.Occupation` | 8 (no usado hoy en ninguna pantalla/agrupación) | no medido | — |
| `Customer.MaritalStatus` | 0 | — | — |

City es **el único campo con el problema activo** entre los que efectivamente se usan en pantalla, y su alcance es mucho mayor al de los 3 ejemplos reportados (57 grupos, no 3; 914 de 1179 clientes con ciudad caen en algún grupo duplicado). El motivo por el que County/InsuranceCompany/State/Agency/RenewalStatus dan 0 es consistente con el código: todos son `<select>` controlados (dataset fijo, tabla propia con validación de duplicado, o el enum de 2 valores del backfill de §18.8) — ninguno permite texto libre salvo `City` (y `Occupation`, que hoy no se usa para agrupar en ningún lado, así que no es urgente pero queda anotado).

Ejemplos reales (top 5 por impacto, de los 57 grupos): NASHVILLE (3 variantes, 142 filas), ANTIOCH (2 variantes, 123 filas), SMYRNA (3 variantes, 108 filas), MURFREESBORO (3 variantes, 91 filas), ORLANDO (3 variantes, 45 filas).

### 22.2 Parte 1 — Propuesta de solución: normalizar el dato, no la query

Dos caminos evaluados:

- **(A) Normalizar el dato en la base** (`UPDATE Customers SET City = <case canónico>` una sola vez + capar el `<input>` del frontend para que no se pueda volver a romper). **Pros**: arregla la causa de raíz para toda la app, no solo el Dashboard — cualquier pantalla/export/reporte futuro que use `City` ya lo ve limpio; cierra la fuga hacia adelante (el `<input>` libre de `CustomerFormFields.jsx:121` sigue sumando variantes nuevas con cada alta/edición si no se toca). **Contras**: hay que decidir un case canónico caso por caso — un Title Case automático a ciegas puede arruinar mayúsculas internas legítimas (ej. "La Vergne", "Fort Lauderdale", "Mt Juliet") — y correr un `UPDATE` masivo sobre datos reales; técnicamente de bajo riesgo (`City` no es FK ni participa del matching de la migración, ver §22.2b), pero toca 914 filas reales.
- **(B) Normalizar solo en la query del Dashboard** (`GROUP BY UPPER(p.City)` o equivalente, sin tocar el dato real). **Pros**: cambio mínimo, un solo archivo (`DashboardService.cs`), sin migración de datos, sin tener que decidir un case canónico. **Contras**: el dato real sigue sucio y el `<input>` sigue sin freno — cualquier funcionalidad futura que use `City` (hoy ninguna otra, ver §22.2b) tendría que repetir la normalización, y la base sigue creciendo con variantes nuevas indefinidamente. Es un parche sobre el síntoma, no sobre la causa.

**✅ Decisión confirmada del responsable: opción (A).** El hallazgo de §22.2b (impacto contenido al Dashboard, sin urgencia) baja la prioridad pero no cambia la decisión — se prefiere cerrar la causa de raíz antes de que se abra en algún otro lugar, en vez de parchear la query y depender de acordarse de replicarlo. **Prioridad normal, no bloqueante** — se implementa cuando haya lugar en el roadmap, no antes de otro trabajo en curso.

**Pasos para cuando se implemente** (ninguno hecho todavía):
1. Mostrar la lista completa de los 304 valores distintos de `City` antes de decidir el `UPDATE` — no aplicar Title Case automático a ciegas. Casos con mayúsculas internas legítimas ("La Vergne", "Fort Lauderdale", "Mt Juliet", posibles siglas) se revisan a mano o con una regla curada, no una función genérica de un solo paso.
2. `UPDATE` masivo sobre los 914 registros afectados, con backup previo (mismo criterio que otros `UPDATE` masivos de este proyecto — ver `D:\backups\WholeCareInsuranceDb_pre_migracion.bak` de §7 como precedente).
3. Frontend: agregar algún freno al `<input>` libre de City en `CustomerFormFields.jsx:121` — evaluar entre (a) un `<datalist>`/autocomplete con las ciudades ya existentes (reduce variantes nuevas sin forzar una lista cerrada que podría no cubrir una ciudad real que falte) o (b) normalizar a Title Case en el blur/submit del formulario antes de guardar (más simple, no depende de tener el dataset de ciudades ya cargado en el cliente). A decidir cuál al implementar.
4. Aplicar la misma normalización a los datos de Parte 2 (gráficos) una vez resuelto esto — ver §22.3.

**Fuera de este alcance, anotado para más adelante**: `Occupation` tiene el mismo tipo de problema (8 grupos duplicados por case) pero menor prioridad — no se usa para agrupar/mostrar en ningún lado hoy, así que no hay urgencia ninguna.

### 22.2b Parte 1 — Alcance del impacto: ¿afecta a algo más que el Dashboard? — Investigado, impacto contenido

A pedido del responsable, se investigó si el mismo problema de case en `City` afecta a algo más allá del widget del Dashboard (búsquedas/filtros en Customers, otros reportes, matching de la migración) antes de decidir la prioridad. **Confirmado: no — es el único lugar de toda la aplicación que agrupa/compara por `City`.**

- `grep` completo de `City` en todos los Controllers/Services del backend: solo aparece en `CustomersController`/`UsersController` pasando el valor tal cual (create/update/response) — nunca se filtra, busca ni agrupa por `City` ahí.
- `CustomerService.Search(page, pageSize)` no tiene ningún filtro además de paginado (ni siquiera por nombre) — el listado de Customers no tiene forma de buscar/filtrar por ciudad hoy.
- `PolicyService.Search` filtra por `firstName`/`lastName`/`policyNumber`/`status`/`type`/`insuranceCompanyId`/`period` — `City` no es uno de los filtros.
- Único `GroupBy` sobre `City` en todo `WholeCareInsurance.api`: `DashboardService.cs:136` (de 6 `GroupBy` totales en el proyecto, los otros 5 son sobre Status/Type/InsuranceCompany/County/PolicyId, ninguno en otro archivo).
- Frontend: `city`/`City` solo precarga formularios de alta/edición (`Customers.jsx`, `Policies.jsx` sección titular Life Insurance) — nunca se usa para buscar, filtrar, ni listar valores distintos en ningún otro lugar.
- `EntityMatcher.ResolveCustomerAsync` (script de migración, §7.1): el matching de Customers para deduplicar usa SSN + Nombre/Apellido/Fecha de nacimiento — `City` se guarda como dato pero no participa del matching, así que las variantes de case tampoco causaron altas duplicadas de Customer por este motivo.

**Conclusión**: el 77.5% es un número real y grande, pero el impacto funcional está contenido al widget "Additional statistics" — no hay búsquedas rotas, no hay filtros afectados, no hay duplicados de Customer causados por esto. Es la base de la decisión de tratarlo con **prioridad normal, no bloqueante** (§22.2).

### 22.3 Parte 2 — Gráficos tipo torta/dona para "Additional statistics" (condicionado a que se resuelva la Parte 1 primero)

Pedido: reemplazar las 3 listas actuales (`NameCountList`, `Dashboard.jsx:96-118`) por el mismo componente de torta+leyenda que ya existe y se usa para Tipo/Status (`ChartWithLegend`/`DonutChart`, `Dashboard.jsx:29-94`) — mismo componente, sin reinventar nada nuevo, solo alimentarlo con datos distintos.

**Por qué depende de la Parte 1**: si se grafica antes de normalizar el case, "Nashville" y "NASHVILLE" van a aparecer como 2 porciones separadas en la torta (peor que en una lista, porque además de duplicar la cuenta, 2 porciones finitas para lo que visualmente debería ser una sola ciudad rompe la lectura de proporciones de un vistazo, que es la razón misma de usar un gráfico de torta).

**Problema de legibilidad a resolver (ya señalado por el responsable)**: `County` y `City` hoy muestran "y N más" (101 más, 295 más — con la normalización de Parte 1 ya aplicada (§22.4), `City` quedó en 191 valores distintos, sigue siendo demasiado para una torta legible). `InsuranceCompany` tiene como máximo 31 valores posibles (§1.5) pero en la práctica los datos reales rondan bastante menos — a confirmar cuántas aseguradoras distintas aparecen realmente con pólizas antes de decidir si necesita el mismo tratamiento.

**Propuesta**: top 9 + "Otros" agrupado (10 porciones como máximo, mismo límite que ya usa `NameCountList` hoy con su `limit = 10`, así que es un criterio ya validado en esta misma pantalla, no uno nuevo) — para los 3 (`ByInsuranceCompany`/`ByCounty`/`ByCity`), por consistencia, aunque `InsuranceCompany` probablemente nunca llegue a necesitar el agrupamiento en la práctica. "Otros" se pinta con un gris neutro fijo (no un color de la paleta categórica, para no confundirlo visualmente con una categoría real) y va sin criterio de orden especial (siempre al final, sea cual sea su tamaño). Reutiliza `CATEGORICAL_COLORS`/`ChartWithLegend` tal cual existen hoy — no hace falta ningún componente nuevo, solo una función chica que colapse `items.slice(9)` en un bucket "Otros" antes de pasarlo a `ChartWithLegend`.

**No implementado todavía** — la Parte 1 ya está resuelta (§22.4), queda pendiente que el responsable confirme la propuesta del top 9 + Otros antes de implementar.

### 22.4 Parte 1 — Backfill de `Customer.City` aplicado — ✅ Hecho (2026-07-29)

Backfill completo de la opción A confirmada en §22.2. **Backup previo**: `D:\backups\WholeCareInsuranceDb_pre_city_normalization.bak` (10.6 MB, `BACKUP DATABASE` completo antes de tocar cualquier fila).

**Aplicado en un solo lote, revisado y confirmado por el responsable antes de cada tanda**:
- **323 filas**: los 57 grupos de duplicado por case (914 filas totales en esos grupos, pero solo 323 tenían un case distinto al canónico — el resto ya estaba bien escrito) + los typos/abreviaturas confirmados a mano cruzando `ZipCode` contra un zip real conocido (ej. `Nasville`→Nashville confirmado por zip 37207; `CASELBERRY`→Casselberry por zip 32707 compartido con `CASSELBERRY`) — todos con Title Case como canónico.
- **1 fila**: `LAND O<carácter corrupto>LAKES` → "Land O' Lakes", matcheada por patrón `LIKE 'LAND O_LAKES'` (comodín de 1 carácter) en vez de un string literal, precisamente para no depender de reproducir un byte corrupto que el cliente SQL de esta sesión no podía representar de forma confiable.
- **2 filas — caso "zip cargado en el campo City"**: `City = "37167"` → "Smyrna" (zip real confirmado externamente, [city-data.com](https://www.city-data.com/zips/37167.html)); `City = "37217"` → "Nashville" **y además** `ZipCode` (que en esa fila decía literalmente el string `"Nashville"`, los dos campos estaban invertidos) → "37217" ([zip-codes.com](https://www.zip-codes.com/zip-code/37217/zip-code-37217.asp)).

**Hallazgos que aparecieron durante la verificación posterior al lote principal (no estaban en el análisis original, se fueron resolviendo a medida que se encontraban, cada uno confirmado antes de aplicar)**:
- **`La�Vergne` (Id 20014)**: el responsable lo investigó directamente en SSMS/Azure Data Studio con `ASCII(SUBSTRING(...))` — no era un apóstrofo corrupto como se sospechaba, era un **non-breaking space (código 160)** en vez de un espacio normal (32) entre "La" y "Vergne". Corregido con `REPLACE(City, NCHAR(160), ' ')`. Se corrió además un `SELECT` buscando `NCHAR(160)` en toda la columna `City` antes de asumir que era el único caso — confirmado que sí lo era.
- **`VERGNE` (Id 20137, sin "La" adelante, mayúsculas)**: mismo zip (37086) que La Vergne — confirmado como el mismo lugar, corregido a "La Vergne".
- **`LA VERGE` (Id 19925)**: mismo zip (37086) — este typo **se había detectado en el análisis automático por distancia de edición pero no llegó a la tabla final mostrada para aprobación** (error del asistente al compilar la tabla). Encontrado recién en la verificación de "mismo ZipCode, distinto City" que se corrió como chequeo cruzado adicional, no como parte del plan original — corregido a "La Vergne" apenas se detectó el faltante.
- **4 casos de "nombre de estado en vez de ciudad"** (`Wisconsin`→Pewaukee zip 53072, `Oklahoma`→Oklahoma City zip 73120, `CALIFORNIA`→Chula Vista zip 91911, `NEW JERSEY`→Lakewood zip 08701) — categoría nueva, no contemplada en el análisis original de "raros" (case/typo/abreviatura/encoding). Encontrados en el mismo chequeo cruzado de "mismo ZipCode, distinto City". Cada ciudad real confirmada por fuente externa (city-data.com/zip-codes.com) antes de aplicar; el de Lakewood además coincidía con un cliente ya existente en la base con ese mismo zip y `City = "Lakewood"`.
- **Efecto secundario del propio fix anterior**: al corregir `Wisconsin`→"Pewaukee", se expuso un duplicado de case nuevo (`PEWAUKEE`, 2 clientes, sin variante de case previa así que nunca había aparecido en los 57 grupos originales) — corregido también, misma categoría que el resto del lote.

**El chequeo de "mismo `ZipCode`, distinto `City`" resultó ser la herramienta más efectiva para encontrar casos no detectados por el análisis de texto/distancia de edición** — la mayoría de los resultados de ese chequeo eran solapamientos reales de zona (ej. zip 33025 cubre Miami/Miramar/Pembroke Pines, zip 37013 cubre Antioch/Cane Ridge/Nashville — vecindarios reales, no errores), pero fue el método que sacó a la luz los 3 típos de "La Vergne" faltantes y los 4 de estado-como-ciudad.

**Verificación final**:

| Métrica | Resultado |
|---|---|
| Grupos con duplicado por case restantes | **0** |
| Filas con `NCHAR(160)` (non-breaking space) restantes | **0** |
| Nombres de estado cargados como `City` restantes | **0** |
| Valores distintos de `City` (antes: 304) | **191** |
| Clientes con `City` no vacío | 1179 (sin cambios, no se agregó ni borró ningún cliente) |

**Pendiente, fuera de este backfill de datos**: el freno al `<input>` libre de `CustomerFormFields.jsx:121` (paso 3 del plan de §22.2, para que esto no se vuelva a ensuciar con altas/ediciones nuevas) y los gráficos de Parte 2 (§22.3) — ninguno de los dos se tocó en esta sesión, ambos siguen pendientes.

### 22.5 Cierre de §22 completo — Parte A (freno al input) + Parte B (gráficos) — ✅ Hecho (2026-07-30)

**Parte A — Freno al input de City:**
- Backend: `GET /api/customers/cities` nuevo (`CustomersController.cs`) — `ICustomerService.GetDistinctCities()`/`CustomerService` devuelven los valores distintos de `Customer.City` (no nulos/vacíos), ordenados. Verificado con curl: devuelve exactamente los 191 valores documentados en §22.4.
- Frontend: `CustomerFormFields.jsx` — el `<input>` de City ahora tiene un `<datalist>` (`list="city-suggestions"`) poblado desde ese endpoint (fetch único al montar el componente, mismo patrón `queueMicrotask` de §20 para no disparar `react-hooks/set-state-in-effect`) — sugiere sin forzar una lista cerrada, cualquier texto se sigue guardando igual. Como red de seguridad adicional, `onBlur` normaliza el valor a Title Case vía `toTitleCase()` (`src/utils/titleCase.js`, nuevo — capitaliza tras espacios/guiones/apóstrofos: "la vergne" → "La Vergne", "o'brien" → "O'Brien") y lo aplica llamando a `onFieldChange` con un evento sintético `{ target: { name: "city", value } }` — funciona igual en `Customers.jsx` y en el panel "crear dependiente nuevo" de `Policies.jsx` porque ambos `onFieldChange` solo leen `e.target.name`/`e.target.value`.
- **No se tocó ningún dato existente** — esto es prospectivo (altas/ediciones nuevas), el backfill de los 914 registros ya se hizo en §22.4.

**Parte B — Gráficos tipo torta/dona para "Additional statistics":**
- `Dashboard.jsx`: las 3 `NameCountList` (listas planas, componente ahora eliminado por quedar sin uso) se reemplazaron por el mismo `ChartWithLegend`/`DonutChart` que ya usan Tipo/Status (§9.2) — sin componente nuevo, tal como estaba planteado en §22.3.
- **Criterio elegido: top 9 + "Otros" agrupado para las 3 categorías (Aseguradora, Condado, Ciudad), sin excepción.** La propuesta original de §22.3 dejaba abierto si `InsuranceCompany` necesitaba el mismo tratamiento ("probablemente nunca llegue a necesitar el agrupamiento en la práctica") — se confirmó con SQL directo que **sí lo necesita**: hay **20 aseguradoras distintas** con pólizas reales (no las 31 posibles de la tabla completa), por encima del límite de 9. Aplicar el mismo criterio a las 3 evita una inconsistencia visual (2 tortas con "Otros" y una sin, dependiendo de cuántos valores tenga cada corrida) y es más simple de mantener. Función nueva `toTopSegments(items, unspecifiedLabel, othersLabel, limit=9)` en `Dashboard.jsx` — el bucket "Otros" usa un gris neutro fijo (`#9ca3af`, no un color de la paleta categórica) y va siempre al final. A diferencia de Tipo/Status (que tienen una lista maestra fija y por eso cada categoría conserva siempre el mismo color, §9.1), Aseguradora/Condado/Ciudad no tienen ese maestro — el color de una categoría puede variar según el ranking del filtro activo (documentado como comentario en el código).
- Clave i18n `stats.andMore` (ya no se usa, era específica del formato de lista) reemplazada por `stats.others` ("Otros"/"Others") en `dashboard.json` (es/en). `stats.unspecified` se mantiene, sigue usándose para traducir el bucket "Sin especificar"/"Unspecified" que ya devolvía el backend.
- **Backend sin cambios** — `DashboardService.GetStats` ya devolvía las 3 listas completas ordenadas descendente; el top 9 + Otros se resuelve enteramente en el frontend, mismo scoping por agente/fecha que el resto del Dashboard (no se tocó `ScopedPolicies` ni ningún endpoint).

**Verificado**: `dotnet build` (0 errores, mismos 17 warnings preexistentes) y `npm run build`/`npm run lint` (limpios, mismo warning preexistente de `Agentes.jsx`) corridos por separado tras cada cambio — Parte A antes de pasar a Parte B. Backend probado con curl: `GET /api/customers/cities` devuelve 191 valores; `GET /api/dashboard/stats` confirma que las 3 categorías superan el límite de 9 con datos reales (20 aseguradoras, 111 condados, 192 valores de ciudad incluyendo "Sin especificar"), validando que el bucket "Otros" se activa en las 3 tortas, no solo en County/City.



---

## 23. Customers duplicados por un bug de matching en la migración — ✅ Hecho (2026-07-29): 2 casos confirmados fusionados (§23.2) + matching reforzado para futuras migraciones, solo reporta (§23.3)

### 23.1 Cómo se detectó: el sufijo "+mig..." en `Customer.Email`

Encontrado al investigar un caso puntual: la Customer "Doris Maldonado" (Id 21390) tenía `Email = dorism89+migP12122025017644@hotmail.com` en vez del email real `dorism89@hotmail.com`. Rastreado a `EntityMatcher.ResolveUniqueEmailAsync` (`WholeCareInsurance.Migration/Matching/EntityMatcher.cs:240-256`): cuando el email real de una fila de origen ya está en uso por otro Customer (índice único de `Customer.Email`, `CustomerConfiguration.cs:21`), la migración no descarta el dato ni falla el import — inserta `+mig<SourceReference>` antes del `@` para no violar la unicidad, dejando constancia en el reporte de migración.

**Alcance medido de ese síntoma**: 35 de 2,130 Customers (≈1.6%) tienen ese sufijo. Reconstruyendo el email limpio para los 35, los 35 chocan con el email de otro Customer real — **0 casos** se podían revertir sin generar una colisión nueva. De esos 35, 33 son grupos familiares reales que comparten un correo de contacto en el sistema origen (cónyuges/padres/hijos con distintos apellidos, comportamiento correcto de la migración). Los otros **2 no eran familias — eran la misma persona real migrada dos veces** como Customers separados.

### 23.2 Los 2 casos confirmados — fusionados — ✅ Hecho (2026-07-29)

Búsqueda exhaustiva en los 2,130 Customers (no solo los 35 con sufijo): agrupando por `DateOfBirth` exacto hay 96 fechas compartidas por más de un Customer (195 registros), en su mayoría coincidencias de cumpleaños sin relación. Cruzando esos grupos contra mismo `Phone` **o** misma `Address1` normalizada (sin filtro de nombre, para no dejar nada afuera), el resultado fueron exactamente los mismos 2 pares — ninguno adicional oculto:

| | Id conservado | Id fusionado (borrado) | DOB | Evidencia |
|---|---|---|---|---|
| Doris Maldonado | **19613** ("Maldonado") | 21390 ("Maldonado Ramirez") | 1989-08-05 | mismo `Phone`, misma `Address1` ("889 Pin Oak Dr" / "Drive"), mismo `AgentId` (3048) en ambas pólizas |
| Mariana Salvador Cruz | **19315** ("Salvador - Cruz", con SSN real) | 21386 ("Salvador Cruz", SSN placeholder) | 2005-03-27 | misma `Address1` exacta ("2700 Glenmont Ct"); `Phone` distinto entre las dos filas de origen |

**Causa raíz confirmada**: `EntityMatcher.NameDobKey` (`EntityMatcher.cs:60-61`) compara `FirstName|LastName|DOB` como string exacto. Una variación mínima de formato en `LastName` entre dos filas de origen de la misma persona (un apellido de más — "Maldonado" vs "Maldonado Ramirez" — o un guión — "Salvador - Cruz" vs "Salvador Cruz") alcanza para que el matching no las una. Tampoco matchean por SSN porque ninguna de las 4 filas de origen traía un SSN real: el placeholder `NS-<SourceReference>` (`BuildSsnPlaceholder`) es único por fila, así que nunca colisiona entre sí.

**Proceso de fusión** (cada Id duplicado tenía exactamente 1 `Policy` propia como titular, 0 como `PolicyDependent`, 0 documentos, 0 beneficiarios):
1. **Backup completo previo**: `D:\backups\WholeCareInsuranceDb_pre_merge_duplicados_20260729.bak` (10.8 MB, `BACKUP DATABASE` completo, mismo criterio que §22.4).
2. `UPDATE Policies SET CustomerId = <Id conservado>` para la única póliza de cada Id duplicado (Doris: póliza 381839 P12122025017644; Mariana: póliza 381835 P22112025017101).
3. Verificación pre-`DELETE` (mostrada y confirmada por el responsable antes de borrar): `Policies` y `PolicyDependents` con `CustomerId` = Id a borrar → 0 filas en ambos casos, para los 2 Ids.
4. `DELETE` de `Customers` Id 21390 y 21386.
5. Verificación final: `COUNT(*)` de `Customers` bajó de 2,130 a **2,128** (exacto, sin efectos colaterales); las 2 pólizas reasignadas quedaron accesibles y con datos correctos consultando por los Ids conservados (19613 tiene 2 pólizas, 19315 tiene 2 pólizas).

**Hallazgo aparte, visto en el camino, sin resolver**: la póliza 381835 de Mariana (la que traía "Salvador Cruz" sin SSN real) tenía `AgentId = 1` (el Admin seedeado, fallback de `EntityMatcher.ResolveAgentAsync` — ver §7/§15.3) en vez de un agente real, porque el nombre de agente de esa fila de origen no matcheó ningún `User` existente al momento de migrar. No es parte de este trabajo de deduplicación de Customers, pero queda anotado como candidato a revisar junto con el pendiente ya documentado en §7 de reasignar `Customer.AgentId` de fallback cuando corresponda (mismo tipo de gap, ahora confirmado que también aplica a `Policy` vía el agente asociado a la fila migrada).

### 23.3 Mejora del matching de deduplicación para futuras migraciones/re-importaciones — ✅ Hecho (2026-07-29): solo reporta, nunca fusiona

**Decisión confirmada del responsable**: el matching fuzzy nuevo **solo deja constancia en el reporte** (mismo patrón que `SsnCollisionWarnings`) — nunca fusiona ni descarta el registro por su cuenta. El `Customer` se sigue creando exactamente igual que antes cuando no hay match exacto por `NameDobKey`; el volumen es bajo (2 en 2,130) y no se justifica el riesgo de una fusión automática de personas distintas por unos pocos casos por corrida. Cualquier "posible duplicado" señalado se revisa manualmente caso por caso, igual que se hizo con Doris y Mariana en §23.2.

**Implementado en `EntityMatcher.cs`**:
- Nuevo índice en memoria `_customerByDob` (poblado en `PreloadCachesAsync` y actualizado en cada alta dentro del mismo run, igual que `_customerByNameDob`), agrupa Customers por `DateOfBirth.Date` con su `FirstName`/`LastName`/`Phone`/`Address1` ya normalizados.
- `CheckPossibleDuplicate(data)`, invocado en `ResolveCustomerAsync` justo antes de crear un `Customer` nuevo (solo cuando el `NameDobKey` exacto ya falló): dispara únicamente si **las dos condiciones se cumplen a la vez** — (1) `FirstName` normalizado idéntico (sin tolerancia, para no generar ruido con nombres comunes) y `LastName` "variante" del mismo apellido (uno contiene al otro tras sacar espacios/guiones/puntuación, o distancia de Levenshtein ≤2) — **y** (2) `Phone` o `Address1` normalizados coinciden exactamente con algún Customer del mismo DOB. Sin la señal (2), no dispara — es lo que evita fusionar por la simple coincidencia de cumpleaños (96 grupos de DOB compartido en la base real, casi todos sin relación).
- Cuando dispara, agrega una entrada a `MigrationReport.PossibleDuplicateWarnings` (nueva lista, impresa en `Print()` igual que las demás advertencias) con el `SourceReference`, el nombre de la fila, el `Customer.Id` candidato y qué señal coincidió (`Phone` o `Address1`) — y el flujo sigue exactamente igual: el `Customer` se crea, nada se fusiona ni se descarta solo.
- Normalización agregada (reutilizable): `StripDiacritics` (quita acentos), `NormalizeNamePart` (trim + minúsculas + sin acentos + solo alfanumérico), `NormalizePhone` (solo dígitos), `NormalizeAddress` (minúsculas + abreviaturas comunes de EE.UU. — "Drive"→"dr", "Street"→"st", etc. — + espacios colapsados), y un `Levenshtein` propio (sin dependencia externa).

**Verificación** (build + prueba funcional dentro de una transacción revertida, sin persistir datos de prueba — mismo criterio de no tocar datos reales sin confirmar):
- `dotnet build` sobre `WholeCareInsurance.Migration`: **0 errores**, solo 2 warnings preexistentes sin relación (nullable en `Truncate`, ya presentes antes de este cambio).
- Harness de prueba descartable (proyecto de consola aparte, fuera del repo, referenciando `WholeCareInsurance.Migration` con `ProjectReference`) corrido dentro de una transacción con `ROLLBACK` al final:
  - **Caso positivo**: fila sintética "Doris Maldonado **Rodriguez**" (variante de apellido), mismo DOB/Phone/Address que el Customer real #19613 ("Doris Maldonado", sobreviviente de la fusión de §23.2) → dispara 1 warning señalando `#19613`, **y el Customer se crea igual** (`CustomerMatchKind.Created`), confirmando que no fusiona.
  - **Caso negativo 1** (mismo DOB + apellido variante, pero Phone/Address sin relación con nadie) → 0 warnings, como se esperaba.
  - **Caso negativo 2** (mismo DOB + mismo Phone/Address que #19613, pero apellido no relacionado — "Gutierrez") → 0 warnings, como se esperaba: confirma que compartir teléfono/dirección solo no alcanza sin el apellido variante.
  - `COUNT(*)` de `Customers` verificado en 2,128 antes y después de la prueba (la transacción de prueba se revirtió, cero datos de prueba persistidos).

**No implementado / fuera de este alcance**: no se corrió una migración real con este cambio (no hay una re-importación pendiente); esto queda listo para la próxima vez que se corra el script de migración o una re-importación. Tampoco se aplicó este mismo chequeo como script de verificación periódica contra la base ya viva (fue lo que se hizo a mano en §23.2 para los 2 casos existentes) — si se quiere repetir ese análisis más adelante, hoy sigue siendo manual.

---

## 24. Rediseño de la relación Customer ↔ Customer (titular/dependiente-aplicante) — ✅ Hecho (2026-07-30): las 4 fases (schema, backfill de 884 filas, endpoints backend, frontend) implementadas y verificadas end-to-end, más el backfill de los 75 dependientes de email real (§24.10) y de Clara/Elizabeth (§24.11) — `CustomerRelationship` en 961 filas. Solo queda el backfill manual de emails placeholder, trabajo operativo — ver §24.9

Motivado por la auditoría de §23 (deduplicación) y una auditoría aparte enfocada específicamente en esta relación (2026-07-30, solo SQL de lectura, sin tocar datos): **947 de 2,128 Customers (44.5%)** tienen email placeholder de migración (`noemail+P<referencia>@migracion.wholecare.local`). De esos, 881 están vinculados como dependientes vía `PolicyDependent` y 66 son titulares de su propia póliza con email real todavía no cargado — **el 100% de los 947 ya tiene algún vínculo formal a una póliza**, no hay huérfanos flotantes. El problema no es de vínculo faltante, es de **modelo**: hoy no existe ninguna forma de decir "este Customer es un dependiente, no un titular" fuera de inferirlo indirectamente a través de `PolicyDependent`, y esa tabla mezcla dos conceptos distintos (relación personal vs. cobertura de una póliza puntual). Detalle completo de la auditoría en el historial de conversación de esta sesión.

**Restricciones de diseño confirmadas por el responsable:**
1. Un dependiente puede estar vinculado a **más de un titular** (5 casos reales confirmados: hijos que aparecen como dependientes en la póliza de cada uno de sus 2 padres, cada uno con su propia póliza separada). Descarta un `ParentCustomerId` simple 1-a-1 en `Customer`.
2. La relación "personal" (quién es dependiente de quién, en general) y la relación "por póliza" (quién está cubierto en qué póliza específica) son conceptualmente distintas y hoy están mezcladas en `PolicyDependent`.
3. `Customer.RelacionConPrincipal` (§1.6) sigue siendo descriptivo, no relacional, y el 62.5% de los dependientes migrados quedó en `"Otro"` genérico por el origen de datos (`HealthDependentsExtractor.cs`/`EnumMaps.cs` mapean "Parent"/"Dependent" del archivo viejo a `"Otro"` sin distinción). **No se promete mejorar esto retroactivamente** como parte de este rediseño.
4. Los emails placeholder se completan a mano con el tiempo, sin bloquear nada mientras tanto — el diseño deja identificable la lista de candidatos (los 947 con `Email LIKE 'noemail+P%'`) sin necesidad de un campo nuevo para eso.
5. El Dashboard cuenta "Miembros" por póliza (§9.3, `MembersOf` en `DashboardService.cs:47-48`) — puede contar a la misma persona física 2 veces si está en 2 pólizas. Se agrega un conteo de personas únicas, sin tocar el existente (§24.4).

### 24.1 Diseño de tablas — tabla nueva `CustomerRelationship`, `PolicyDependent` no cambia

**`PolicyDependent` se mantiene exactamente como está** (`PolicyId` + `CustomerId` + `IsAplicante`) — sigue respondiendo "¿quién está cubierto en esta póliza puntual, y es aplicante?". No se toca su schema ni sus endpoints existentes.

**Tabla nueva, `CustomerRelationship`** — responde "¿quién es dependiente de quién, en general, más allá de una póliza puntual?":

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `int` (PK) | |
| `TitularCustomerId` | `int` (FK → `Customer`) | El principal |
| `DependentCustomerId` | `int` (FK → `Customer`) | El dependiente |
| `RelationshipType` | `string?` | Copiado de `Customer.RelacionConPrincipal` al migrar (ver §24.2); editable después por relación, no por persona — permite que la misma persona sea "Hijo/a" de un titular y algo distinto de otro, a futuro |
| `Source` | `string` | `"Sistema"` / `"Migración"` — mismo patrón ya usado en `PolicyHistory.Source` (§13), para diferenciar vínculos creados a mano de los derivados del backfill |
| `CreatedAt` | `DateTime` | |

- **Índice único compuesto** en (`TitularCustomerId`, `DependentCustomerId`) — evita duplicar el mismo vínculo, y al ser dos columnas separadas (no una FK única) soporta de forma nativa que un `DependentCustomerId` aparezca en múltiples filas con distinto `TitularCustomerId` (restricción #1) y que un `TitularCustomerId` tenga muchos dependientes.
- **Ambas FKs a `Customer` con `OnDelete(DeleteBehavior.Restrict)`** — mismo motivo que ya está documentado en `PolicyDependentConfiguration.cs:18-20`: dos FKs a la misma tabla desde una tabla intermedia no pueden cascadear ambas sin que SQL Server rechace el constraint por múltiples paths de cascada.
- **No se agrega ningún flag `IsDependent`/`Role` en `Customer`** — la pregunta "¿es titular o dependiente?" se responde con una consulta derivada (`EXISTS` contra `Policies.CustomerId` para titular, contra `CustomerRelationship.DependentCustomerId` para dependiente; una persona puede ser ambas cosas a la vez, ej. un hijo mayor que además es titular de su propia póliza — confirmado que ese patrón ya existe hoy, ver §24.2 "2 casos con sufijo que también son titulares"). Guardar un flag desnormalizado obligaría a mantenerlo sincronizado en cada alta/baja de relación o póliza, con riesgo de quedar desactualizado — se prefiere derivarlo en la query, no cachearlo.

**Vínculo entre las dos tablas**: al agregar un dependiente a una póliza (`POST /api/policies/{id}/dependents`, ya existente), el backend además crea (si no existe ya) la fila correspondiente en `CustomerRelationship` con `TitularCustomerId = Policy.CustomerId`. Si el par titular-dependiente ya tiene una fila (ej. el mismo dependiente se agrega a una segunda póliza del mismo titular — el caso de "renovación" confirmado en 25 de los 30 casos de la auditoría), no se duplica, gracias al índice único.

**✅ Confirmado (2026-07-30)**: al *sacar* un dependiente de una póliza (`DELETE .../dependents/{customerId}`), **la `CustomerRelationship` se conserva** — no se borra junto con `PolicyDependent`. La relación familiar sigue siendo cierta aunque se dé de baja una cobertura puntual.

### 24.2 Migración de los 947 Customers con email placeholder existentes

Se deriva `CustomerRelationship` a partir de los datos que **ya son confiables** — `PolicyDependent` + `Policies.CustomerId` — no del patrón de sufijo del email (ese patrón solo se usó como heurística de análisis en la auditoría, no es la fuente de verdad).

1. **881 dependientes con `PolicyDependent`**: por cada fila de `PolicyDependent`, insertar (si no existe) `CustomerRelationship(TitularCustomerId = Policy.CustomerId, DependentCustomerId = PolicyDependent.CustomerId, RelationshipType = Customer.RelacionConPrincipal, Source = "Migración")`. El índice único colapsa automáticamente los 25 casos de "mismo titular, 2 pólizas" (renovación) en una sola fila — no hace falta lógica especial para ese caso.
2. **66 titulares con email placeholder**: no generan ninguna fila en `CustomerRelationship` (no son dependientes de nadie) — quedan igual de identificables que hoy por `Email LIKE 'noemail+P%'` + `EXISTS` en `Policies.CustomerId`, para la revisión manual de email del punto #4.
3. **Los 5 casos de doble-titular ya confirmados** (customers 20496, 20497, 20516, 20517, 20644) se resuelven solos con este enfoque: cada uno genera automáticamente 2 filas en `CustomerRelationship`, una por cada titular real — es exactamente el comportamiento que la restricción #1 pide soportar, sin intervención manual.
4. **Los 17 grupos de sufijo ambiguo** (los que en la auditoría mapeaban a 2 pólizas distintas por prefijo de email compartido — superset que incluye los 5 del punto anterior más 12 grupos donde un solo Customer terminó con 2 vínculos de póliza sin la confirmación visual de "2 padres reales" que sí se hizo para esos 5): **se excluyen del INSERT automático** y quedan en una lista de revisión aparte (mismo patrón que `MigrationReport.PossibleDuplicateWarnings` de §23.3 — un reporte que se imprime, no una fusión/inserción automática). El responsable revisa esos casos puntuales (¿son de verdad 2 titulares distintos, como los 5 ya confirmados, o es un error de la migración original?) y recién después se corre un segundo paso que los inserta ya confirmados, uno por uno o en lote.
5. **Backup previo obligatorio** antes de correr el INSERT masivo, mismo criterio que §22.4/§23.2 (`BACKUP DATABASE` completo con timestamp en el nombre).
6. **Verificación post-migración**: `COUNT(*)` de `CustomerRelationship` esperado ≈ 862 (881 menos los ~19 que colapsan por duplicado de titular) menos los excluidos por el punto 4, más los que se sumen a mano después de la revisión — número exacto a confirmar corriendo la query real al momento de implementar, no antes.

### 24.3 Pantallas/endpoints afectados (alcance, sin implementar)

**Backend:**
- Modelo `CustomerRelationship` + `CustomerRelationshipConfiguration` + migración de EF Core (Fase 1, solo schema).
- Endpoints nuevos, mismo prefijo que ya usa Customers (`api/customers`): `GET /api/customers/{id}/dependents` (titulares → sus dependientes) y `GET /api/customers/{id}/titulares` (dependiente → sus titulares). **✅ Nombres confirmados (2026-07-30)** — sujetos a cambiar solo si al implementar aparece algo mejor, sin necesidad de re-discutirlo antes.
- `PoliciesController.AddDependent`/`RemoveDependent` (`PoliciesController.cs:251-`, `:303-`): se les agrega el efecto colateral de upsert/conservación en `CustomerRelationship` descripto en §24.1.
- `CustomerService.GetAll()`/`Search()` (`CustomerService.cs:16`, `:28`): nuevo filtro opcional (`?role=titular|dependiente`) para que `Customers.jsx` y los dropdowns de selección puedan separar roles — hoy no existe ningún filtro de este tipo (confirmado en la auditoría), la lista siempre trae todo mezclado.
- `DashboardService.GetSummary` (`DashboardService.cs:50-76`): nueva query para el conteo de personas únicas (§24.4) — **aditiva**, no reemplaza `MembersCount` existente ni toca `GetByStatus`/`GetByType`.

**Frontend:**
- `Customers.jsx`: **✅ confirmado (2026-07-30)** — filtro de rol como query param sobre el listado paginado existente (`?role=titular|dependiente`), no una pestaña/vista aparte. Usa el `?role=` nuevo del backend; hoy la tabla no distingue nada, es la causa directa del hallazgo de la auditoría anterior de que "cantidad de Customers" mezcla ambos roles.
- `Policies.jsx`: la sección "Dependientes" (§1.2/§2) no necesita cambios funcionales — sigue hablando con `PolicyDependent` igual que hoy. Posible mejora menor (no obligatoria): mostrar en el detalle de un dependiente si tiene otro titular además del de esta póliza, usando el endpoint nuevo.
- `Dashboard.jsx`: nueva tarjeta KPI "Personas únicas" junto a "Miembros" (§9.2) — ver §24.4.
- No se propone ninguna pantalla nueva dedicada a "revisión de email placeholder" — el filtro `?role=` de Customers.jsx ya alcanza para listar los 947 candidatos (`email LIKE 'noemail+P%'`) sin construir nada aparte.

### 24.4 "Miembros" vs. "Personas únicas" en el Dashboard

Se propone **no tocar** `MembersCount`/`GetByStatus`/`GetByType` — miden cobertura por póliza (cuántas coberturas activas hay), que es una métrica de negocio legítima por sí misma (ej. para comisiones o volumen de pólizas). Se agrega un campo **nuevo** a `DashboardSummaryDto`, `UniqueMembersCount`: `COUNT(DISTINCT CustomerId)` sobre la unión de titulares de las pólizas en el scope (`ScopedPolicies(...).Select(p => p.CustomerId)`) y sus dependientes vía `CustomerRelationship` (`DependentCustomerId` donde `TitularCustomerId` está en ese mismo conjunto) — deduplicando por persona física, no por par titular-póliza. Mismo scoping por agente/fecha que ya tiene `GetSummary` (§9.5), sin cambios en `ScopedPolicies` ni en el resto del pipeline.

### 24.5 Fases de trabajo (estimación)

- **Fase 1 — Backend/schema — ✅ Hecho (2026-07-30)**: modelo `CustomerRelationship` (`Models/CustomerRelationship.cs`), `CustomerRelationshipConfiguration.cs` (índice único compuesto `TitularCustomerId`+`DependentCustomerId`, ambas FKs a `Customer` en `Restrict`, igual que `PolicyDependentConfiguration.cs:18-20`), `DbSet` agregado a `AppDbContext.cs`, migración `20260730141422_AddCustomerRelationship` generada y aplicada contra la base de dev (`dotnet ef database update`). Sin backfill de datos todavía — tabla creada vacía a propósito (Fase 2 la puebla). Verificado: `dotnet build` sin errores nuevos (mismos 17 warnings preexistentes de nullable en DTOs de `Users`), schema confirmado por SQL directo (columnas, índice único, ambas FK con `ON DELETE NO ACTION`/Restrict), `COUNT(*)` de la tabla nueva en 0, y la API arranca limpio (`dotnet run`, seedea el admin, Swagger responde 200) sin ningún error de mapeo del modelo nuevo.
- **Fase 2 — Migración de datos existentes — ✅ Hecho (2026-07-30)**: ver detalle completo en §24.6.
- **Fase 3 — Endpoints backend — ✅ Hecho (2026-07-30)**: ver detalle completo en §24.7.
- **Fase 4 — Frontend — ✅ Hecho (2026-07-30)**: ver detalle completo en §24.8.
- **Fuera de fase, trabajo continuo sin bloquear nada**: ver §24.9 — Clara/Elizabeth, los 69 dependientes de email real sin migrar, y el backfill manual de emails placeholder.

**Los 3 puntos abiertos de la propuesta original ya están resueltos** (comportamiento de `RemoveDependent`, filtro de rol como query param, nombres de tabla/endpoints — ver marcas ✅ arriba). **Las 4 fases están hechas** (ver §24.6, §24.7, §24.8) — ver cierre general en §24.9.

### 24.6 Cierre de la Fase 2 — backfill de `CustomerRelationship` — ✅ Hecho (2026-07-30)

**Backup previo**: `D:\backups\WholeCareInsuranceDb_pre_customerrelationship_migracion_20260730.bak` (10.9 MB, `BACKUP DATABASE` completo, mismo criterio que §22.4/§23.2).

**Alcance final, ajustado en dry-run contra el plan original de §24.2**: al reconstruir el detalle completo de los 17 grupos de sufijo ambiguo (nombres, pólizas y titulares reales de cada miembro), 14 de los 17 resultaron ser renovaciones inofensivas (la misma familia migrada dos veces bajo el mismo titular, sin ambigüedad real) y solo 3 grupos tenían un miembro con titulares genuinamente distintos — exactamente los 5 ya confirmados en la auditoría original. Con esa evidencia, el responsable ajustó el alcance del `--commit`:
- **Incluidos**: los 25 pares titular-dependiente de renovación pura (colapsan a 1 fila cada uno por el índice único, sin pérdida de información) + los 5 casos de doble titular confirmado (`20496`, `20497`, `20516`, `20517`, `20644` — 2 filas cada uno, un `TitularCustomerId` distinto por fila).
- **Excluidos, documentados para revisión manual aparte** (no se insertó nada por ellos):
  - **Clara Hasboun De Baptista (Id `20643`)**: comparte el grupo de sufijo `noemail+P23102025015943` con Jamal Baptista rivas (`20644`, uno de los 5 confirmados), pero Clara individualmente tiene un solo vínculo `PolicyDependent` (titular único, 19504 Luis Alfonso Baptista Zambrano) — no hay ninguna señal concreta de que sea un caso doble, solo quedó agrupada por compartir el prefijo de email con su hermano ambiguo. **Para incluirla después**: confirmar con el responsable si Clara tiene o no un segundo titular real (ej. el mismo padre/madre separado que aparece para Jamal) — si no lo tiene, se inserta como caso normal (`TitularCustomerId = 19504`).
  - **Elizabeth Gonzalez rea (Id `20626`)**: mismo patrón — comparte el grupo `noemail+P26032026018209` con Nohemi Dugarte (`20625`) y Jahdiel Andara dugarte (`20627`), ambos con 2 vínculos de renovación al mismo titular (19491 Edder Andara palacios), pero Elizabeth individualmente tiene un solo vínculo `PolicyDependent` a ese mismo titular. **Para incluirla después**: mismo chequeo — confirmar que no falta un segundo vínculo real antes de insertarla como caso normal (`TitularCustomerId = 19491`).

**Ejecución**: `INSERT INTO CustomerRelationships (TitularCustomerId, DependentCustomerId, RelationshipType, Source, CreatedAt) SELECT DISTINCT ...` desde `PolicyDependents` + `Policies` + `Customer.RelacionConPrincipal`, filtrando `Email LIKE 'noemail+P%'` y excluyendo únicamente `20643` y `20626`. `Source = 'Migración'` en las 884 filas (vs. `'Sistema'`, reservado para altas manuales futuras vía UI, cuando exista en Fase 3).

**Verificación post-`--commit`**:

| Chequeo | Resultado |
|---|---|
| `COUNT(*)` de `CustomerRelationships` | **884** (coincide exacto con el dry-run confirmado) |
| Los 5 casos de doble titular tienen 2 filas cada uno, con 2 `TitularCustomerId` distintos | ✅ confirmado (`20496`, `20497`, `20516`, `20517`, `20644`, todos 2/2) |
| Clara (`20643`) y Elizabeth (`20626`) no tienen ninguna fila | ✅ confirmado, `COUNT(*) = 0` para ambos Ids |

**Pendiente, fuera de este backfill**: revisión manual de Clara y Elizabeth (arriba) — no bloquea las Fases 3/4, que ya pueden construirse sobre las 884 filas existentes.

### 24.7 Cierre de la Fase 3 — endpoints backend — ✅ Hecho (2026-07-30)

**Implementado, siguiendo exactamente el alcance de §24.3:**
- `GET /api/customers/{id}/dependents` y `GET /api/customers/{id}/titulares` nuevos en `CustomersController.cs` — 404 si el Customer no existe, devuelven `CustomerRelationshipResponseDto[]` (`CustomerId`, `FirstName`, `LastName`, `RelationshipType`).
- `ICustomerService`/`CustomerService`: `GetDependentsOf`, `GetTitularesOf` (ambos con `Include` del lado correspondiente de `CustomerRelationship`), y `UpsertRelationship(titularCustomerId, dependentCustomerId, relationshipType)` — no duplica si ya existe (chequeo `AnyAsync` antes del `Add`, respeta el índice único).
- `PoliciesController.AddDependent`: después de crear el `PolicyDependent`, llama a `_customers.UpsertRelationship(policy.CustomerId, dto.CustomerId, customer.RelacionConPrincipal)` con `Source = "Sistema"` (vs. `"Migración"` de la Fase 2, mismo campo `CustomerRelationship.Source` distingue el origen).
- `PoliciesController.RemoveDependent`: sin cambios de comportamiento, solo un comentario explícito dejando constancia de que **no** se toca `CustomerRelationship` — decisión confirmada en §24.6.
- `CustomerService.GetAll()`/`Search()`: nuevo parámetro opcional `role` (`"titular"` → `EXISTS` en `Policies`, `"dependiente"` → `EXISTS` en `CustomerRelationships.DependentCustomerId`, cualquier otro valor no filtra), aplicado también en `CustomersController.GetAll` vía `?role=`. Los dos roles no son excluyentes, tal como estaba documentado.
- `DashboardSummaryDto.UniqueMembersCount` (nuevo, aditivo) + `DashboardService.UniqueMembersCount(titularIds)`: `COUNT(DISTINCT CustomerId)` sobre la unión de titulares del scope y sus dependientes vía `CustomerRelationship`. `MembersCount`/`GetByStatus`/`GetByType` sin cambios.

**Verificado con curl contra la base local (con las 884 filas reales de la Fase 2), todo end-to-end antes de pasar al siguiente endpoint:**

| Chequeo | Resultado |
|---|---|
| `GET /api/customers/20496/titulares` | 2 titulares reales (`Irma Castro Pastrana`, `Josue Maradiaga argenal`) — **no 1**, confirma el caso de doble titular |
| `GET /api/customers/19264/dependents` | 3 dependientes (`Gisela`, `Montserrat`, `Cristina`), no 6 — confirma que el colapso de renovación de la Fase 2 se ve reflejado correctamente al leer |
| `GET /api/customers/999999/dependents` | `404` (Customer inexistente) |
| `GET /api/customers?role=titular` | 1181 — coincide exacto con `COUNT(DISTINCT CustomerId)` de `Policies` por SQL directo |
| `GET /api/customers?role=dependiente` | 879 — coincide exacto con `COUNT(DISTINCT DependentCustomerId)` de `CustomerRelationships` por SQL directo |
| `GET /api/customers` (sin filtro) | 2128, sin cambios |
| `POST /api/policies/380997/dependents` (titular real 19613, Customer de prueba 21389) | Crea `PolicyDependent` **y** `CustomerRelationship(19613, 21389, Source="Sistema")` automáticamente |
| `DELETE /api/policies/380997/dependents/21389` | Borra el `PolicyDependent`; la `CustomerRelationship` **sigue existiendo** (confirmado por SQL directo) — dato de prueba borrado a mano después de verificar, `CustomerRelationships` vuelve a 884 |
| `GET /api/dashboard/summary` (vista global) | `policiesCount=1211`, `membersCount=2198`, `uniqueMembersCount=2058` — verificado por SQL cruzado: 1181 titulares + 879 dependientes − 2 de overlap (Customers que son titular y dependiente a la vez) = 2058 |
| `GET /api/dashboard/summary?agentId=3013` (Ana Ayala Marin, scopeado) | `policiesCount=386`, `membersCount=798`, `uniqueMembersCount=760` — `agenciesCount`/`agentsCount` en `null` como corresponde a vista scopeada |

**Build**: `dotnet build` limpio, 0 errores, mismos 17 warnings preexistentes de nullable en DTOs de `Users` (sin relación). Sin cambios de frontend en esta fase — no corresponde `npm run lint`.

### 24.8 Cierre de la Fase 4 — frontend — ✅ Hecho (2026-07-30)

**Implementado, siguiendo exactamente el alcance de §24.3:**
- `Customers.jsx`: selector "Mostrar" (Todos/Titulares/Dependientes) junto al botón "+ Nuevo cliente", dispara `?role=titular|dependiente` sobre el mismo listado paginado existente (query param, sin pestaña/vista aparte, tal como se confirmó). Al cambiar el filtro se resetea a página 1; cambiar de página con un filtro activo lo conserva. Claves nuevas `roleFilter.*` en `customers.json` (es/en).
- `Dashboard.jsx`: tarjeta KPI nueva "Personas únicas" (`summary.uniqueMembersCount`) agregada junto a "Miembros" en la fila de KPIs — mismo componente `StatTile`, sin tocar la tarjeta de Miembros existente ni su posición. Clave nueva `summary.uniqueMembers` en `dashboard.json` (es/en).

**Verificado**: `npm run build`/`npm run lint` limpios después de cada cambio (mismo warning preexistente de `react-hooks/exhaustive-deps` en `Agentes.jsx`, sin relación) — build y lint corridos por separado tras `Dashboard.jsx` y de nuevo tras `Customers.jsx`, antes de pasar al siguiente. Confirmado en el navegador por el responsable en `localhost:5173` con API real corriendo en `localhost:5279`:
- Filtro "Todos"/"Titulares"/"Dependientes" en Customers.jsx: totales de 2128/1181/879 respectivamente, coinciden con lo verificado por SQL en la Fase 3.
- KPI "Personas únicas" en el Dashboard: 2058 en vista global (junto a Miembros=2198, sin modificarla), y el número cambia correctamente al filtrar por agente.
- Nada roto en el resto de Customers/Policies.

**Hallazgo durante la verificación en navegador, investigado y cerrado sin cambios de código**: filtrando por "Titulares" aparecen 68 Customers con email placeholder (`noemail+P...`, ej. Arnaldo Acosta) — no son 66 como en la auditoría original. Confirmado por SQL directo que **no es un bug del filtro**: son los 66 titulares "puros" sin sufijo de la auditoría original de §24, **más 2** Customers con sufijo de grupo familiar (típicamente dependientes) que además resultan titulares de su propia póliza — el mismo caso "2 casos con sufijo que también son titulares" ya documentado en la auditoría inicial de §24. `66 + 2 = 68`, cierra exacto. El filtro `?role=titular` (`EXISTS` en `Policies.CustomerId`) funciona como se diseñó.

### 24.9 Cierre de §24 — ✅ Las 4 fases hechas (2026-07-30)

Diseño, backfill de datos, endpoints backend y frontend completos y verificados end-to-end (SQL directo + curl + navegador). Quedan 3 puntos de trabajo **aparte, sin bloquear nada** — ninguno requiere código nuevo, son revisión manual u operativa:

1. ~~Clara Hasboun De Baptista (`20643`) y Elizabeth Gonzalez rea (`20626`)~~ — ✅ Hecho (2026-07-30), ver §24.11.
2. ~~Dependientes con email real (no placeholder) que quedaron fuera del alcance de la Fase 2~~ — ✅ Hecho (2026-07-30), ver §24.10. El Customer `Id 9` ("Carlos Mendez", `carlos.mendez@example.com`) sigue siendo el único caso sin ningún vínculo (ni `Policy`, ni `PolicyDependent`, ni `CustomerRelationship`) — parece dato de prueba/seed anterior a la migración real, no un Customer de negocio; no requiere acción.
3. **Backfill manual de los ~947 Customers con `Email = NULL`** (originalmente placeholders `noemail+P...`, convertidos a `NULL` por §25 el mismo día) — trabajo operativo del responsable/agentes, no bloquea nada. El filtro `?role=titular` de Customers.jsx (§24.8) ya sirve como lista de candidatos: los titulares sin email son exactamente los que necesitan revisión. **Único punto operativo que queda de §24**, no es trabajo de desarrollo.

### 24.10 Backfill de los dependientes con email real — ✅ Hecho (2026-07-30)

**Corrección del número reportado en §24.9, punto 2**: se había dicho "69 dependientes con email real" — el número correcto es **75**. El "69" salió de una query de la sesión anterior que, sin darse cuenta, mezclaba dos poblaciones distintas: Customers sin `Policy` propia y sin `CustomerRelationship`, con `PolicyDependent`, **sin filtrar por tipo de email** — eso incluía a la vez 67 dependientes de email real que no son titulares de nada, **más Clara (`20643`) y Elizabeth (`20626`)** (las 2 excluidas de la Fase 2, que también cumplen "sin Policy, sin CustomerRelationship, con PolicyDependent" por ser placeholder). `67 + 2 = 69` — el número cerraba por casualidad, pero no representaba lo que decía representar. La población real de "dependientes de email real sin `CustomerRelationship`" es **75**: los 67 de arriba **más 8** que además son titulares de su propia póliza (entre ellos, dos casos notables: `19347` Josue Maradiaga argenal y `19373` Yureima Fernandez Fonseca — exactamente los 2 titulares del caso de doble-titular confirmado en la Fase 2 — aparecen ahora también como dependientes en la póliza de su ex-pareja/familiar; el modelo M:N ya soportaba este caso sin cambios).

**Backup previo**: `D:\backups\WholeCareInsuranceDb_pre_customerrelationship_email_real_20260730.bak` (11.3 MB).

**Revisión de ambigüedad** (mismo criterio que la Fase 2, §24.6): de los 75, solo un Customer (`20456`, Sophia Medina) tenía más de una fila en `PolicyDependent` — 2 pólizas, **mismo titular** (`19280`, Lucy Lugo Martinez) — renovación pura, sin ambigüedad. La búsqueda específica de "dependiente con más de un titular distinto" no encontró ningún caso nuevo. **No hizo falta excluir a nadie** — a diferencia de la Fase 2, acá no hay ningún Clara/Elizabeth equivalente.

**Ejecución**: mismo `INSERT ... SELECT DISTINCT` que §24.6, sobre `PolicyDependents`+`Policies`+`Customer.RelacionConPrincipal`, filtrando `Email NOT LIKE 'noemail+P%'` y sin ningún `CustomerRelationship` previo. `Source = 'Migración'`.

**Verificación post-`--commit`**:

| Chequeo | Resultado |
|---|---|
| `COUNT(*)` de `CustomerRelationships` | **959** (884 + 75, exacto) |
| Josue (`19347`): fila como titular (Fase 2, dependientes `20496`/`20497`) **y** fila como dependiente (esta pasada, titular `19346` Irma Castro Pastrana) | ✅ confirmado, ambas coexisten |
| Yureima (`19373`): fila como titular (Fase 2, dependientes `20516`/`20517`) **y** fila como dependiente (esta pasada, titular `19433` Taishy Vento Fernandez) | ✅ confirmado, ambas coexisten |

Confirma que el modelo soporta sin fricción que la misma persona sea titular de una póliza y dependiente de otra a la vez — tal como estaba diseñado desde §24.1, ahora con un caso real de la base que lo ejercita.

**Con esto, el hueco de cobertura de la Fase 2 queda cerrado.** Los únicos puntos que siguen pendientes, sin bloquear nada, son Clara/Elizabeth (punto 1 arriba) y el backfill manual de emails placeholder (punto 3 arriba).

### 24.11 Revisión manual de Clara y Elizabeth — ✅ Hecho (2026-07-30)

**Diagnóstico** (solo lectura, antes de tocar nada): ambas tienen exactamente **1 fila** en `PolicyDependent` — Clara con titular único `19504` (Luis Alfonso Baptista Zambrano, póliza `P23102025015943`, Cancelado), Elizabeth con titular único `19491` (Edder Andara palacios, póliza `P26032026018209`, Procesado). Ninguna de las dos es titular de su propia póliza, ninguna tenía ya una fila en `CustomerRelationship`.

**Confirmado el motivo de la exclusión original**: fue puramente el agrupamiento por prefijo de email compartido de la Fase 2 (§24.6), no evidencia individual de ambigüedad. Clara comparte grupo (`noemail+P23102025015943`) con Jamal Baptista rivas (`20644`, uno de los 5 casos reales de doble titular); Elizabeth comparte grupo (`noemail+P26032026018209`) con Nohemi Dugarte (`20625`) y Jahdiel Andara dugarte (`20627`), que tenían 2 filas pero al **mismo** titular (renovación, ya incluidos en los 25 de la Fase 2). Ni Clara ni Elizabeth mostraban, a nivel individual, ninguna señal distinta de cualquiera de los 884+75 casos ya insertados — la exclusión fue por prudencia de grupo, no por un hallazgo propio.

**Backup previo**: `D:\backups\WholeCareInsuranceDb_pre_customerrelationship_clara_elizabeth_20260730.bak`.

**Ejecución**: 2 filas insertadas a mano en `CustomerRelationship` (`RelationshipType = "Otro"`, igual que su `Customer.RelacionConPrincipal`; `Source = "Migración"`, mismo criterio que el resto del backfill):
- `TitularCustomerId=19504, DependentCustomerId=20643` (Clara)
- `TitularCustomerId=19491, DependentCustomerId=20626` (Elizabeth)

**Verificación**: `COUNT(*)` de `CustomerRelationships` pasó de **959 a 961** (exacto), ambas filas confirmadas con el titular correcto.

**Con esto, §24 no tiene ningún punto operativo pendiente salvo el backfill manual de emails placeholder** (punto 3 de §24.9) — que es trabajo del responsable/agentes, no de desarrollo.

---

## 25. `Customer.Email` opcional — reemplaza el placeholder `noemail+P...` por `NULL` — ✅ Hecho (2026-07-30): las 3 fases (schema, backfill, script de migración) completas y verificadas

Decisión del responsable: en vez de mantener el placeholder sintético `noemail+P<referencia>@migracion.wholecare.local` para los Customers migrados sin email real (§24, auditoría original), se prefiere dejar el campo directamente vacío (igual que el legacy, que muestra "Empty") para que cada agente cargue el dato real con el tiempo. Diagnóstico técnico previo (mismo día, sin cambios de código) confirmó que es viable pero no trivial — el índice único existente solo tolera **una** fila con `NULL` en toda la tabla salvo que se pase a un índice filtrado.

**Orden estricto acordado** (invertirlo rompe el índice): Fase A (schema + índice filtrado, sin tocar datos) → Fase B (backfill de los ~947 existentes) → Fase C (evitar que una futura re-importación vuelva a generar placeholders).

### 25.1 Fase A — Schema e índice filtrado — ✅ Hecho (2026-07-30)

- `Models/Customer.cs`: `Email` pasó de `string` a `string?`.
- `CustomerConfiguration.cs`: se sacó `.IsRequired()` de la property; el índice único pasó a **filtrado** — `entity.HasIndex(c => c.Email).IsUnique().HasFilter("[Email] IS NOT NULL")`. Motivo (probado empíricamente antes de decidir, ver conversación de esta sesión): un índice único normal de SQL Server solo permite **una** fila con `NULL` en toda la tabla — con un índice filtrado, cualquier cantidad de filas con `NULL` conviven sin problema, y el índice solo sigue exigiendo unicidad entre los emails reales que sí se carguen.
- `CustomerCreateDto`/`CustomerUpdateDto` (esta última hereda de la primera): se sacó `[Required]` de `Email`, se mantuvo `[EmailAddress][MaxLength(200)]`. `CustomerResponseDto.Email` pasó a `string?`.
- Migración `20260730155631_MakeCustomerEmailNullableWithFilteredIndex` generada y aplicada contra la base de dev (`ALTER COLUMN Email NULL` + `DROP`/`CREATE UNIQUE INDEX ... WHERE [Email] IS NOT NULL`).
- Frontend: se sacó `required` del `<input type="email">` de `CustomerFormFields.jsx` (usado tanto en `Customers.jsx` como en el panel "crear dependiente nuevo" de `Policies.jsx`).
- **Hallazgo durante la verificación, corregido en el mismo paso**: `[EmailAddress]` de ASP.NET Core rechaza con 400 un string **vacío** (`""`) — solo `null` es válido cuando no hay `[Required]` (confirmado con curl: `email:""` → 400 "not a valid e-mail address"; `email:null` → 201). Como el estado default del formulario usa `""`, no `null`, se agregó la conversión `email: form.email === "" ? null : form.email` en el armado del body tanto en `Customers.jsx` como en `Policies.jsx` (`handleSubmit`/`handleCreateDependent`) — sin este fix, guardar un Customer sin tocar el campo Email hubiera fallado con 400 pese a que el campo ya no es obligatorio.

**Verificado**:
- Metadata del índice por SQL directo: `has_filter=1`, `filter_definition='([Email] IS NOT NULL)'`.
- Prueba empírica (transacción con `ROLLBACK`, sin dejar datos): 2 filas reales de `Customers` con `Email=NULL` insertadas sin conflicto contra el índice ya filtrado.
- `dotnet build`/`npm run build`/`npm run lint` limpios (mismos warnings preexistentes de siempre, ninguno nuevo).
- End-to-end con curl contra la API real (limpiando los datos de prueba después de cada verificación): crear 2 Customers reales con `email:null` → ambos `201`; editar uno con un email real → `200`; intentar poner ese mismo email real en el otro → rechazado (el índice sigue exigiendo unicidad entre emails reales — cae en un `500` genérico de `GlobalExceptionMiddleware`, gap preexistente sin relación a este cambio, ya señalado en el diagnóstico previo); volver a vaciar el email vía edición → `200`, confirma que el fix de `""→null` del frontend funciona en el flujo de edición también.

### 25.2 Fase B — Backfill de los ~947 existentes — ✅ Hecho (2026-07-30)

**Backup previo**: `D:\backups\WholeCareInsuranceDb_pre_email_null_backfill_20260730.bak` (11.3 MB).

**Re-medición antes de aplicar** (confirmada con el responsable antes de correr el `UPDATE`): `SELECT COUNT(*) FROM Customers WHERE Email LIKE 'noemail+P%'` → **947**, idéntico al número original de la auditoría de §24 — confirma que ninguna de las inserciones posteriores en `CustomerRelationship` (Fase 2, backfill de email real, Clara/Elizabeth) tocó `Customer.Email`.

**Ejecución**: `UPDATE Customers SET Email = NULL WHERE Email LIKE 'noemail+P%'` (con `SET QUOTED_IDENTIFIER ON`, requerido por SQL Server para escribir en una tabla con índice filtrado) — **947 filas actualizadas**, exacto.

**Verificación post-`UPDATE`**:

| Chequeo | Resultado |
|---|---|
| Customers con `Email IS NULL` | **947** |
| Total de Customers (no debía cambiar) | **2128**, sin cambios |
| Placeholders `noemail+P%` residuales | **0** |
| Customers con email real intacto | **1181** |
| Suma de control (947 + 1181) | **2128**, cierra exacto |

### 25.3 Fase C — Evitar nuevos placeholders en futuras re-importaciones — ✅ Hecho (2026-07-30)

`EntityMatcher.cs`, `ResolveUniqueEmailAsync`: cuando no hay email real en el origen (`string.IsNullOrWhiteSpace(rawEmail)`), ahora devuelve `null` en vez de generar `noemail+{sourceReference}@migracion.wholecare.local` — la firma pasó de `Task<string>` a `Task<string?>`. La lógica de desambiguación por colisión (sufijo `+mig{sourceReference}`, §23.1) se mantiene intacta para cuando sí hay un email real que choca con el de otro Customer — ese caso no cambió. El chequeo de "email ya en uso" se ajustó a `c.Email != null && c.Email.ToLower() == candidate.ToLower()` para reflejar que `Customer.Email` ahora es nullable.

Solo afecta a una futura re-importación o re-run del script — el script ya corrido en §7 no se re-ejecuta, así que no hay backfill retroactivo que hacer por este cambio (ya lo hizo §25.2 directamente sobre la base).

**Verificado**: `dotnet build` sobre `WholeCareInsurance.Migration` — 0 errores; los únicos 2 warnings de nullable en el archivo (`FirstName`/`LastName` vía `Truncate`) son los mismos preexistentes ya documentados en §23.3, sin relación a este cambio — no se agregó ningún warning nuevo gracias al guard `c.Email != null`. No se corrió una migración real (no hay una re-importación pendiente) — mismo criterio que §23.3, queda listo para ejercitarse la próxima vez que se corra el script.

**Con esto, §25 queda completo.** El único paso que sigue siendo trabajo operativo (no de desarrollo) es que los agentes carguen los emails reales de a poco, ahora sobre un campo que empieza en `NULL` en vez de un placeholder sintético.

---

## 26. Rediseño de la sección "Profile" — ver/editar datos del perfil + cambio de contraseña — ✅ Hecho (2026-07-30)

Pedido: hoy `/profile` (accesible desde el ícono de usuario en `Header.jsx`) muestra únicamente el formulario de cambio de contraseña — no hay forma de que un usuario logueado vea ni edite sus propios datos (nombre, teléfono, dirección, etc.). El legacy sí tiene una ficha de perfil completa (Name/agencia, Description/nombre de persona, Phone, Email, y una sección Address separada con Address #1/#2, City, Country, State/Province, Zip code, County — captura de referencia adjunta por el responsable). Se pide agregar esa vista/edición sin sacar el cambio de contraseña, con algún tipo de menú/tabs para navegar entre las dos secciones.

### 26.1 Investigación previa — qué existe hoy

**Backend, endpoints relacionados a "mis propios datos"** (`UsersController.cs`):
- `GET /users/me` (`:97-105`) — existe, pero devuelve `UserMeDto`, un DTO **deliberadamente chico**: solo `Nombre`, `Email`, `Rol`, `IsEncargado`, `PreferredLanguage`, `MustChangePassword`. **No incluye `Phone`, `Agency`, `Address1/Address2/City/State/ZipCode/County` ni ninguno de los campos de perfil del Agente (§11)** — hoy se usa solo para la reconciliación en background de `AppLayout.jsx` (detectar `MustChangePassword` en una sesión activa), no para mostrar un perfil.
- `PUT /users/me/language` (`:107-119`) — existe, pero solo actualiza `PreferredLanguage` (lo usa el selector de idioma del Header). No sirve como base para editar el resto de los campos.
- `PUT /users/{id:int}` (`:121-169`) — el único endpoint que edita el resto de los campos (`Phone`, `Agency`, `Address1/2`, `City`, `State`, `ZipCode`, `County`, `Licensed`, `NpnNumber`, etc.), pero está restringido con `[Authorize(Roles = "Admin")]`. **Un Agente no puede llamarlo ni siquiera para su propio Id** — hoy no existe ningún camino por el que un Agente autenticado pueda editar un solo campo de su propio perfil más allá del idioma.
- Aparte, `GET /users/{id}` (`:88-95`) solo exige `[Authorize]` (cualquier rol autenticado) y no restringe a que `id` sea el propio — cualquier usuario logueado puede traer el perfil completo de **cualquier otro** User por Id. No es parte de este pedido, pero queda anotado como hallazgo aparte (ver §26.4).

**Conclusión: hoy no existe ningún endpoint que le permita a un Agente ver o editar su propio perfil completo — hay que crear uno nuevo** (`GET`/`PUT /users/me`, con un DTO más completo que `UserMeDto`, o extender ese mismo endpoint).

### 26.2 Qué puede editar el usuario autenticado — restricciones, ✅ confirmadas por el responsable (2026-07-30)

Comparando contra `PUT /users/{id}` (hoy Admin-only, edita todo), **no todos los campos deberían ser auto-editables por un Agente**:

- **Editables razonablemente por el propio usuario**: `Nombre`, `MiddleName`, `Gender`, `Phone`, `Email`, `Address1`, `Address2`, `City`, `State`, `ZipCode`, `County` — son datos de contacto/identidad de la persona, no organizacionales.
- **`Agency`** — ✅ **confirmado: solo lectura en el perfil propio** (se ve, no se edita). Mismo criterio que ya se aplicó a `Customer.AgentId`/`RecordAgentId` (asignación organizacional, solo Admin la cambia) — coincide además con que hoy solo hay 2 valores reales posibles (§15.1), no es un campo de "texto libre de la persona".
- **`Rol`, `IsEncargado`, `IsActive`** — organizacionales/de permisos, deben seguir siendo Admin-only (ya lo son en `PUT /users/{id}`). No tiene sentido que aparezcan en absoluto en la pantalla de "mi perfil".
- **`Licensed`/`LicenseNumber`/`NpnNumber`/`NpnOverride`/`HasCompanyContract`/`ContractNumber`/`CompanyName`/`ContractsWanted`/`AdditionalInformation`** — campos del formulario de alta de Agente (§11), no mencionados en el pedido ni en la captura del legacy adjuntada (que solo muestra Name/Description/Phone/Email/Address). Fuera de alcance de "mi perfil" por ahora — son datos que hoy solo carga/edita un Admin al dar de alta o editar un agente desde `/agentes`, no parte de la ficha de perfil simple que se está pidiendo.
- **`Email`** — ✅ **confirmado: cambiar el propio email requiere confirmar con la contraseña actual**, mismo criterio que `POST /auth/change-password` (`currentPassword` + valor nuevo). Motivado por el caso de Alexander Centeno de esta misma sesión: el email es el campo de login, así que no alcanza con solo estar autenticado para cambiarlo — al implementar, el endpoint `PUT /users/me` tiene que validar la contraseña actual específicamente cuando el `Email` del body difiere del actual, devolviendo 400 si no matchea (mismo patrón de verificación que `AuthService.ChangePassword`, `BCrypt.Verify`).

### 26.3 Estado actual de `Profile.jsx` — conviene extender, no rehacer

`Profile.jsx` (108 líneas) es un componente único y simple: un `<h2>` + una tarjeta con el formulario de cambio de contraseña (`POST /auth/change-password`), sin ningún estado de navegación interna ni componente de menú. No hay tabs en ningún lugar de la app hoy — es un patrón nuevo para este código.

**El ícono de usuario en `Header.jsx` (`:92-128`) ya tiene su propio menú desplegable** (inline, `position:absolute`, sin usar `Modal` ni `ActionsMenu`) con 3 ítems: "Profile" (navega a `/profile`), "Help" (sin `onClick`, dead), "Logout". **Este menú no es el que hay que tocar** — el pedido es sobre la estructura *interna* de la página `/profile` en sí (hoy solo tiene una sección; hace falta una forma de elegir entre "Ver/editar perfil" y "Cambiar contraseña" una vez adentro).

### 26.4 Diseño del selector de sección — ✅ confirmado por el responsable (2026-07-30): 2 botones toggle, no tabs

Evaluadas las opciones mencionadas en el pedido:

- **`ActionsMenu` (⋮)**: descartado. Es un componente pensado específicamente para acciones de fila en una tabla (Policies/Customers/Agentes) — se posiciona con `position:fixed` + portal, ancla su menú a un botón chico dentro de una fila. Usarlo para navegar entre 2 secciones de una página completa sería forzar un patrón semánticamente distinto al que fue diseñado, y visualmente no hay precedente de un ⋮ como selector de sección en ningún lugar de la app hoy.
- **Tabs**: descartado. No existe ningún componente de tabs en el código hoy — habría que crear uno nuevo desde cero, más superficie de la necesaria.
- **✅ Confirmado — 2 botones tipo toggle, mismo patrón que ya usa `Customers.jsx`/`Policies.jsx`** para mostrar/ocultar el formulario de alta (`showForm ? "Cerrar formulario" : "+ Nuevo cliente"`, ver `Customers.jsx:194-200`): un par de botones ("Datos del perfil" / "Cambiar contraseña") arriba de la tarjeta, que controlan un estado local (`activeSection`) determinando qué sección se renderiza debajo. Menor superficie nueva (no hay que crear un componente de tabs), patrón ya validado visualmente en 2 pantallas de la app.

### 26.5 Alcance propuesto (sin implementar)

**Backend:**
- Nuevo DTO `UserProfileDto` (o extender `UserMeDto` con los campos que faltan — ver punto abierto único, abajo) con `Nombre`, `MiddleName`, `Gender`, `Email`, `Phone`, `Agency` (solo lectura, se expone igual para mostrarlo), `Address1`, `Address2`, `City`, `State`, `ZipCode`, `County`.
- `GET /users/me` — o se amplía el DTO que ya devuelve, o se agrega un endpoint nuevo más completo (a decidir al implementar, ver punto abierto único).
- `PUT /users/me` nuevo — mismo patrón que `PUT /users/{id}` pero resolviendo el `id` desde el JWT (`ClaimTypes.NameIdentifier`, mismo criterio que `PUT /users/me/language`), **sin** permitir tocar `Rol`/`IsEncargado`/`IsActive`/`Agency` (ver §26.2) ni los campos de agente (Licensed/NPN/contrato). Valida `currentPassword` contra el hash existente específicamente cuando `Email` cambia (§26.2).

**Frontend:**
- `Profile.jsx`: agregar el selector de sección de 2 botones (§26.4) + una sección nueva "Datos del perfil" (formulario con los campos de §26.2, similar en estructura a los campos de dirección que ya existen inline en `Agentes.jsx` — evaluar si conviene extraerlos a un componente compartido, mismo criterio que `CustomerFormFields.jsx` en su momento, §2). La sección "Cambiar contraseña" existente se mueve tal cual a la segunda sección, sin cambios de lógica.
- Sin cambios en `Header.jsx` — su menú desplegable actual (Profile/Help/Logout) sigue igual, solo sigue apuntando a `/profile`.

**Fuera de alcance de este rediseño:**
- El hallazgo aparte de `GET /users/{id}` sin restringir a "propio Id" (§26.1) — no es parte de este pedido, se deja anotado para evaluar aparte si corresponde restringirlo.
- El ítem "Help" del menú del Header, sin `onClick` (dead) — no mencionado en este pedido, no se toca.
- Campos de agente (Licensed/NPN/contrato/ContractsWanted/AdditionalInformation) — fuera de "mi perfil" según §26.2, siguen editables solo por Admin desde `/agentes`.

**Único punto abierto restante** (los otros 3 quedaron confirmados arriba — a criterio técnico al momento de implementar, el responsable no necesita resolverlo de antemano): si `GET/PUT /users/me` reemplaza el `UserMeDto`/`GET /users/me` actual, o se agrega un endpoint nuevo aparte — priorizando **no romper el uso actual que ya hace `AppLayout.jsx`** (reconciliación en background de `MustChangePassword`).

### 26.6 Implementación — ✅ Hecho (2026-07-30)

**Punto abierto de §26.5 resuelto**: se extendió `UserMeDto` en vez de agregar un endpoint nuevo — `AppLayout.jsx` solo lee `preferredLanguage`/`mustChangePassword` de la respuesta de `GET /users/me`, así que agregarle campos no le afecta, y evita mantener dos DTOs "yo mismo" en paralelo.

**Backend:**
- `UserMeDto` (`DTOs/Users/UserMeDto.cs`) extendido con `MiddleName`, `Gender`, `Phone`, `Agency`, `Address1`, `Address2`, `City`, `ZipCode`, `State`, `County`. `Agency` se expone (solo lectura en el frontend) pero no es editable desde este flujo.
- DTO nuevo `UserProfileUpdateDto` (`DTOs/Users/UserProfileUpdateDto.cs`) para `PUT /users/me` — **a propósito no tiene propiedades `Rol`/`IsEncargado`/`IsActive`/`Agency`**: como esos campos ni siquiera existen en la clase, el model binding de ASP.NET Core los ignora aunque alguien arme el body a mano (verificado, ver abajo).
- `PUT /users/me` nuevo en `UsersController.cs`, resuelve el usuario por `ClaimTypes.NameIdentifier` (mismo criterio que `PUT /users/me/language`). Si `Email` cambia respecto al valor actual (comparación case-insensitive), exige `CurrentPassword` y lo valida contra el hash (`BCrypt.Verify`, mismo criterio que `POST /auth/change-password`) antes de aplicar el cambio; si no cambia, no la pide. También chequea que el nuevo email no esté en uso por otro usuario (mismo patrón que el chequeo de nombre duplicado de `InsuranceCompaniesController`) antes de guardarlo, para no depender de que el índice único de la tabla tire una excepción 500 sin manejar.
- **Bug encontrado y corregido de paso**: `GET /users/me` (`Me()`) resolvía al usuario buscando por `User.Identity.Name` (el claim `Name` del JWT, que en este sistema es el email). Como el JWT no se reemite en el momento al cambiar el email desde `PUT /users/me`, la primera llamada a `GET /users/me` después de cambiar el email —con el token viejo, que sigue siendo válido hasta que expire o se refresque— buscaba por el email *anterior* y devolvía 404, rompiendo silenciosamente la reconciliación de `AppLayout.jsx` (que ignora errores que no sean 401). Se cambió a resolver por `ClaimTypes.NameIdentifier` (Id), igual que ya hacían `UpdateMe`/`UpdateMyLanguage`/`Logout` en el mismo controller — con eso el Id nunca cambia, así que no hay ventana rota. Verificado explícitamente (ver Verificación abajo).

**Frontend (`Profile.jsx`):**
- 2 botones toggle ("Datos del perfil" / "Cambiar contraseña") controlando un estado local `activeSection`, mismo estilo de color que el botón `showForm` de `Customers.jsx`/`Policies.jsx` pero como par de botones fijos (no uno solo que alterna texto), ya que acá se elige entre 2 secciones y no se abre/cierra un formulario.
- Sección "Cambiar contraseña" movida tal cual a un componente `ChangePasswordSection`, sin tocar su lógica (sigue pegándole a `POST /auth/change-password`).
- Sección nueva `ProfileDataSection`: carga `GET /users/me` al montar, formulario con los campos editables + `Agency` como `<input disabled>` de solo lectura. Si el valor de Email difiere del cargado originalmente, aparece el campo "Confirmá tu contraseña actual" (`required` solo en ese caso) y se manda `currentPassword` en el body solo cuando corresponde.
- **Sin componente compartido para los campos de dirección** (evaluado en §26.5): se armaron inline en `Profile.jsx`, reutilizando los datos `US_STATES`/`US_COUNTIES`/`GENDERS` ya existentes (mismos que usa `Agentes.jsx`) pero sin extraer un `ProfileFormFields.jsx` — es la única pantalla que los usa, así que extraerlos no evita duplicación real, solo agregaría una capa de indirección sin otro consumidor.

**Hallazgo durante la verificación — decisión del responsable**: el admin sembrado por `AdminUserSeeder` nunca carga `Address1`/`City`/`State`/`ZipCode`/`County` (quedan en `""`), y `UserProfileUpdateDto` los marca `[Required]` (mismo criterio que `UserUpdateDto`, según lo pedido) — `[Required]` de .NET rechaza string vacío, no solo `null`. Esto significa que en cualquier deploy nuevo, el primer admin que entre a "Datos del perfil" no puede guardar ni un cambio de Phone sin completar antes una dirección de EE.UU. completa. **Consultado, el responsable confirmó dejarlo tal cual** (mismo patrón que `PUT /users/{id}`, sin relajar la validación) — queda anotado acá por si en el futuro se vuelve a topar con el mismo síntoma.

**Verificación:**
- Backend: `dotnet build` sin errores (mismos warnings preexistentes de nulabilidad que ya tenían `UserResponseDto`/`UserCreateDto`, no introducidos por este cambio).
- Frontend: `npm run build` y `npm run lint` sin errores (1 warning preexistente en `Agentes.jsx`, no tocado acá).
- Backend, script contra la API real (`dotnet run` + llamadas HTTP encadenadas simulando `Profile.jsx`): login → `GET /users/me` → `PUT /users/me` cambiando solo Phone (200) → intento de inyectar `agency`/`rol`/`isEncargado`/`isActive` en el body (200, todos ignorados, `agency` siguió `null`) → cambio de Email sin `currentPassword` (400) → con `currentPassword` incorrecta (400) → con `currentPassword` correcta (200) → `GET /users/me` inmediatamente después **con el JWT viejo** (200, confirma el fix de `Me()`) → login con el email viejo (401) → login con el email nuevo (200) → revertido a `admin@wholecare.com` para dejar la base limpia.
- Frontend, en el navegador real (Vite dev server + API local, login como `admin@wholecare.com`): toggle entre las 2 secciones funciona y preserva el estado de cada una; cambiar solo Phone y guardar muestra "Datos actualizados correctamente."; cambiar Email dispara la aparición del campo de confirmación de contraseña; guardar con la contraseña incorrecta muestra el error del backend tal cual ("Para cambiar el email hay que confirmar la contraseña actual."); guardar con la contraseña correcta actualiza el email y hace desaparecer el campo de confirmación; navegar a otra pantalla (`/agentes`) después de cambiar el email **no** desloguea ni rompe la sesión (confirma el fix de `Me()` también end-to-end, no solo por script); campo `Agency` se muestra deshabilitado, sin forma de editarlo desde la UI.

---

## 27. Preview de PolicyDocument en el navegador (sin descargar) — Parte 1 (PDF/imagen) ✅ Hecho (2026-07-31), Parte 2 (`.docx`) ✅ Cerrada por descarte (2026-08-05, §1.12)

Pedido: hoy los documentos cargados en el detalle de una póliza (`PolicyDocument`, §1.7) solo se pueden descargar — el cliente pidió poder **visualizarlos** en el navegador sin bajar el archivo primero.

### 27.1 Parte 1 — Preview de PDF/imagen (caso simple, cubre lo que existe hoy)

**Diagnóstico del estado actual:**

- **Almacenamiento**: filesystem del servidor, no blob en SQL ni storage externo. `PolicyDocumentStorage` (`Services/PolicyDocumentStorage.cs`) guarda cada archivo en `App_Data/PolicyDocuments/{policyId}/{guid}{extension}`, fuera de `wwwroot`. La tabla `PolicyDocuments` solo guarda metadata (`OriginalFileName`, `StoredFileName`, `ContentType`, `SizeBytes`).
- **Tipos permitidos al subir**: whitelist estricta en `Utils/FileValidationHelper.cs`, no "cualquier cosa" — solo `.pdf`, `.docx`, `.jpg`, `.jpeg`, con validación en cascada de extensión + tamaño (máx. 5 MB) + contenido real por magic bytes (`%PDF`, firma JPEG, ZIP+entradas OOXML reales para `.docx`, descarta un `.zip` renombrado).
- **Estado real en la base**: consultada la tabla `PolicyDocuments` — hoy hay 1 solo documento cargado, un `.pdf` (`application/pdf`). Como el upload solo acepta esos 4 tipos, el universo futuro se limita a ellos.

**Tres obstáculos identificados, hay que resolver los tres — no alcanza con tocar solo uno:**

1. **Backend fuerza descarga siempre.** `DownloadDocument` (`Controllers/PoliciesController.cs:376-386`) hace `return PhysicalFile(path, document.ContentType, document.OriginalFileName);`. El Content-Type devuelto ya es el real (`application/pdf`, `image/jpeg`, resuelto por `FileExtensionContentTypeProvider` en el upload), pero pasar el 3er parámetro (`fileDownloadName`) hace que ASP.NET Core agregue siempre `Content-Disposition: attachment; filename=...` — eso es lo que fuerza "Guardar como" incluso para tipos que el navegador podría mostrar nativamente.
2. **El endpoint exige JWT Bearer.** `PoliciesController` tiene `[Authorize]` a nivel de clase. Un `<a href="...">` o `window.open(url)` apuntando directo a la URL de la API fallaría con 401, porque el navegador no adjunta el header `Authorization` en una navegación normal de página — solo `apiFetch` (`src/api.js`) lo hace, leyendo el token de `localStorage`. Esto descarta la solución "más simple" de abrir la URL de descarga directamente en una pestaña nueva.
3. **El frontend actual fuerza descarga también, independientemente del backend.** `handleDownloadDocument` (`src/pages/Policies.jsx:363-381`) ya pasa por `apiFetch` (por el punto 2), pero siempre convierte la respuesta en blob, crea un `<a>` y le setea `link.download = doc.originalFileName` antes de simular el click — eso fuerza "Guardar como" sin importar qué `Content-Disposition` devuelva el backend. Aunque se arregle el punto 1, este código seguiría descargando.

**Plan de implementación propuesto:**

- **Backend**: en `DownloadDocument`, cambiar a `Content-Disposition: inline` para los tipos previsualizables (o para todos — a decidir al implementar; ver nota de compatibilidad abajo). Puede resolverse seteando el header manualmente en vez de usar el overload de `PhysicalFile` que fuerza `attachment` (por ejemplo, devolver el `FileStreamResult`/`PhysicalFileResult` sin `fileDownloadName` y setear `Response.Headers.ContentDisposition` a mano con `Inline`).
- **Frontend**: agregar una acción "Ver" (además de "Descargar") junto a cada documento en la lista (`Policies.jsx`). El flujo: fetch autenticado vía `apiFetch` (mismo mecanismo que ya usa `handleDownloadDocument` para sortear el punto 2) → `res.blob()` → `URL.createObjectURL(blob)` → `window.open(url, "_blank")` (sin setear `download` en ningún `<a>`, a diferencia de `handleDownloadDocument`). El botón "Descargar" existente queda igual, sin tocar.
- **Nota de compatibilidad**: si se pasa a `inline` para `.docx` también, el navegador no sabría renderizarlo (no hay soporte nativo) y probablemente dispararía una descarga igual o un error, dependiendo del navegador — por eso probablemente convenga que el backend decida `inline` vs `attachment` según `ContentType` (`inline` solo para `application/pdf` e `image/jpeg`), dejando `.docx` con el comportamiter actual de descarga hasta que se resuelva la Parte 2.

### 27.2 Parte 2 — Investigación de visor externo para `.docx` (sin implementar)

`.docx` es el único de los 4 tipos permitidos que ningún navegador abre nativamente. Opciones evaluadas:

**Opción A — Google Docs Viewer** (`docs.google.com/viewer?url=<url>&embedded=true`)
- Cómo funciona: requiere una URL **pública** del archivo — los servidores de Google la descargan y renderizan como imagen/canvas.
- No es un producto oficial ni documentado por Google para embeber contenido de terceros; la página de configuración standalone fue discontinuada hace años y lo que queda es un endpoint no oficial, sin SLA ni garantía de que siga funcionando.
- Costo: gratis, pero sin límites publicados ni soporte — puede dejar de funcionar sin aviso.
- Seguridad: expone el documento completo (con datos de clientes, potencialmente sensibles) a un tercero externo sin acuerdo formal de tratamiento de datos.

**Opción B — Microsoft Office Online Viewer** (`view.officeapps.live.com/op/embed.aspx?src=<url>`)
- Cómo funciona: igual que Google — requiere URL pública, Microsoft la descarga y renderiza.
- Confirmado por Microsoft Q&A: **no está oficialmente soportado para uso comercial/de producción de terceros** — Microsoft recomienda Office Online Server (self-hosted) o integraciones de Microsoft 365 para eso. Es decir, usarlo así sería apoyarse en un comportamiento no garantizado.
- Costo: gratis, mismo problema de falta de SLA/soporte.
- Seguridad: mismo problema que la Opción A — el documento sale a un tercero externo.

**Opción C — Conversión server-side a PDF (LibreOffice headless)**
- Cómo funciona: el backend convierte el `.docx` a PDF on-the-fly (`soffice --headless --convert-to pdf`) al pedir el preview, y sirve el PDF resultante reutilizando la solución de la Parte 1 (preview nativo de PDF).
- Seguridad: el archivo nunca sale del servidor propio — no hay exposición a terceros.
- Costo/limitaciones: gratis (LibreOffice es software libre), pero agrega infraestructura — instalar LibreOffice en el servidor/contenedor, latencia de conversión en cada request (o cachear el PDF generado), y posibles problemas de fidelidad en documentos con formato complejo.

**Opción D — Librería client-side (`docx-preview`, npm)**
- Cómo funciona: renderiza el `.docx` a HTML/CSS **enteramente en el navegador**, sin ningún servidor ni tercero externo — reutiliza el mismo fetch autenticado que ya existe (Parte 1: `apiFetch` → blob), pasando el blob directo a `docx.renderAsync(blob, container)`.
- Seguridad: la mejor de las cuatro — el archivo nunca sale de la sesión autenticada del usuario, no hay URL pública ni tercero involucrado en ningún momento.
- Costo/limitaciones: gratis, mantenida activamente (según investigación, última versión reciente, uso activo vía npm). Limitación real: renderiza a HTML semántico, no es pixel-perfect — documentos con formato muy complejo (ciertos estilos, objetos incrustados, control de cambios) pueden no verse idénticos al original en Word. Para los documentos de esta app (probablemente formularios/contratos simples) es probablemente suficiente, pero no está garantizado sin probarlo contra ejemplos reales.

**Recomendación:**

Descartar A y B de entrada — no es solo una cuestión de límites de uso, es que **exponer documentos de pólizas de clientes (potencialmente con datos sensibles) a través de una URL pública hacia un servicio de terceros no oficialmente soportado** es un riesgo de privacidad/compliance que no se resuelve con una "URL firmada de corta duración": aunque se firme y expire en minutos, en esa ventana el archivo completo pasa por la infraestructura de Google o Microsoft sin que haya un acuerdo de tratamiento de datos de por medio, y ninguna de las dos empresas garantiza el comportamiento del servicio.

Entre C y D, **la Opción D (`docx-preview` client-side) es la recomendada**: mismo nivel de seguridad que la Parte 1 (nunca sale del servidor/sesión autenticada), cero infraestructura nueva en el backend, y reutiliza el mismo patrón de fetch ya construido. El trade-off (fidelidad no pixel-perfect) es aceptable si el objetivo es "puedo ver rápido qué dice el documento sin descargarlo" y no "reemplazo de abrir el archivo en Word". La Opción C queda como alternativa si al probar D con documentos reales la fidelidad resulta insuficiente.

**Punto abierto para el responsable (histórico, ya no aplica)**: decidir, con documentos `.docx` reales de la app en mano, si vale la complejidad de D (o C) — o si simplemente se mantiene "solo descargar" para `.docx` y el preview in-browser queda limitado a PDF/imagen (Parte 1), que ya cubre el único documento existente hoy en la base.

**✅ Cerrada por descarte (2026-08-05):** §1.12 restringió el upload de documentos a PDF/JPG/JPEG/PNG, sacando `.docx` de los formatos permitidos — ya no se pueden subir `.docx` nuevos. Verificado con SQL directo contra `PolicyDocuments` que tampoco había ningún `.docx` ya subido (0 filas, la tabla solo tenía 2 PDF + 1 JPEG) — se pidió explícitamente por el responsable borrar cualquier `.docx` legacy que hubiera para no dejar la base "sucia" con un formato que ya no se va a permitir cargar, pero no hizo falta borrar nada porque no había ninguno. Con esto, la Parte 2 de §27 (investigación de visor externo para `.docx`) queda cerrada por descarte: no hay ni habrá `.docx` en el sistema para previsualizar.

### 27.3 Implementación de la Parte 1 (PDF/imagen) — ✅ Hecho (2026-07-31)

Implementado exactamente según el plan de §27.1, resolviendo los 3 obstáculos identificados.

**Backend (`Controllers/PoliciesController.cs`):**
- `DownloadDocument` (`{id:int}/documents/{documentId:int}`) pasó a aceptar un query param opcional `[FromQuery] bool inline = false` — **un solo endpoint**, no uno nuevo separado, para no duplicar la resolución de `GetDocument`/verificación de archivo en disco y para que el botón "Descargar" existente siga golpeando la misma URL de siempre sin ningún cambio de contrato.
- Sin el query param (o con `inline=false`), el comportamiento es **bit a bit idéntico al de antes**: mismo `return PhysicalFile(path, document.ContentType, document.OriginalFileName)`, mismo `Content-Disposition: attachment`. El botón "Descargar" no se tocó en el frontend y no necesitaba tocarse en el backend tampoco.
- Con `inline=true` **y** `document.ContentType` en la whitelist `PreviewableContentTypes` (`application/pdf`, `image/jpeg`), se arma el header a mano con `Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline")` + `SetHttpFileName(...)` (la misma clase que usa ASP.NET Core internamente para el caso `attachment`, así que el nombre de archivo con caracteres especiales/acentos se encodea correctamente vía RFC 5987 — verificado con el documento real, que tiene una tilde en el nombre, ver Verificación) y se devuelve `PhysicalFile(path, document.ContentType)` sin el 3er parámetro.
- **Decisión explícita, coincide con la nota de compatibilidad del plan original**: si `inline=true` se pide para un tipo no previsualizable (`.docx`, o cualquier tipo futuro), el backend **ignora el pedido y sigue devolviendo `attachment`** — la decisión de qué es previsualizable vive en el backend, no se confía en que el frontend nunca mande el query param para un tipo incorrecto.

**Frontend (`src/pages/Policies.jsx`):**
- `PREVIEWABLE_DOCUMENT_TYPES` (`["application/pdf", "image/jpeg"]`) nueva, junto a las otras constantes de documentos (`ALLOWED_DOCUMENT_EXTENSIONS`, etc.).
- `handleViewDocument(doc)` nueva, junto a `handleDownloadDocument` (sin tocarla): `apiFetch(".../documents/{id}?inline=true")` → `res.blob()` → `URL.createObjectURL(blob)` → `window.open(url, "_blank")` — **sin** crear ningún `<a>` ni setear `download`, a diferencia de `handleDownloadDocument`. Mismo mecanismo de auth que ya usa la descarga (`apiFetch` adjunta el Bearer token desde `localStorage`), así que sortea el obstáculo 2 del diagnóstico igual que ya lo hacía el flujo de descarga.
- Botón "Ver" agregado al menú de opciones (⋮) de cada documento, **antes** de "Descargar" — condicionado a `PREVIEWABLE_DOCUMENT_TYPES.includes(doc.contentType)`: para `.docx` el botón directamente no se renderiza (se eligió ocultar en vez de deshabilitar+explicar, mismo criterio que ya usa la app para mostrar/ocultar el botón de WhatsApp condicional a que haya teléfono — no hay precedente de botón deshabilitado-con-tooltip en ningún lugar de esta app).
- Claves i18n nuevas `documents.view`/`documents.viewError` (es/en), junto a las ya existentes de `download`/`downloadError`.

**Nota técnica descubierta durante la verificación (no cambia nada de la implementación, solo aclara por qué funciona)**: el header `Content-Disposition` del backend no es, en rigor, lo que hace que "Ver" abra sin descargar — eso lo logra el propio flujo del frontend (`blob()` + `URL.createObjectURL` + `window.open`, que nunca deja que el navegador vea la respuesta HTTP cruda ni su `Content-Disposition`, un blob: URL no lleva headers). El cambio de backend sigue siendo correcto y necesario tal como está en el plan aprobado: dejar `inline` disponible en el endpoint es lo que documenta la intención real de cada tipo de contenido a nivel de API (útil para cualquier consumidor futuro que sí navegue directo a la URL, ej. Swagger/Postman con el token a mano) y es lo que se pidió implementar explícitamente.

**Verificado:**
- `dotnet build`: 0 errores, mismos warnings preexistentes de nulabilidad en DTOs de `Users` (sin relación).
- `npm run build`/`npm run lint`: limpios, mismo warning preexistente de `exhaustive-deps` en `Agentes.jsx`.
- Con `curl` contra la API real (login como `admin@wholecare.com`, documento real `RecetaMédica-Javier.pdf`, `application/pdf`, único documento cargado hoy en la base — Policy 381840, Maite Medina):
  - `GET .../documents/2002?inline=true` → `200`, `Content-Disposition: inline; filename=RecetaM_dica-Javier.pdf; filename*=UTF-8''RecetaM%C3%A9dica-Javier.pdf`.
  - `GET .../documents/2002` (sin query param) → `200`, `Content-Disposition: attachment; filename=...` — **idéntico al comportamiento de antes de este cambio**, confirma que "Descargar" no se rompió.
- En el navegador real (Vite dev server + API local, login como Admin, Policy 381840 → modal de detalle → sección Documents): el menú de opciones del documento muestra **View / Download / Delete** (los 3, porque es un `.pdf`); click en "View" dispara `GET .../documents/2002?inline=true` (confirmado por Network, `200 OK`) sin errores de consola; click en "Download" dispara `GET .../documents/2002` sin el query param (confirmado por Network, `200 OK`), sin cambios respecto al comportamiento ya conocido.
- No se pudo confirmar visualmente el render de la pestaña nueva del PDF en el navegador automatizado de esta sesión (limitación del tooling con `window.open`, no de la app) — la verificación de red + los headers reales por `curl` cubren el mecanismo completo end-to-end; el `.pdf` real usado en la prueba es el único documento existente en la base hoy, mismo que documenta §27.1.

Con esto, §27 Parte 1 queda completa. Parte 2 (`.docx` vía `docx-preview` client-side u otra opción) sigue con el punto abierto de §27.2, sin implementar.

---

## 28. Corrección de contraseñas de agentes migrados + email de bienvenida — Paso (a) ✅ Hecho (2026-07-31), Paso (b) 📋 documentado

Diagnóstico previo: el import original (§15.2) asignó **una sola contraseña temporal compartida** a los 41 agentes reales (`Program.cs` la generaba una vez y se la pasaba a `AgentImporter.RunAsync` para los 41). Nunca se envió ningún email — hoy `IEmailService` (`BrevoEmailService`/`ConsoleEmailService`, §10.3) solo está conectado al flujo de "olvidé mi contraseña", no al alta/import de agentes.

### 28.1 Paso (a) — Contraseña individual por agente — ✅ Hecho
`AgentPasswordResetRunner.cs` nuevo (mismo patrón que `AgentContactBackfillRunner`, §17.1: re-lee el xlsx de agentes y matchea por Email contra los `User` ya existentes, sin `ImportPipeline`/`EntityMatcher`). A diferencia de §15.2, genera una contraseña con `RandomNumberGenerator` **por fila**, no una compartida. `MustChangePassword` no se toca (ya estaba en `true` para los 41). Nuevo modo `--reset-agent-passwords` en `WholeCareInsurance.Migration`, combinable con `--dry-run`/`--commit --confirm` (mismo criterio que el resto de los runners).
- **Backup previo**: `D:\backups\WholeCareInsuranceDb_pre_agent_password_reset_20260731.bak` (`BACKUP DATABASE` completo, mismo criterio que §22.4 y siguientes).
- Las contraseñas generadas **no se persisten en el repo**: se guardan en `D:\backups\agent-temp-passwords-<timestamp>.txt` (fuera del repo por completo, no solo gitignored — mismo criterio que los `.bak`), además de mostrarse en consola.
- **Caso Alexander Centeno** (`User.Id=3011`, Admin): el xlsx original trae `alexfinancial22@gmail.com`, pero su email real hoy es `info@wholecareinsurancellc.com` (cambio intencional hecho en una sesión anterior vía el autoservicio de `Profile.jsx`, §26 — no un dato desactualizado a ignorar). Resuelto con un override explícito en el runner (`KnownEmailDriftToUserId`, matchea por `User.Id` directo para este caso puntual, documentado en el código con el email viejo/nuevo).
- **Mejora futura, no implementada** (evaluada, no es simple de hacer ahora): que el runner prefiera matchear por `User.Id` en general en vez de por Email del xlsx requeriría un mapeo persistido fila-del-xlsx → `User.Id` desde la corrida original de §15.2 — ese mapeo nunca existió (el reporte original solo se imprimió en consola, sin guardar Ids). Sin ese mapeo, Email es la única clave natural disponible; el caso de Centeno se resolvió con un override puntual en vez de un mecanismo genérico.
- **`--dry-run` confirmado**: 41/41 agentes matcheados (40 por Email + 1 por el override de Id).
- **`--commit --confirm` aplicado**. Verificado con SQL directo post-commit: `SELECT COUNT(*), COUNT(DISTINCT PasswordHash) FROM Users WHERE MustChangePassword = 1` → `41` y `41` (ninguna coincidencia); `SELECT PasswordHash, COUNT(*) FROM Users GROUP BY PasswordHash HAVING COUNT(*) > 1` → 0 filas en toda la tabla `Users`, no solo entre los 41 — confirma que no quedó ningún hash compartido en ningún lado.
- Contraseñas reales de esta corrida guardadas en `D:\backups\agent-temp-passwords-20260731-161438.txt` (fuera del repo) — insumo para el paso (b) cuando corresponda.

### 28.2 Paso (b) — Email de bienvenida con contraseña temporal — 📋 Documentado, sin implementar
Bloqueado a propósito hasta que el VPS/Test esté desplegado y `Frontend__BaseUrl` (env var ya prevista en §8.1) apunte a la URL real — el patrón existente de `IEmailService` (`AuthService.ForgotPassword`, §10.3) arma el link con `_config["Frontend:BaseUrl"]` (default `http://localhost:5173`), así que generar y enviar el email de bienvenida desde local hoy produciría un link roto (`localhost`) para los agentes reales.

**Requisitos confirmados para cuando se implemente:**
- Runner/script de envío nuevo (mismo proyecto `WholeCareInsurance.Migration`, reusando `IEmailService`/`BrevoEmailService` del `api` o una llamada HTTP equivalente) que acepte un **filtro opcional** (lista de emails o de `User.Id`) para poder correr contra un subconjunto específico.
- **Sin el filtro** (o con un flag explícito tipo `--all`): corre contra los 41 completos — pensado para el momento de Producción.
- **Con el filtro**: corre solo contra los 2-3 agentes de prueba que el responsable elija (gente que él controle) — pensado para Test, para no mandar emails reales a agentes que todavía no deberían recibirlos.
- Mismo criterio de `--dry-run` que el resto de los runners de este proyecto: mostrar a quién se le enviaría (con o sin filtro) antes de disparar cualquier envío real.
- Precondición dura: `Brevo__ApiKey`/`Brevo__SenderEmail`/`Frontend__BaseUrl` reales seteados en el ambiente donde se corra (si no, cae a `ConsoleEmailService` y no sale ningún email real, ver §8.1).

---

## 21. Orden sugerido de trabajo

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
19. Firma digital de consentimiento — proveedor definido por el responsable (2026-08-05): Dropbox Sign (HelloSign), ver §4.1; costo de la suscripción confirmado (lo asume el responsable directamente); implementación técnica todavía no iniciada. Notificación ya definida (email + SMS vía Twilio) y cuenta de Twilio ya creada (modo Trial, apta para desarrollo — §4.1/§4.4). Evaluación de SendGrid para email transaccional de agentes (§4.3) y plan de implementación técnica de Twilio (§4.4) documentados, no iniciados.
20. ~~Infraestructura de hosting (VPS) — Dockerfiles/compose/README~~ ✅ Hecho (§8.1); falta el despliegue real al VPS
21. ~~Campos de plan (ACA) y financieros en Policy~~ ✅ Hecho (§1.11)
22. ~~Migración de datos del sistema anterior~~ ✅ Hecho (§7): script implementado y corrido con `--commit`. La reasignación de `Customer.AgentId` en filas con fallback se cerró en el ítem 37 (§15.3) — 1178/1179 pólizas (99.92%); queda 1 caso puntual sin resolver, ver ítem 46 (§23.2).
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
43. ~~Rediseño de la tabla de Policies (12 columnas nuevas, scroll horizontal, `Policy.CreatedAt`/`Policy.RenewalStatus` nuevos, `CustomerResponseDto.AgentAgency`)~~ ✅ Hecho (§18), incluye 2 hallazgos fuera del plan original: menú de acciones (⋮) reutilizable en Policies/Customers/Agentes y fix de un bug de layout preexistente en `AppLayout.jsx` (§18.11)
44. ~~Deuda técnica — ESLint `react-hooks/set-state-in-effect`~~ ✅ Hecho (§20): 5 casos de fetching corregidos vía `queueMicrotask` (Dashboard/Customers/Agentes/InsuranceCompanies + un 6to caso silencioso en Policies no reportado por el linter, ver §20.1) + el caso distinto de estado derivado (§20.3, extracción de `TitularLifeSection.jsx` con `key={customerId}`) — 6/6 resueltos, sin ningún caso pendiente.
45. ~~Dashboard "Additional statistics" — bug de normalización de mayúsculas en `Customer.City`~~ ✅ Hecho: backfill de datos (336 filas corregidas con backup previo, 304→191 valores distintos, §22.4) + freno al `<input>` libre (`<datalist>` de autocomplete + normalización a Title Case en el blur, §22.5 Parte A) + gráficos tipo torta/dona con top 9 + Otros para Aseguradora/Condado/Ciudad, reemplazando las 3 listas planas (§22.5 Parte B)
46. ~~Customers duplicados por bug de matching en la migración (Doris Maldonado, Mariana Salvador Cruz)~~ ✅ Hecho: los 2 casos confirmados fusionados con backup previo y verificación (§23.2); refuerzo del matching (`_customerByDob` + `CheckPossibleDuplicate`, solo reporta, nunca fusiona automáticamente) implementado y verificado (§23.3). Queda pendiente, aparte, revisar el `AgentId` en fallback Admin encontrado en la póliza de Mariana (§23.2, mismo gap que §7)
47. ~~Rediseño de la relación Customer ↔ Customer (titular/dependiente-aplicante)~~ ✅ Hecho (§24): Fase 1 (modelo `CustomerRelationship`, migración) + Fase 2 (backfill de 884 filas, §24.6) + Fase 3 (endpoints backend, filtro `?role=`, KPI `UniqueMembersCount`, §24.7) + Fase 4 (filtro de rol en Customers.jsx, tarjeta "Personas únicas" en el Dashboard, §24.8) + backfill de los 75 dependientes de email real (§24.10) + Clara/Elizabeth revisadas e incluidas (§24.11) — `CustomerRelationships` en 961 filas, todo verificado end-to-end con SQL directo, curl y navegador. Solo queda el backfill manual de emails placeholder (§24.9), trabajo operativo, no de desarrollo.
48. ~~`Customer.Email` opcional (reemplaza el placeholder `noemail+P...` por `NULL`)~~ ✅ Hecho (§25): Fase A (schema + índice único filtrado, DTOs, fix de `""→null` en el frontend) + Fase B (backfill de los 947 existentes a `NULL`, backup previo, verificado 947/2128/0 residuales/1181 con email real) + Fase C (`EntityMatcher.ResolveUniqueEmailAsync` devuelve `null` en vez de generar placeholder, para futuras re-importaciones) — todo verificado con SQL directo, curl y build.
49. ~~Rediseño de la sección "Profile" (ver/editar datos del perfil + cambio de contraseña)~~ ✅ Hecho (§26): `PUT /users/me` nuevo (extiende `UserMeDto` en vez de agregar endpoint aparte, §26.6) con `Agency` de solo lectura y confirmación de contraseña actual al cambiar `Email`, selector de sección con 2 botones toggle (no tabs) en `Profile.jsx`, incluye el fix de un bug encontrado de paso (`GET /users/me` resolvía por email en vez de por Id, rompía la reconciliación de `AppLayout.jsx` tras cambiar el propio email).
50. ~~Preview de PolicyDocument en el navegador sin forzar descarga — Parte 1 (PDF/imagen)~~ ✅ Hecho (§27.1/§27.3): endpoint `DownloadDocument` acepta `inline=true` para tipos previsualizables (`application/pdf`, `image/jpeg`, y `image/png` desde §1.12), botón "Ver" nuevo junto a "Descargar" en Policies.jsx. Parte 2 (`.docx` vía `docx-preview` u otra opción) ~~sigue ⏸ sin implementar~~ ✅ Cerrada por descarte (§27.2, §1.12): ya no se pueden subir `.docx` y no había ninguno legacy en la base (verificado con SQL directo).
51. ~~Corrección de la contraseña única compartida de los 41 agentes migrados (§15.2) por contraseñas individuales~~ ✅ Hecho (§28.1): 41/41 con `PasswordHash` distinto entre sí (verificado con SQL directo, 0 duplicados en toda la tabla `Users`), incluye el caso de Alexander Centeno (email cambiado post-import vía §26, resuelto con override manual por `User.Id`). Email de bienvenida con la contraseña (paso b, §28.2) documentado con filtro Test/Producción, bloqueado hasta el despliegue al VPS (`Frontend__BaseUrl` real).
52. ~~Restringir formatos de archivo permitidos en upload de documentos~~ ✅ Hecho (§1.12): lista permitida pasó de PDF/DOCX/JPG/JPEG a PDF/JPG/JPEG/PNG (`FileValidationHelper.cs` y `ALLOWED_DOCUMENT_EXTENSIONS` en `Policies.jsx`), con magic bytes nuevos para `.png` y mensajes de error (backend + i18n ES/EN) actualizados. PNG sumado también a `PREVIEWABLE_DOCUMENT_TYPES`/`PreviewableContentTypes` para preview inline. Verificado con curl (`.docx` rechazado, `.png` aceptado y previsualizable) y en la app corriendo (`accept` del input actualizado); `dotnet build`/`npm run build`/`npm run lint` limpios. Verificado con SQL directo que no había ningún `.docx` legacy en `PolicyDocuments` (0 filas, tabla con solo 2 PDF + 1 JPEG) — no hizo falta borrar nada, pero cierra por descarte la Parte 2 de §27 (preview de `.docx`, §27.2).
