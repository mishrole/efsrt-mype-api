# MYPE API

API REST para la gestión financiera de una MYPE. Proporciona autenticación, negocios, categorías, productos, movimientos financieros, evidencias privadas, historial y dashboard.

## Tecnologías principales

- .NET 10 y ASP.NET Core Minimal APIs.
- Entity Framework Core 10 con Npgsql.
- PostgreSQL.
- JWT y refresh tokens.
- Azure Blob Storage para evidencias privadas.
- Serilog para registros.
- Scalar para documentación interactiva de la API.
- xUnit, Moq y Testcontainers para pruebas.

## Arquitectura

```text
src/
├── Mype.Api/
├── Mype.Application/
├── Mype.Domain/
├── Mype.Infrastructure/
└── Mype.Shared/

tests/
└── Mype.Tests/
```

- `Mype.Api`: endpoints, autenticación, middleware y composición de la aplicación.
- `Mype.Application`: casos de uso, validaciones, contratos y resultados.
- `Mype.Domain`: entidades, reglas, estados y comportamiento del dominio.
- `Mype.Infrastructure`: Entity Framework Core, repositorios, autenticación y almacenamiento.
- `Mype.Shared`: constantes y modelos compartidos.
- `Mype.Tests`: pruebas unitarias, de endpoints y de integración con PostgreSQL.

## Requisitos

Instale antes de comenzar:

- .NET SDK 10.
- PostgreSQL accesible mediante una cadena de conexión Npgsql.
- Docker en ejecución para las pruebas de integración con Testcontainers.
- Azure CLI autenticada para usar Azure Blob Storage durante el desarrollo local.

## Configuración local

La API carga un archivo `.env` desde el directorio de contenido cuando se ejecuta en `Development`. Las variables de entorno del sistema también se agregan a la configuración.

Cree `src/Mype.Api/.env` con valores equivalentes a los siguientes:

```dotenv
DB_CONNECTION=Host=localhost;Port=5432;Database=mype;Username=postgres;Password=<PASSWORD>
SERVICE_NAME=Mype.Api
TIMEZONE=America/Lima
ORIGINS_CONFIGURATION=http://localhost:5173,https://localhost:5173
CORS_POLICY_NAME=MypeCors
JWT_SECRET_KEY=<SECRETO_ALEATORIO_DE_AL_MENOS_32_CARACTERES>
JWT_ISSUER=Mype.Api
JWT_AUDIENCE=Mype.Web
JWT_EXPIRATION_MINUTES=60
REFRESH_TOKEN_PEPPER=<SECRETO_ALEATORIO_INDEPENDIENTE>
RETRY_COUNT=3
SLEEP_DURATION_PROVIDER=2
EVIDENCE_STORAGE_PROVIDER=AzureBlob
EVIDENCE_STORAGE_SERVICE_URI=https://<CUENTA>.blob.core.windows.net
EVIDENCE_STORAGE_CONTAINER=evidences
EVIDENCE_MAX_SIZE_BYTES=5242880
EVIDENCE_READ_URI_MINUTES=5
EVIDENCE_PURGE_RETENTION_DAYS=30
```

No confirme `.env` ni secretos reales en el repositorio.

## Variables de entorno

| Variable | Descripción |
|---|---|
| `DB_CONNECTION` | Cadena de conexión Npgsql a PostgreSQL. |
| `SERVICE_NAME` | Nombre lógico del servicio. |
| `TIMEZONE` | Zona horaria usada por la aplicación. |
| `ORIGINS_CONFIGURATION` | Orígenes CORS separados por comas. En desarrollo puede dejarse vacío para aceptar orígenes loopback. |
| `CORS_POLICY_NAME` | Nombre de la política CORS registrada y utilizada por la aplicación. |
| `JWT_SECRET_KEY` | Clave de firma del JWT. Use un secreto robusto de al menos 32 caracteres. |
| `JWT_ISSUER` | Emisor esperado del JWT. |
| `JWT_AUDIENCE` | Audiencia esperada del JWT. |
| `JWT_EXPIRATION_MINUTES` | Vigencia del token de acceso en minutos. |
| `REFRESH_TOKEN_PEPPER` | Secreto usado al proteger los refresh tokens. |
| `RETRY_COUNT` | Cantidad de reintentos de conexión o migración. |
| `SLEEP_DURATION_PROVIDER` | Base temporal usada entre reintentos. |
| `EVIDENCE_STORAGE_PROVIDER` | Proveedor de almacenamiento. El valor implementado es `AzureBlob`. |
| `EVIDENCE_STORAGE_SERVICE_URI` | URI del servicio de Azure Blob Storage. |
| `EVIDENCE_STORAGE_CONTAINER` | Nombre del contenedor privado de evidencias. |
| `EVIDENCE_MAX_SIZE_BYTES` | Tamaño máximo permitido por archivo. |
| `EVIDENCE_READ_URI_MINUTES` | Vigencia del acceso temporal de lectura. |
| `EVIDENCE_PURGE_RETENTION_DAYS` | Días de retención antes de la purga de evidencias retiradas. |

