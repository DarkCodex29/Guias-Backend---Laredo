# Guías Laredo Backend - API de Gestión de Guías de Remisión

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-brightgreen.svg)](https://dotnet.microsoft.com/apps/aspnet)
[![C#](https://img.shields.io/badge/C%23-12.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-blueviolet.svg)](https://docs.microsoft.com/en-us/ef/core/)
[![License](https://img.shields.io/badge/License-Proprietary-red.svg)]()

API RESTful desarrollada con ASP.NET Core para la gestión integral de guías de remisión. Proyecto implementado para Agroindustrial Laredo que permite crear, gestionar y monitorear guías de remisión según especificaciones SUNAT. Este backend proporciona una interfaz robusta para el manejo de guías, usuarios y recursos relacionados.

<div align="center">
  <p><strong>Desarrollado para:</strong></p>
  <img src="logo.png" alt="Agroindustrial Laredo Logo" width="250"/>
</div>

## 📋 Características

- **Autenticación y autorización**: Sistema JWT para autenticación segura con roles (ADMINISTRADOR/USUARIO)
- **Gestión de usuarios**: CRUD completo de usuarios con roles y permisos
- **Gestión de guías**: Creación, consulta y administración de guías de remisión
- **Gestión de recursos asociados**: Administración de transportistas, cuarteles, jirones, campos y equipos
- **API RESTful**: Endpoints intuitivos para todas las operaciones del negocio
- **Documentación Swagger**: Documentación interactiva para facilitar la integración
- **Seguridad avanzada**: Encriptación de contraseñas, control de acceso y protección contra ataques
- **Persistencia Oracle**: Integración con base de datos Oracle para almacenamiento confiable

## 🚀 Instalación

### Requisitos previos

- .NET SDK 8.0 o superior
- Acceso a base de datos Oracle
- Editor de código (Visual Studio, VS Code, etc.)
- Acceso a certificado para HTTPS (en producción)

### Pasos de instalación

1. Clona este repositorio:
   ```bash
   git clone https://github.com/tu-usuario/GuiasBackend.git
   cd GuiasBackend
   ```

2. Restaura las dependencias y compila el proyecto:
   ```bash
   dotnet restore
   dotnet build
   ```

3. Configura las variables de entorno mediante User Secrets (desarrollo):
   ```bash
   dotnet user-secrets init --project GuiasBackend
   dotnet user-secrets set "DB_PASSWORD" "tu-contraseña-bd" --project GuiasBackend
   dotnet user-secrets set "JWT_SECRET_KEY" "tu-clave-jwt-muy-segura" --project GuiasBackend
   dotnet user-secrets set "CERT_PASSWORD" "tu-contraseña-certificado" --project GuiasBackend
   dotnet user-secrets set "EMAIL_USERNAME" "tu-correo@empresa.com" --project GuiasBackend
   dotnet user-secrets set "EMAIL_PASSWORD" "tu-contraseña-correo" --project GuiasBackend
   dotnet user-secrets set "EMAIL_SENDER" "tu-correo@empresa.com" --project GuiasBackend
   ```

4. Ejecuta las migraciones de la base de datos (si aplica):
   ```bash
   dotnet ef database update
   ```

5. Ejecuta la aplicación:
   ```bash
   dotnet run --project GuiasBackend
   ```

6. Accede a la documentación de la API:
   ```
   https://localhost:5001/swagger
   ```

## ⚙️ Configuración

### Variables de entorno

El proyecto utiliza variables de entorno para la configuración segura:

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `DB_PASSWORD` | Contraseña de la base de datos Oracle | `contraseña_segura` |
| `JWT_SECRET_KEY` | Clave para firmar tokens JWT | `clave_secreta_muy_larga_y_compleja` |
| `CERT_PASSWORD` | Contraseña del certificado HTTPS | `contraseña_certificado` |
| `EMAIL_USERNAME` | Usuario para envío de correos | `correo@empresa.com` |
| `EMAIL_PASSWORD` | Contraseña del correo | `contraseña_correo` |
| `EMAIL_SENDER` | Dirección del remitente | `correo@empresa.com` |

### ⚠️ Seguridad

Para garantizar la seguridad de las credenciales y datos sensibles:

- Los archivos de configuración con credenciales están incluidos en `.gitignore`
- Se utiliza ASP.NET Core User Secrets para desarrollo local
- En producción, se recomienda usar variables de entorno del sistema
- **NUNCA** guardes credenciales reales en el código fuente o archivos de configuración que se suban al repositorio
- **NUNCA** incluyas información sensible en commits o PRs
- Si crees que has expuesto accidentalmente alguna credencial, cámbiala inmediatamente

### Seguridad de Datos

Para proteger los datos de la aplicación:

1. **Contraseñas**: Almacenadas con hash seguro mediante BCrypt
2. **Autenticación**: Sistema basado en JWT con validación completa
3. **Autorización**: Control de acceso basado en roles
4. **HTTPS**: Obligatorio en producción para todas las comunicaciones
5. **Validación**: Todos los datos de entrada son validados para prevenir inyecciones y otros ataques

## 🏗️ Arquitectura

El proyecto sigue una arquitectura en capas bien definida:

```
/
├── Controllers/       # Endpoints de la API REST
├── Services/          # Lógica de negocio
│   └── Interfaces/    # Interfaces para servicios
├── Models/            # Entidades y modelos de datos
│   └── Requests/      # Modelos para solicitudes
├── DTOs/              # Objetos de transferencia de datos
├── Data/              # Acceso a datos y contexto EF Core
├── Middleware/        # Middleware para procesamiento de solicitudes
├── Extensions/        # Extensiones de métodos útiles
├── Configuration/     # Clases de configuración
├── Helpers/           # Utilidades y helpers
├── Constants/         # Constantes y valores predefinidos
└── Program.cs         # Punto de entrada y configuración
```

### Patrones y frameworks utilizados:

- **ASP.NET Core**: Framework para desarrollo de APIs
- **Entity Framework Core**: ORM para acceso a datos Oracle
- **JWT Bearer**: Para autenticación basada en tokens
- **Swagger/OpenAPI**: Para documentación de API
- **Dependency Injection**: Para gestión de dependencias
- **Repository Pattern**: Para acceso a datos
- **Service Pattern**: Para encapsular lógica de negocio

## 🔍 Validación y Pruebas

La API ha sido validada con:

- **Pruebas manuales**: Verificación de endpoints y flujos de trabajo
- **Pruebas en entorno de QA**: Validación en ambiente similar a producción
- **Verificación SUNAT**: Conformidad con requerimientos oficiales para guías de remisión

## 📄 Licencia

Este proyecto es propiedad de Agroindustrial Laredo S.A.A. Todos los derechos reservados.

## 👥 Desarrollo

Proyecto desarrollado para Agroindustrial Laredo S.A.A.

### Desarrollador Principal
- **Gianpierre Mio**: Desarrollador de software, encargado de implementar esta solución para Agroindustrial Laredo.

Para contribuir al proyecto:

1. Revisa las guías de estilo de código C#
2. Crea una rama para tu funcionalidad (`git checkout -b feature/nueva-funcionalidad`)
3. Haz commit de tus cambios (`git commit -m 'Agrega nueva funcionalidad'`)
4. Envía un Pull Request

## 📞 Contacto

Para soporte o consultas, contacta al desarrollador:

- Nombre: Gianpierre Mio
- Email: gianxs296@gmail.com
- Teléfono: +51952164832
