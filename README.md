# GuiasBackend

API backend para gestión de guías y usuarios.

## Configuración del proyecto

Para ejecutar este proyecto, necesitas configurar las siguientes variables secretas:

### Configuración de desarrollo (User Secrets)

```bash
# Inicializar user secrets (solo una vez)
dotnet user-secrets init --project GuiasBackend

# Configurar variables necesarias
dotnet user-secrets set "DB_PASSWORD" "tu-contraseña-bd" --project GuiasBackend
dotnet user-secrets set "JWT_SECRET_KEY" "tu-clave-jwt-muy-segura" --project GuiasBackend
dotnet user-secrets set "CERT_PASSWORD" "tu-contraseña-certificado" --project GuiasBackend

# Variables opcionales para correo electrónico
dotnet user-secrets set "EMAIL_USERNAME" "tu-correo@empresa.com" --project GuiasBackend
dotnet user-secrets set "EMAIL_PASSWORD" "tu-contraseña-correo" --project GuiasBackend
dotnet user-secrets set "EMAIL_SENDER" "tu-correo@empresa.com" --project GuiasBackend
```

### Configuración de producción

En entornos de producción, utiliza variables de entorno del sistema o servicios de gestión de secretos.

Ejemplo con variables de entorno:

```bash
# Linux/macOS
export DB_PASSWORD="tu-contraseña-bd"
export JWT_SECRET_KEY="tu-clave-jwt-muy-segura"
export CERT_PASSWORD="tu-contraseña-certificado"

# Windows (PowerShell)
$env:DB_PASSWORD="tu-contraseña-bd"
$env:JWT_SECRET_KEY="tu-clave-jwt-muy-segura"
$env:CERT_PASSWORD="tu-contraseña-certificado"
```

## Estructura del proyecto

- **Controllers**: Endpoints de la API REST
- **Services**: Lógica de negocio
- **Models**: Entidades y DTOs
- **Data**: Configuración de base de datos

## Tecnologías

- ASP.NET Core 8.0
- Entity Framework Core con Oracle
- JWT para autenticación
- Swagger para documentación
