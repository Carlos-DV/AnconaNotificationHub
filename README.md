# AnconaNotificationHub

Servicio central de **tiempo real** del backoffice (ASP.NET Core **.NET 10**). Cualquier servicio —sobre
todo los Windows Services, que no tienen URL— publica un `NotificationEvent` a RabbitMQ; esta API lo
consume y lo emite por SignalR a los grupos indicados. Agregar un servicio publicador nuevo **no requiere
tocar esta API**.

```
AnconaWarrantyReturns ─┐
Otro servicio          ├─► Exchange anc.notifications.<env> (topic, routing key = EventType)
                       │        └─► Cola AnconaNotificationHub-<ENV> (binding "#", DLQ)
                       │               └─► AnconaNotificationHub (consumer → SignalR)
                       │                      └─► /hubs/notifications ─► IIS ─► Ocelot ─► bweb-next-fe
```

Principios:

1. **El canal en tiempo real es un aviso, no la fuente de verdad.** Se puede perder (pestaña cerrada,
   red, deploy). Al reconectar o al abrir una pantalla, el frontend consulta la API del dominio.
2. **Publisher experto, consumer cartero.** El publisher decide `EventType`, `Audience` y `Payload`. Esta
   API no conoce ningún dominio: valida, traduce audiencia a grupos y emite.
3. **Payload pequeño** (máx. 32 KB): id, folio, status. Si la pantalla necesita más, consulta su API.

---

## Arquitectura

```
AnconaNotificationHub.Api (host)  →  Infrastructure  →  Application  →  Domain
                                                         └──────────→  Contracts
```

| Proyecto | Contenido |
|---|---|
| `Contracts` | `NotificationEvent`, `Audience`, `AudienceType`. `netstandard2.0;net10.0`; es lo único que referencian los publishers |
| `Domain` | `GroupName`: `"{tenant}:{tipo}:{valor}"` o `"{tenant}:all"`, en minúsculas |
| `Application` | Validación del sobre y despacho (`Features/DispatchEvent`), grupos al conectar y reglas de `Subscribe` (`Features/Connections`), puertos y `NotificationSetting` |
| `Infrastructure` | Consumer de RabbitMQ (LilHermes), `NotificationHub` + `SignalRNotifier`, resolver de sucursales y permisos (Dapper), health checks |
| `AnconaNotificationHub.Api` | `Program.cs`, JWT, CORS, `MapHub`, `/health`, `appsettings.json` |
| `tests/Application.UnitTests` | xUnit, dobles escritos a mano |

## Cómo publicar un evento (para otros servicios)

1. Referenciar `AnconaNotificationHub.Contracts`.
2. Armar el sobre **siempre** con `NotificationEvent.Create(...)` (genera `EventId`, `OccurredAt` en UTC y
   serializa el payload en camelCase):

   ```csharp
   var evt = NotificationEvent.Create(
       eventType: "warranty.return.status-changed",
       tenant: "ancona",
       source: "AnconaWarrantyReturns",
       audience: [ Audience.Topic("warranty.returns"),
                   Audience.Entity("warranty.return", ret.WarrantyReturnKey),
                   Audience.Branch(ret.BranchCode) ],
       payload: new { ret.WarrantyReturnKey, ret.Folio, ret.BranchCode, Status = ret.Status.ToString() });
   ```

3. Publicarlo como `MessageContext<NotificationEvent>.Data` (LilHermes) al exchange
   `anc.notifications.<env>` con **routing key = `EventType`**.

Reglas:

- `EventType` = `{dominio}.{entidad}.{acción}` en minúsculas, caracteres `[a-z0-9._-]`.
- Payload pequeño (máx. 32 KB), objeto JSON. Lo demás lo consulta el frontend.
- Publicar **después** de confirmar la operación de negocio, en un `try/catch` que solo registra: una
  notificación fallida nunca revierte ni bloquea la operación.
