# AnconaNotificationHub — Contexto del proyecto

## Estado (2026-10-02)

- **Diseño aprobado** por secciones: `docs/superpowers/specs/2026-09-30-notification-hub-design.md`.
- **Plan 1 terminado** (API + Contracts): `docs/superpowers/plans/2026-09-30-notification-hub-api.md`.
  Tasks 1–15 en `feat/notification-hub-api` (mergeada a `development`, PR #1). Consultas del resolver
  verificadas en SSMS (DEV).
- **Task 16 verificada en DEV el 2026-10-02:** exchange, cola, `.retry`/`.error` y binding `#` existen;
  `/health` Healthy; WebSocket; grupos resueltos; evento a `user` + `topic` llega 2 veces (dedupe por
  `eventId`); `Subscribe user 99` rechazado; evento de 10 min descartado; `Audience` vacío → 3 reintentos
  de 30 s → `AnconaNotificationHub-DEV.error`. Herramientas locales en `docs/tools/` (`README-demo.md`,
  `smoke-client.html`, `publish-test-event.ps1`) y secretos en `docs/README-secretos.md`.
- **Permisos con espacios, acentos o signos** (`Permission.Auditorías.View`,
  `Permission.Transito, Recibo e Ingresos.View`) ya forman grupo (rama `fix/group-name-normalization`,
  65 tests; verificado en DEV con el uid 38 sin grupos omitidos). Ver "Sobre y grupos".
- **Pendiente:** los planes 2 y 3 se escriben **en sus propios proyectos**: plan 2 en `bweb-next-fe`,
  plan 3 en `AnconaWarrantyReturns` (spec §6 y §7 como base).
- **Pendiente:** LilHermes 1.0.0 trae `OpenTelemetry.Api` 1.4.0 con vulnerabilidad moderada
  (NU1902, GHSA-g94r-2vxg-569j). Corregir en LilHermes o fijar una versión más nueva en Infrastructure.
- `docs/` es **local, no se versiona** (va en `.gitignore`, igual que en AnconaWarrantyReturns).

## Qué es

Servicio central de tiempo real del backoffice. Cualquier servicio (sobre todo Windows Services sin URL)
publica un `NotificationEvent` a RabbitMQ; esta API (ASP.NET Core **.NET 10**, detrás de IIS + Ocelot)
lo consume y lo emite por SignalR al frontend. Agregar un publisher nuevo **no requiere tocar esta API**.

```
Servicio ─► Exchange anc.notifications.<env> (topic, routing key = EventType)
             └─► Cola AnconaNotificationHub-<ENV> (binding "#", DLQ) ─► API ─► /hubs/notifications ─► bweb-next-fe
```

## Principios (decididos con el usuario)

1. **El canal en tiempo real es un aviso, no la fuente de verdad.** Se puede perder; al reconectar o abrir
   una pantalla el frontend consulta la API del dominio. Sin notificaciones persistentes por ahora (campanita
   fuera de alcance; el sobre ya trae `EventId`/`Audience` para agregarla después como otro consumidor).
2. **Publisher experto, consumer cartero.** El publisher decide `EventType`, `Audience` y `Payload`; la API
   valida, traduce audiencia → grupos y emite. No conoce dominios.
3. **Payload pequeño** (máx. 32 KB): id, folio, status. Lo demás lo consulta el frontend.
4. **Mantenible por alguien nuevo:** una regla ("publica un `NotificationEvent` al exchange"), un solo
   método cliente (`ReceiveNotification`), contratos en un paquete (`AnconaNotificationHub.Contracts`).

## Stack y arquitectura

- .NET 10, Clean Architecture + Features, convenciones del template `ancona-worker`.
- LilHermes 1.0.0 (nuget.org, autor Carlos-DV), Dapper + Microsoft.Data.SqlClient, JWT HS256 (`JWTSettings`),
  Serilog → Console/File/Seq, xUnit con dobles escritos a mano.
- `Contracts` → `netstandard2.0;net10.0` (para que publiquen también servicios viejos).

```
src/Contracts        NotificationEvent, Audience, AudienceType (lo que referencian los publishers)
src/Domain           GroupName "{tenant}:{tipo}:{valor}" / "{tenant}:all"
src/Application      Features/DispatchEvent, Features/Connections, puertos, NotificationSetting
src/Infrastructure   Messaging (LilHermes), Realtime (NotificationHub, SignalRNotifier), Persistence, Health
src/AnconaNotificationHub.Api   Program.cs, JWT, CORS, MapHub, /health
```

## Sobre y grupos

`NotificationEvent { EventId, EventType, Tenant, Source, OccurredAt, Audience[], Payload(JsonElement) }`,
creado siempre con `NotificationEvent.Create(...)` (payload en camelCase). Viaja como `MessageContext.Data`.

| Audience | Grupo | Quién une |
|---|---|---|
| `user` (uid del JWT) | `ancona:user:38` | Servidor al conectar |
| `all` | `ancona:all` | Servidor al conectar |
| `branch` (código 3 dígitos `U_SO1_01SUCURSAL`, **no** `BranchOffice.Code` = almacén) | `ancona:branch:001` | Servidor al conectar |
| `perm` (ClaimValue) | `ancona:perm:<permiso>` | Servidor al conectar |
| `topic` | `ancona:topic:warranty.returns` | Cliente con `Subscribe` |
| `entity` | `ancona:entity:warranty.return:<key>` | Cliente con `Subscribe` |

- El **tenant va siempre en el grupo** y sale del token/sobre, nunca del cliente.
- Grupos por **permiso**, no por rol (los roles del token son de sistema: `SuperAdmin`, `Admin_LMS`).
- `Subscribe` solo acepta `topic`/`entity`, máx. 50 por conexión, pasa por `ISubscriptionPolicy`
  (hoy permite todo dentro del tenant; punto de extensión para exigir permiso por topic).

### Memoria y escala (analizado el 2026-10-02)

- Datos de `BO_ADMON`: 953 usuarios, 56 permisos en promedio, máx. 400, 67 con más de 200, 409 distintos.
- Cada grupo de una conexión cuesta ~150–200 B (entrada en el grupo + `HashSet` de la conexión + su
  string). Estimado: ~10 MB con 1000 conexiones, ~20 MB con 2000. Publicar solo busca los grupos del
  evento; en .NET 10 desconectar solo recorre los grupos de esa conexión.
- **Redis no reduce memoria:** cada servidor guarda los grupos de sus conexiones y además se suscribe a un
  canal por grupo. Sirve solo para tener 2+ instancias.
- Se descartó por ahora resolver `perm` al publicar (consultar usuarios con el permiso y mandar a sus
  grupos `user`): ahorra ~10 MB a cambio de SQL en el despacho. Reconsiderar si hay varias instancias,
  miles de permisos por usuario, o si los cambios de permisos deben aplicar sin reconectar.

## Dónde vive cada dato

| Dato | BD |
|---|---|
| `Company` (cadena del tenant, buscar por `Identifier`) | Raíz (`ConnectionStrings:Root` = **`BO_ADMON`**; `BO_ANCONA` es el tenant) |
| `User`, `UserRoles`, `RoleClaims`, `UserClaims` (`ClaimType = 'permission'`) | Raíz (como `PermissionService` de system-api) |
| `UserBranchOffice` → `BranchOffice.U_SO1_01SUCURSAL` | Tenant (como sale-api) |

Claims del JWT usados: `tenant`, `uid`. Sucursal y área **no** vienen en el token (el usuario prefirió
resolverlas en backend). Resultado cacheado por `(tenant, uid)` 5 min; la cadena del tenant, 1 h.
Si el resolver falla, la conexión sigue solo con `user` y `all` (Warning). Implementación en
`Infrastructure/Persistence/UserGroupResolver.cs` y `TenantConnectionProvider.cs`.

## Gotchas conocidas

- **IIS apaga el app pool** tras 20 min sin requests → el consumer (BackgroundService) deja de leer.
  App pool `AlwaysRunning`, `Idle Time-out = 0`, sitio `Preload Enabled` (Application Initialization).
- **Eventos viejos se descartan** (`MaxEventAgeSeconds`, default 300) para no mandar avalanchas tras una caída.
- **SignalR no deduplica** entre grupos: el frontend descarta por `eventId`.
- **Los grupos dinámicos se pierden al reconectar** (nuevo connectionId): el cliente debe resuscribir.
  Hoy `csa/src/store/hubStore.ts` no lo hace con `Chat.{pkTicket}`.
- **WebSocket por IIS + Ocelot:** característica "WebSocket Protocol" en IIS; en Ocelot `UseWebSockets()`
  antes de `UseOcelot()`, ruta `negotiate` (https) + ruta del hub (wss). Si no, cae a long polling.
- **Una sola instancia**; con 2+ agregar backplane Redis (`AddStackExchangeRedis`).
- **Al arrancar, el consumer procesa la cola antes de que haya clientes** (incluso antes de que Kestrel
  escuche): lo publicado con la API caída se emite y se pierde. Es el principio 1, no un bug.
- **LilHermes** reintenta 3 veces (30 s en `.retry`) y manda a `.error` con routing key `parked` **sin
  escribir en el log** al estacionarlo (solo hay un Error por intento). No valida config del consumer
  (`ValidateRabbitSettings` truena al arrancar si falta cola/exchange/routing keys). Se usa
  `AddLilHermesConsumer` (existe en 1.0.0): esta API solo consume, no publica.
- **El consumer vive en la API** (`NotificationEventConsumer`, BackgroundService): si truena, el hub sigue
  sirviendo pero `/health` reporta Unhealthy (checks `consumer` y `root-database`).
- **JWT en .NET 10:** llave HS256 < 256 bits → `IDX10720`.
- Secretos (`ConnectionStrings:Root`, `RabbitMQ:Password`, `JWTSettings:Key`) nunca en `appsettings.json`.
  La cadena de `appsettings.Development.json` de system-api apunta a `BO_ANCONA`: copiarla tal cual como
  `Root` da `Invalid object name 'UserRoles'` al conectar.
- **CORS en DEV** solo permite `http://localhost:3002`: el smoke client se sirve ahí; `file://`,
  `127.0.0.1` u otro puerto dan `NetworkError` en `negotiate`.
- `ClaimValue` viene con mayúsculas, espacios, acentos y signos. Solo para `perm`, `GroupName` lo pasa a
  minúsculas, quita acentos y cambia cada tramo fuera de `[a-z0-9._:]` por un `-`
  (`Permission.Reporte Max/Min.View` → `ancona:perm:permission.reporte-max-min.view`). El publisher manda
  el `ClaimValue` tal cual. `topic`/`entity` siguen estrictos (`[a-z0-9._:-]`): un nombre mal escrito truena.

## Publishers

- **Piloto: AnconaWarrantyReturns** (plan 3): `warranty.return.status-changed` en cada cambio de estado de la
  DE (`Pending` → `TransferRequested` → `TransferOutRequested` → `TransferOutRegistered` → `TransferInRegistered`),
  audiencia `topic:warranty.returns` + `entity:warranty.return:{key}` + `branch:{sucursal}`. Publicar tras
  confirmar la transacción, en `try/catch` que solo registra. Hoy no publica nada (`PublishExchangeName: "#"`);
  su `MessageBus` necesita `PublishAsync(event, routingKey)`.
- Pendiente decidir: distribución de `Contracts` (NuGet interno vs. referencia de proyecto).

## Frontend

- **Primero en `bweb-next-fe`** (plan 2): Next 14, React 18, `@microsoft/signalr` 8, Zustand 5, `next-auth`
  (`getSession()` → `session.token.accessToken`), URL `${NEXT_APP_API_URL}hubs/notifications`.
- Módulo: `store/notifications/notification-hub-store.ts`, `hooks/useNotificationEvent.ts`,
  `hooks/useNotificationSubscription.ts`, `utils/notification-events.ts`. Una conexión por sesión,
  `accessTokenFactory` pide la sesión en cada reconexión, dedupe por `eventId`, resuscripción automática.
- Pantallas existentes con conexiones propias (`OrderManager`, `HeaderContent`) no se tocan.

## Comandos

```powershell
dotnet build AnconaNotificationHub.slnx
dotnet test tests/Application.UnitTests
dotnet run --project src/AnconaNotificationHub.Api --launch-profile https   # https://localhost:7190
```

Secretos en DEV con `dotnet user-secrets` (ver README, "Configuración y secretos").

## Convenciones

- Commits en inglés, Conventional Commits + gitmoji al final; el usuario aprueba cada commit y hace el push.
- `internal sealed` en Infrastructure, primary constructors, nada de magic strings (todo desde config).
- `Application` no depende de ASP.NET ni de hosting.