## Preparar PostgreSQL

Cree una base de datos vacía y un usuario con permisos suficientes. Ejemplo:

```sql
CREATE DATABASE mype;
```

Configure `DB_CONNECTION` con las credenciales reales.

Las migraciones de Entity Framework Core se aplican al iniciar la API. No ejecute una migración manual adicional para una base de datos nueva salvo que el inicio informe un error específico.

## Preparar Azure Blob Storage

1. Cree o use una cuenta de almacenamiento.
2. Cree un contenedor privado, por ejemplo `evidences`.
3. Configure `EVIDENCE_STORAGE_SERVICE_URI` y `EVIDENCE_STORAGE_CONTAINER`.
4. Autentique Azure CLI:

```bash
az login
```

5. Verifique que la identidad autenticada tenga permisos sobre blobs y contenedores requeridos por la aplicación.

En desarrollo, la infraestructura utiliza las credenciales de Azure CLI. En un despliegue en Azure debe usarse la identidad administrada o la identidad configurada para el servicio.

## Restaurar y compilar

Desde la raíz del repositorio:

```bash
dotnet restore
dotnet build --no-restore
```

## Ejecutar localmente

Inicie la API en modo Development:

```bash
dotnet run --project src/Mype.Api/Mype.Api.csproj
```

La dirección final se muestra en la terminal. El proyecto expone:

- `GET /health` para comprobar el estado básico.
- `/scalar/v1` en desarrollo para consultar la documentación interactiva.
- Rutas funcionales bajo `/api/v1`.

Para usar una URL local fija:

```bash
ASPNETCORE_URLS=https://localhost:7020 dotnet run --project src/Mype.Api/Mype.Api.csproj
```

En PowerShell:

```powershell
$env:ASPNETCORE_URLS = "https://localhost:7020"
dotnet run --project src/Mype.Api/Mype.Api.csproj
```

## Certificado HTTPS local

Confíe en el certificado de desarrollo de .NET:

```bash
dotnet dev-certs https --trust
```

Después configure el frontend con la misma URL HTTPS.

## Pruebas

Docker debe estar disponible porque las pruebas de integración levantan PostgreSQL mediante Testcontainers.

Ejecute toda la suite:

```bash
dotnet test tests/Mype.Tests/Mype.Tests.csproj
```

Ejecute compilación y pruebas en configuración Release:

```bash
dotnet build --configuration Release
dotnet test tests/Mype.Tests/Mype.Tests.csproj --configuration Release --no-build
```

Cobertura en formato OpenCover:

```bash
dotnet test tests/Mype.Tests/Mype.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

El workflow del proyecto exige al menos 80 % de cobertura global y 80 % sobre código nuevo analizado por Sonar.

## Flujo de inicio

Al iniciar, la API:

1. Carga `.env` en desarrollo.
2. Agrega las variables del entorno.
3. Registra infraestructura, aplicación y CORS.
4. Configura autenticación JWT.
5. Aplica las migraciones pendientes de PostgreSQL.
6. Expone health check, documentación y endpoints.

## Integración con el frontend

Configure en el frontend:

```dotenv
VITE_API_BASE_URL=https://localhost:7020
```

Configure en la API un origen compatible:

```dotenv
ORIGINS_CONFIGURATION=http://localhost:5173,https://localhost:5173
```

La aplicación web consume rutas `/api/v1` y envía credenciales de sesión según los contratos implementados.

## Solución de problemas

### `DB_CONNECTION` no está configurada

Revise que `src/Mype.Api/.env` exista y que la variable contenga una cadena Npgsql válida.

### PostgreSQL rechaza la conexión

Verifique host, puerto, base de datos, credenciales, SSL y reglas de red. Confirme también que el usuario pueda crear y modificar el esquema requerido por las migraciones.

### Azure Blob Storage devuelve 401 o 403

Ejecute:

```bash
az login
```

Compruebe la cuenta activa y los permisos sobre la cuenta y el contenedor configurados.

### El frontend recibe un error de CORS

Agregue el origen exacto del frontend a `ORIGINS_CONFIGURATION`. Los orígenes múltiples se separan con comas.

### Testcontainers no inicia PostgreSQL

Inicie Docker y compruebe que el usuario actual tenga acceso al daemon. Después repita `dotnet test`.

### Error de JWT al iniciar

Compruebe `JWT_SECRET_KEY`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRATION_MINUTES` y `REFRESH_TOKEN_PEPPER`.

## Despliegue

La API está preparada para Azure App Service. Configure allí todas las variables de entorno y asigne a la identidad del servicio acceso a Azure Blob Storage. La cadena de PostgreSQL debe almacenarse como secreto de la plataforma, no en el repositorio.