- Proteger la publicación con el circuit breaker de Polly como en
  `AnconaDocEventDispatcher/src/Infrastructure/Messaging/MessageBus.cs`.
- **Versionado:** un cambio incompatible en el payload se publica como un tipo nuevo
  (`warranty.return.status-changed.v2`). Nunca se cambia el significado de un `EventType` existente.

## Audiencias

| `Type` | `Value` | Grupo resultante | Quién une al usuario |
|---|---|---|---|
| `user` | `uid` del JWT (`38`) | `ancona:user:38` | Servidor, al conectar |
| `all` | vacío | `ancona:all` | Servidor, al conectar |
| `branch` | código de sucursal de 3 dígitos (`001`) | `ancona:branch:001` | Servidor, al conectar |
| `perm` | valor del permiso (`ClaimValue`) | `ancona:perm:<permiso>` | Servidor, al conectar |
| `topic` | nombre de pantalla/colección (`warranty.returns`) | `ancona:topic:warranty.returns` | Cliente, con `Subscribe` |
| `entity` | `{tipo}:{id}` (`warranty.return:<key>`) | `ancona:entity:warranty.return:<key>` | Cliente, con `Subscribe` |

- Valores normalizados a minúsculas; caracteres permitidos `[a-z0-9._:-]`.
- **Sucursal = código de 3 dígitos** (`dbo.BranchOffice.U_SO1_01SUCURSAL`), no `BranchOffice.Code`
  (que es el almacén).
- El tenant va siempre al inicio del grupo y sale del sobre o del token, nunca del cliente.

## Cliente

- Hub: `/hubs/notifications`, requiere JWT. El navegador no manda headers en WebSocket, así que el token
  va en `?access_token=` (con `@microsoft/signalr`, vía `accessTokenFactory`).
- Al conectar, el servidor une la conexión a `user`, `all`, sus sucursales y sus permisos.
- Métodos invocables:

  | Método | Regla |
  |---|---|
  | `Subscribe(type, value)` | Solo `topic` y `entity`. Máximo `Notification:MaxSubscriptionsPerConnection` (50) por conexión. Violación → `HubException` |
  | `Unsubscribe(type, value)` | Idempotente |

- Un solo método del servidor al cliente:
  `ReceiveNotification({ eventId, eventType, occurredAt, payload })`. Agregar eventos no agrega métodos.
- **SignalR no deduplica**: si una conexión está en dos grupos destino recibe el evento dos veces. El
  cliente descarta por `eventId`.
- **Los grupos dinámicos se pierden al reconectar** (nuevo `connectionId`): el cliente debe repetir sus
  `Subscribe` en `onreconnected`.

## Configuración y secretos

| Sección | Contenido |
|---|---|
| `ConnectionStrings:Root` | BD raíz (`Company`, `UserRoles`, `RoleClaims`, `UserClaims`). **Secreto** |
| `JWTSettings` | `Key` (**secreto**), `Issuer`, `Audience`: los mismos que el resto del backoffice |
| `Cors:AllowedOrigins` | Orígenes del frontend (explícitos; SignalR con credenciales no admite `*`) |
| `Notification` | `MaxEventAgeSeconds` (300), `MaxPayloadBytes` (32768), `MaxSubscriptionsPerConnection` (50), `UserGroupsCacheMinutes` (5) |
| `RabbitMQ` | Conexión (`Password` es **secreto**), `QueueName`, `QueueExchangeName`, `QueueRoutingKeys` (`#`), `PrefetchCount`, `EnableDLQ` |
| `Serilog` | Console, File (CLEF) y Seq |

