# Pendientes — WholeCareInsurance

> Auditado contra código real el 2026-07-13 (modelos, DTOs, migraciones aplicadas y componentes de frontend). Donde el pedido del responsable no coincidía con lo implementado, se priorizó lo verificado en código — ver notas "⚠️ Discrepancia" en los puntos afectados.

---

## 1. Policies — campos y funcionalidades

### 1.1 Campo Tipo (dropdown) — ✅ Hecho
`Type` en `Policy` restringido a `Obama Care`, `Salud`, `Auto`, `Otro` vía `[AllowedValues]` en `PolicyCreateDto`. `<select>` en el formulario, igual patrón que el resto de los enums.

### 1.2 Dependientes (vínculo con Customers existentes) — ✅ Hecho
Los dependientes son **Customers** vinculados a un Customer principal dentro de una póliza (tabla intermedia `PolicyDependents`, `PolicyId`+`CustomerId`).
- Endpoints: `GET/POST /api/policies/{id}/dependents`, `PUT/DELETE /api/policies/{id}/dependents/{customerId}`.
- Frontend: sección "Dependientes" en el formulario de Policies, visible solo al **editar** una póliza ya guardada. El buscador filtra en el cliente sobre la lista de `customers` ya cargada — **solo permite vincular Customers que ya existen en el sistema, no crear uno nuevo desde ahí.** Ver §2 (nuevo pendiente).

### 1.3 Buscador / filtro de pólizas — ✅ Hecho
`GET /api/policies?firstName=&lastName=&policyNumber=&status=&type=&insuranceCompany=` con `Where` dinámico contra la DB (`PolicyService.Search`). Filtros combinables (AND), barra de filtros en el frontend con Search/Clear.

### 1.4 Vista de detalle de póliza — ✅ Hecho (contenido base, faltan campos por definir)
Modal con datos de la póliza, datos del titular, lista de dependientes y documentos (§1.7). Pendiente: el responsable aún no definió qué información adicional debería mostrarse — sumar cuando se defina.

### 1.5 Campo Compañía aseguradora (dropdown) — ✅ Hecho, confirmado cerrado
`InsuranceCompany` en `Policy` (`Models/Policy.cs`), `[Required]` + `[AllowedValues("WholeCareInsurance", "Otro")]` en `PolicyCreateDto`. Migración `20260710172345_AddPolicyInsuranceCompany` aplicada. `<select>` en formulario, columna en tabla, filtro superior y línea en modal de detalle. `PolicyService.Search` soporta filtro `insuranceCompany`. No quedó nada abierto en este punto.

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
`PolicyCreateDto.Status` ahora restringido vía `[AllowedValues]` a 8 valores canónicos en español (mismo patrón que `Type`/`MigrationStatus`): `Draft`, `Pendiente`, `Cancelado`, `Por procesar`, `En proceso`, `En corrección`, `Procesado`, `Cambio de agente`. Default cambiado de `"Active"` a `"Draft"`. Traducciones EN agregadas en `en/enums.json` (`Pending`, `Canceled`, `To be processed`, `In Process`, `By correction`, `Processed`, `Agent change`).
- Migración `20260713180205_AddPolicyStatusEnum` aplicada: remapea datos existentes (`Cancelled`→`Cancelado`, `Active`/`activa`→`Procesado`, `Expired`→`Cancelado`) con `ELSE Status` como red de seguridad para valores no contemplados. Verificado contra la base de dev (0 pólizas al momento del cambio, por lo que no hubo remapeo real que auditar, pero la lógica quedó lista para Test/Prod).
- Verificado con curl: `"Active"` (valor viejo) rechazado con 400; `"En corrección"` (valor nuevo) aceptado con 201.
- Nota: el valor `"Actualizado"` que aparecía en la referencia visual original del Dashboard (§9.2) **no forma parte de este enum** — se reemplazó por `Pendiente` y `En corrección` en la lista final acordada. Ver §9.2, ya actualizada.

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
`IsEncargado` (bool) en `Models/User.cs:10`, checkbox en el formulario de Agentes (`Agentes.jsx:155`), usado para filtrar el dropdown de Agente Record en Customers (§3.1). No queda nada pendiente en este punto.

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

