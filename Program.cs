using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GuiasBackend.Services;
using GuiasBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Oracle.EntityFrameworkCore;
using System.Net;
using GuiasBackend.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using GuiasBackend.Configuration.HealthChecks;
using GuiasBackend.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

ProcessConfiguration(builder.Configuration);

ConfigureKestrel(builder);
ConfigureServices(builder);

var app = builder.Build();
ConfigureMiddleware(app);

await app.RunAsync();

static void ConfigureKestrel(WebApplicationBuilder builder)
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        var addresses = new[] { IPAddress.Any, IPAddress.IPv6Any };
        foreach (var address in addresses)
        {
            serverOptions.Listen(address, 80);
            serverOptions.Listen(address, 443, listenOptions =>
            {
                listenOptions.UseHttps(options =>
                {
                    if (!builder.Environment.IsDevelopment())
                    {
                        options.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                            Path.Combine(builder.Environment.ContentRootPath, "GuiasBackend.pfx"),
                            builder.Configuration["Certificate:Password"] ?? throw new InvalidOperationException("CERT_PASSWORD no configurada")
                        );
                    }
                });
            });
        }
    });
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Content-Disposition");
        });
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; // Cambiado a camelCase
        });

    ConfigureDatabase(builder);
    ConfigureAuthentication(builder);
    ConfigureAuthorization(builder);
    ConfigureSwagger(builder);
    ConfigurePerformance(builder);
    ConfigureHealthChecks(builder);
    ConfigureDependencyInjection(builder);
}

static void ConfigureMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "GuiasBackend API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "GuiasBackend API Documentation";
        });
    }

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseHsts();
    app.UseHttpsRedirection();
    
    app.UseCors("AllowAll");
    
    app.UseRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.UseResponseCaching();
    app.UseResponseCompression();
    app.UseGlobalExceptionHandler();

    app.MapHealthChecks("/health");
    app.MapControllers();
}

static void ConfigureDatabase(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada en appsettings.json");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseOracle(connectionString)
               .LogTo(Console.WriteLine, LogLevel.Debug));
}

static void ConfigureAuthentication(WebApplicationBuilder builder)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Importante para desarrollo
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada"))
            ),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                {
                    context.Token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                }
                return Task.CompletedTask;
            }
        };
    });
}

static void ConfigureAuthorization(WebApplicationBuilder builder)
{
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("ADMINISTRADOR"));
        options.AddPolicy("RequireUserRole", policy => policy.RequireRole("USUARIO", "ADMINISTRADOR"));
        options.FallbackPolicy = null;
    });
}

static void ConfigureSwagger(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "GuiasBackend API",
            Version = "v1",
            Description = "API para gestión de usuarios y guías",
            Contact = new OpenApiContact
            {
                Name = "Administrador",
                Email = "gianxs296@gmail.com"
            }
        });

        // Configuración de seguridad para JWT
        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        c.AddSecurityDefinition("Bearer", securityScheme);

        var securityRequirement = new OpenApiSecurityRequirement
        {
            {
                securityScheme,
                Array.Empty<string>()
            }
        };

        c.AddSecurityRequirement(securityRequirement);
    });
}

static void ConfigurePerformance(WebApplicationBuilder builder)
{
    builder.Services.AddResponseCaching();
    builder.Services.AddMemoryCache();
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });
}

static void ConfigureHealthChecks(WebApplicationBuilder builder)
{
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("Database")
        .AddCheck("Memory", () =>
        {
            var allocated = GC.GetTotalMemory(false);
            var healthy = allocated < 1024 * 1024 * 1024;
            return healthy 
                ? HealthCheckResult.Healthy($"Memoria asignada: {allocated / 1024 / 1024} MB")
                : HealthCheckResult.Degraded($"Memoria alta: {allocated / 1024 / 1024} MB");
        });
}

static void ConfigureDependencyInjection(WebApplicationBuilder builder)
{
    builder.Services.AddScoped<IUsuarioService, UsuarioService>();
    builder.Services.AddScoped<ICampoService, CampoService>();
    builder.Services.AddScoped<ICuartelService, CuartelService>();
    builder.Services.AddScoped<IJironService, JironService>();
    builder.Services.AddScoped<IEmpleadoService, EmpleadoService>();
    builder.Services.AddScoped<IEquipoService, EquipoService>();
    builder.Services.AddScoped<ITransportistaService, TransportistaService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IPasswordService, PasswordService>();
    builder.Services.AddScoped<IGuiasService, GuiasService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
}

static void ProcessConfiguration(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        connectionString = connectionString.Replace("#{DB_PASSWORD}#", 
            configuration["DB_PASSWORD"] ?? throw new InvalidOperationException("DB_PASSWORD no configurada. Ejecuta: dotnet user-secrets set \"DB_PASSWORD\" \"tu-contraseña\""));
        
        configuration["ConnectionStrings:DefaultConnection"] = connectionString;
    }

    var jwtKey = configuration["Jwt:Key"];
    if (!string.IsNullOrEmpty(jwtKey) && jwtKey.Contains("#{JWT_SECRET_KEY}#"))
    {
        configuration["Jwt:Key"] = jwtKey.Replace("#{JWT_SECRET_KEY}#", 
            configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT_SECRET_KEY no configurada. Ejecuta: dotnet user-secrets set \"JWT_SECRET_KEY\" \"tu-clave-jwt\""));
    }

    var certPassword = configuration["Certificate:Password"];
    if (!string.IsNullOrEmpty(certPassword) && certPassword.Contains("#{CERT_PASSWORD}#"))
    {
        configuration["Certificate:Password"] = certPassword.Replace("#{CERT_PASSWORD}#", 
            configuration["CERT_PASSWORD"] ?? throw new InvalidOperationException("CERT_PASSWORD no configurada. Ejecuta: dotnet user-secrets set \"CERT_PASSWORD\" \"tu-contraseña-certificado\""));
    }
    
    // Procesar configuraciones de email si existen
    ProcessEmailConfiguration(configuration);
}

static void ProcessEmailConfiguration(IConfiguration configuration)
{
    var emailUsername = configuration["Email:Username"];
    if (emailUsername?.Contains("#{EMAIL_USERNAME}#") == true)
    {
        configuration["Email:Username"] = emailUsername.Replace("#{EMAIL_USERNAME}#", 
            configuration["EMAIL_USERNAME"] ?? "correo@ejemplo.com");
    }
    
    var emailPassword = configuration["Email:Password"];
    if (emailPassword?.Contains("#{EMAIL_PASSWORD}#") == true)
    {
        configuration["Email:Password"] = emailPassword.Replace("#{EMAIL_PASSWORD}#", 
            configuration["EMAIL_PASSWORD"] ?? "password");
    }
    
    var emailSender = configuration["Email:SenderEmail"];
    if (emailSender?.Contains("#{EMAIL_SENDER}#") == true)
    {
        configuration["Email:SenderEmail"] = emailSender.Replace("#{EMAIL_SENDER}#", 
            configuration["EMAIL_SENDER"] ?? configuration["EMAIL_USERNAME"] ?? "correo@ejemplo.com");
    }
}