`appsettings.json` no lleva credenciales. En DEV van en User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:Root" "<cadena BD raíz>" --project src/AnconaNotificationHub.Api
dotnet user-secrets set "RabbitMQ:Password" "<password>" --project src/AnconaNotificationHub.Api
dotnet user-secrets set "JWTSettings:Key" "<llave del backoffice>" --project src/AnconaNotificationHub.Api
```

En servidor: `appsettings.Production.json` (ignorado por git) o variables de entorno
(`ConnectionStrings__Root`, `RabbitMQ__Password`, `JWTSettings__Key`). Si falta alguno, la API no arranca
y lo dice en el log.

## Comandos

```powershell
dotnet build AnconaNotificationHub.slnx
dotnet test tests/Application.UnitTests
dotnet run --project src/AnconaNotificationHub.Api --launch-profile https
```

`GET https://localhost:7190/health` → `Healthy` cuando el consumer está suscrito y la BD raíz responde.

## Prueba manual

RabbitMQ → Exchanges → `anc.notifications.dev` → Publish message, routing key
`warranty.return.status-changed`. `OccurredAt` debe ser la hora UTC actual
(`(Get-Date).ToUniversalTime().ToString("o")`) o el evento se descarta por antigüedad; `Value` de `user`
= uid del token con el que se conectó el cliente:

```json
{
  "CorrelationId": "11111111-1111-1111-1111-111111111111",
  "MessageId": "22222222-2222-2222-2222-222222222222",
  "Timestamp": "2026-09-30T12:00:00Z",
  "SourceService": "manual",
  "Data": {
    "EventId": "33333333-3333-3333-3333-333333333333",
    "EventType": "warranty.return.status-changed",
    "Tenant": "ancona",
    "Source": "manual",
    "OccurredAt": "2026-09-30T12:00:00Z",
    "Audience": [
      { "Type": "user", "Value": "38" },
      { "Type": "topic", "Value": "warranty.returns" }
    ],
    "Payload": { "warrantyReturnKey": "demo", "folio": "001001DE999", "branchCode": "001", "status": "Pending" }
  }
}
```

## Errores

| Caso | Resultado |
|---|---|
| Sobre inválido | Excepción → 3 reintentos de LilHermes → DLQ `AnconaNotificationHub-<ENV>.error` |
| Evento más viejo que `MaxEventAgeSeconds` | ack, descarte (log Debug) |
| Grupo sin conexiones | ack (no es error) |
| Falla al resolver sucursales/permisos al conectar | La conexión sigue solo con `user` y `all` (Warning) |

## Despliegue (IIS + Ocelot)

**IIS (servidor de la API y del gateway):**

- Característica de Windows **WebSocket Protocol** instalada.
- App pool: `Start Mode = AlwaysRunning`, `Idle Time-out = 0`.
- Sitio: `Preload Enabled = True` (requiere **Application Initialization**).
  Sin esto el consumer se detiene cuando IIS apaga el pool por inactividad (20 min sin requests).

**Ocelot:**

- `app.UseWebSockets()` antes de `app.UseOcelot()`.
- Ruta `/hubs/notifications/negotiate` → downstream `https`.
- Ruta `/hubs/notifications` → downstream `wss`.
- Ocelot no autentica rutas WebSocket; el JWT lo valida esta API.

**Escalar a 2+ instancias:** agregar `AddStackExchangeRedis` como backplane de SignalR. Nada más cambia.

## Troubleshooting

| Síntoma | Causa |
|---|---|
| `IDX10720` al validar el token | La llave HS256 tiene menos de 256 bits: .NET 10 la rechaza. Revisar la llave, no bajar la validación |
| El transporte queda en long polling | Falta **WebSocket Protocol** en IIS o `UseWebSockets()` en Ocelot |
| El consumer no recibe nada | El exchange `anc.notifications.<env>` no existe o `QueueRoutingKeys` está vacío. `/health` lo reporta |
| Los eventos "desaparecen" | Son más viejos que `MaxEventAgeSeconds`: revisar el reloj del publisher y que `OccurredAt` vaya en UTC |
| Deja de llegar todo tras un rato sin uso | El app pool de IIS se apagó: falta `AlwaysRunning` + `Preload` |