---

## 6. Dashboard y UX general

### 6.1 Dashboard — ver §9
Placeholder, bloqueado hasta tener la data migrada (§7).

### 6.2 Selector de idioma (Español/Inglés) en el Header — ✅ Hecho, confirmado
`react-i18next` con diccionarios por namespace. `translateEnum()` desacopla el valor guardado en la DB (español) del texto mostrado. `User.PreferredLanguage` (default `"en"`) persistido vía `PUT /users/me/language`. Sin cambios desde la última revisión — sigue sin nada pendiente en este punto.

---

## 7. Migración de datos del sistema anterior — ⏸ Bloqueado, en espera de respuesta

Se solicitó al responsable del proyecto el archivo de export **completo** (todas las pólizas, todos los tipos en un solo archivo, no separado por tipo). Quedaron 4 preguntas enviadas, **en espera de respuesta**:
1. Si la columna "Members" trae solo cantidad o el detalle completo de cada dependiente.
2. Si existe un ID interno para Agentes/Agencias además del nombre.
3. Si el export incluye solo pólizas activas o también históricas/canceladas.
4. Diccionario de datos para: Reference, Marketplace ID, Contract identification, Renewal status, Confirmed consent.

**Columnas detectadas** en la pantalla de export del sistema anterior (referencia para el futuro mapeo): Reference, Agency, Agent, Full name, First/Middle/Last name, DOB, Gender, Email, Phone, Legal Status, SSN, Green card, Work permit, Estado civil, Address 1/2, City, State/Province, Zip code, County, Employer name, Company Phone, Position/Occupation, Annual income, Policy number, Marketplace ID, Contract identification, Number of applicants, Effective date, Company, Insurance plan, Type of plan, Tax Credit/Subsidy, Monthly premium amount, Status, Tags, Period, Confirmed consent, Registration date, Update date, Renewal status, Members.

> Nota: buena parte de estas columnas ya mapean directo a los campos nuevos de Customer (§3.2) y Policy (`Period`/`Number of applicants`, §1.8/§1.9) — todos cerrados. Ya no hay campos pendientes de agregar antes de diseñar el script de migración; solo falta el archivo real + las 4 respuestas de arriba.

**BLOQUEADO:** no diseñar el script de migración hasta recibir el archivo real + las 4 respuestas. Se probará primero contra la base del ambiente de test (§8.1), nunca directo contra producción.

**Punto abierto no bloqueante:** cada dependiente en el sistema anterior tiene un campo "Policy number" individual — no está claro su propósito, aclarar con el responsable más adelante (no urgente).

---

## 8. Hosting y despliegue (VPS)

### 8.1 Infraestructura — ⏸ Pendiente de implementación (Dockerfiles + compose + README)
VPS ya comprado y corriendo: Ubuntu 24.04, KVM2 (2 CPU, 8GB RAM, 100GB disco), con **EasyPanel** preinstalado.

**Decisiones tomadas:**
- SQL Server como **contenedor** Docker (`mcr.microsoft.com/mssql/server`) — no instalación nativa, por incompatibilidad de Ubuntu 24.04 con SQL Server nativo.
- Un solo contenedor de SQL Server compartido entre test y producción, con 2 bases de datos separadas (`WholeCareInsuranceDb_Test` y `WholeCareInsuranceDb_Prod`) — por limitación de RAM (8GB totales).
- Frontend: `VITE_API_URL` se resuelve vía build-arg por ambiente (no runtime) — cada ambiente reconstruye su propia imagen.
- Migraciones EF Core: auto-migrate al iniciar el contenedor de la API (`dbContext.Database.Migrate()` en el startup).
- Variables de entorno mapeadas: `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenMinutes`, `Cors__AllowedOrigin`, `ASPNETCORE_ENVIRONMENT`.

