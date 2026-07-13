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

### 1.8 Period (año de vigencia/cobertura) — 🔲 Pendiente, no implementado
No existe ningún campo `Period` en `Models/Policy.cs` ni en los DTOs (`PolicyCreateDto`/`PolicyUpdateDto`/`PolicyResponseDto`). Falta: campo en el modelo (dropdown o numérico de año), migración, validación `[Required]`, y el `<select>`/input correspondiente en `Policies.jsx`.

### 1.9 Number of applicants — 🔲 Pendiente, no implementado
No existe en el modelo ni en los DTOs de Policy/Dependent. Debe ser numérico, carga manual del agente, ubicado en la sección de Members/Dependientes de la póliza (§1.2).

### 1.10 Enum de Status de Policy — ⚠️ Discrepancia importante, requiere decisión
`PolicyCreateDto.Status` **no tiene `[AllowedValues]`** (es un `string` libre, default `"Active"`). Los valores realmente usados hoy en el frontend (`Policies.jsx:8`) son:
```
Active, Expired, Cancelled, activa
```
Esto **no coincide en nada** con los 7 valores solicitados (`Draft, Cancelado, Por procesar, En proceso, Procesado, Actualizado, Cambio de agente`) — ni los nombres ni la cantidad. Además `"activa"` en minúscula sugiere datos sucios/duplicados ya cargados con el enum viejo.
- Impacto: este cambio también bloquea el widget de tarjetas por estado del Dashboard (§9.2), que asume que el enum de Status ya tiene esos 7 valores.
- Falta decidir: ¿se migran las pólizas existentes con status `Active`/`Expired`/`Cancelled`/`activa` a los 7 nuevos valores (mapeo manual, ¿cuál mapea a cuál?), o conviven ambos temporalmente? Este mapeo debe definirse con el responsable antes de tocar el modelo.

---

## 2. Extensión del flujo de Dependientes — crear Customer nuevo desde Members — 🔲 Pendiente prioritario

Hoy la sección Dependientes de Policies (§1.2) **solo permite buscar y vincular Customers que ya existen** (`dependentCandidates` en `Policies.jsx` filtra sobre la lista de `customers` ya cargada — no hay ningún flujo de alta). Falta:
- Opción de crear un Customer **nuevo** directamente desde la sección Members/Dependientes de la póliza (para personas que todavía no existen en el sistema), con paridad de campos con el formulario de Customer completo (incluyendo los campos nuevos de §3).
- Al crearlo así, debe quedar guardado como un Customer normal en la base y vinculado automáticamente vía `PolicyDependents`.

No implementar todavía — se documenta como pendiente prioritario según lo pedido. Como depende de que el formulario de Customer esté completo, conviene resolver primero §3.2.

---

## 3. Customers — campos nuevos

### 3.1 Ya implementado: Agente / Agente Asistente / Agente Record + datos demográficos — ✅ Hecho
- `Customer`: `ZipCode`, `State`, `City`, `County`, `MaritalStatus`, `Occupation` (todos opcionales, sin `[AllowedValues]` a propósito — ver comentario en `CustomerCreateDto.cs`).
- `County`: dataset de los 3143 condados del US Census Bureau, bundleado en `src/data/usCounties.json`, filtrado por Estado (`<select>` de Condado se resetea si cambia el Estado).
- `AgentId`/`AssistantAgentId`/`RecordAgentId` en `Customer` (FKs a `User`, nullable, `OnDelete Restrict`). No-Admin se auto-asigna como `AgentId` al crear (forzado server-side); Admin puede setear los tres, validados contra `Rol == "Agente"` (`RecordAgentId` además contra `IsEncargado == true`).
- Página `/agentes` (solo Admin) para alta/edición de agentes.

### 3.2 Campos pendientes de agregar — 🔲 Pendiente, ninguno implementado
Auditado `Models/Customer.cs` completo — el modelo hoy solo tiene: `SocialSecurityNumber`, `FirstName`, `LastName`, `DateOfBirth`, `Email`, `Address` (campo único, no separado en #1/#2), `Phone`, `MigrationStatus`, `RelacionConPrincipal`, más lo listado en §3.1. **Ninguno de los siguientes campos existe en el modelo, DTOs, migraciones ni formulario**:
- Middle name (texto, opcional)
- Gender (dropdown: Masculino, Femenino, Otro)
- Green card (texto, número de tarjeta, opcional)
- Work permit (texto, número de permiso, opcional)
- Address # 1 (texto, obligatorio) / Address # 2 (texto, opcional) — hoy es un solo campo `Address`, habría que decidir si se migra el dato existente a Address#1 o se agregan campos nuevos en paralelo
- Employer name (texto, opcional)
- Company Phone (texto, opcional)
- Annual income (numérico/moneda, obligatorio)
- Tags (texto libre — PENDIENTE de definir uso exacto con el responsable, no bloqueante para implementar como campo simple)
- Language (dropdown English/Spanish) — idioma de preferencia de **contacto** del cliente. **No confundir con `User.PreferredLanguage`** (§6.2), que es el idioma de la interfaz del usuario logueado — son conceptos distintos, ninguno de los dos cubre al otro.

### 3.3 Renombrado "Legal Status" (label, sin cambio de modelo) — 🔲 Pendiente, no implementado
El campo `MigrationStatus` sigue mostrándose en el formulario como **"Migration Status"** (`src/i18n/locales/en/customers.json:16`, `src/i18n/locales/es/customers.json:16` dice "Estatus migratorio", que ya está bien en español). Falta solo cambiar la clave de traducción en inglés a "Legal Status" — el campo, los valores (`Permiso de trabajo`, `Residente permanente`, `Ciudadano`, `Otro`) y el modelo no cambian. Es un cambio de una línea cuando se priorice.

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

> Nota: buena parte de estas columnas mapean directo a los campos nuevos de Customer/Policy pendientes en §3.2, §1.8 y §1.9 — conviene cerrar esos campos antes de diseñar el script de migración, para no mapear dos veces.

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
Fila de tarjetas por estado de póliza (cantidad + miembros por cada una): Draft, Cancelado, Por procesar, En proceso, Procesado, Actualizado, Cambio de agente.
⚠️ **Depende de §1.10** — el enum de Status hoy no tiene estos 7 valores, hay que resolver eso primero.
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
13. **Definir y cerrar el enum de Status de Policy (§1.10)** — bloquea el Dashboard, conviene resolverlo antes de seguir sumando campos nuevos
14. Campos nuevos de Customer (§3.2) + renombrado "Legal Status" (§3.3)
15. Period + Number of applicants en Policy (§1.8, §1.9)
16. Crear Customer nuevo desde Members/Dependientes de la póliza (§2)
17. Firma digital de consentimiento — bloqueado hasta que el responsable elija proveedor (§4.1)
18. Infraestructura de hosting (VPS) — plan definido, pendiente de ejecución (§8.1)
19. Migración de datos del sistema anterior — bloqueado hasta recibir el archivo + respuestas (§7)
20. Dashboard — bloqueado hasta tener la data migrada, y hasta resolver §1.10 (§9)