**Cambios de código pendientes identificados** (no solo config):
- Quitar/condicionar `app.UseHttpsRedirection()` fuera de Development (conflicto con el proxy de EasyPanel) + agregar `UseForwardedHeaders` para `X-Forwarded-Proto`.
- Mover CORS `AllowedOrigin` (hoy hardcodeado a `http://localhost:5173`) a variable de entorno.
- Volumen persistente para `App_Data/PolicyDocuments` (§1.7) — sin volumen, los documentos se pierden en cada redeploy.
- Vaciar `Jwt:Key`/connection string hardcodeados en `appsettings.json`; en prod se inyectan por variable de entorno.

**Próximo paso:** `WholeCareInsurance.api/Dockerfile`, `wholecare-admin-vs/Dockerfile`, los cambios de código de arriba, `docker-compose.yml` de referencia, `README.md` de despliegue en EasyPanel. Se decidió posponer la ejecución para una próxima sesión dedicada.

### 8.2 Ver §7 para la migración de datos (antes en esta sección, movida para agrupar con el resto de la migración).

---

## 9. Dashboard — ⏸ Bloqueado hasta tener la data migrada

No implementar hasta que la migración de datos del sistema anterior (§7) esté completa.

### 9.1 Paleta de colores general (aplica a toda la app, no solo Dashboard)
- Colores a resaltar: verde, blanco y azul.
- Botones semánticos: Submit/Guardar → verde, Eliminar → rojo, Editar → amarillo.

### 9.2 Referencia visual del Dashboard
Fila de tarjetas KPI: Agencias, Agentes, Pólizas (+ miembros), Recordatorios.
Fila de tarjetas por estado de póliza (cantidad + miembros por cada una): `Draft`, `Pendiente`, `Cancelado`, `Por procesar`, `En proceso`, `En corrección`, `Procesado`, `Cambio de agente` — actualizado según el enum final de §1.10 (ya no incluye "Actualizado", que no llegó a implementarse; en su lugar el enum real suma `Pendiente` y `En corrección`).
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

## 10. Orden sugerido de trabajo

1. ~~Tipo en Policy~~ ✅ Hecho
2. ~~Dependientes (vínculo con Customers existentes)~~ ✅ Hecho
3. ~~Botón de WhatsApp~~ ✅ Hecho
4. ~~Buscador/filtro de pólizas~~ ✅ Hecho
5. ~~Modal de detalle de póliza~~ ✅ Hecho (contenido base, ver §1.4)
6. ~~Refactorizaciones (API client, variable de entorno, refresh automático)~~ ✅ Hecho
7. ~~Mover DTOs de Customer/Policy a archivos separados~~ ✅ Hecho
8. ~~Compañía aseguradora en Policy~~ ✅ Hecho (confirmado cerrado, §1.5)
9. ~~Relación con el principal + Es aplicante~~ ✅ Hecho
10. ~~Documentos de póliza~~ ✅ Hecho
11. ~~Agentes (Agente/Asistente/Record) + datos demográficos en Customer~~ ✅ Hecho
12. ~~Selector de idioma ES/EN~~ ✅ Hecho
13. ~~Definir y cerrar el enum de Status de Policy~~ ✅ Hecho (§1.10)
14. ~~Campos nuevos de Customer + renombrado "Legal Status"~~ ✅ Hecho (§3.2, §3.3)
15. ~~Period + Number of applicants en Policy~~ ✅ Hecho (§1.8, §1.9)
16. ~~Crear Customer nuevo desde Members/Dependientes de la póliza~~ ✅ Hecho (§2)
17. Firma digital de consentimiento — bloqueado hasta que el responsable elija proveedor (§4.1)
18. Infraestructura de hosting (VPS) — plan definido, pendiente de ejecución (§8.1)
19. Migración de datos del sistema anterior — bloqueado hasta recibir el archivo + respuestas (§7)
20. Dashboard — bloqueado hasta tener la data migrada (§9)
