# Changelog

Todas las modificaciones notables de este proyecto están documentadas en este archivo siguiendo la política de documentación técnica continua (`America/Bogota`).

## [1.11.0] - 2026-10-27 18:59:00 -05:00

### Fixed
- **Envío de Fechas UTC en `StaffController.Create` (`SkillHub.Web`)**:
  - Se forzó explícitamente `DateTime.SpecifyKind(dto.StartDateTime, DateTimeKind.Utc)` y `DateTime.SpecifyKind(dto.EndDateTime, DateTimeKind.Utc)` en el payload `CreateWorkshopRequestDto` antes de despachar a la API, eliminando fallos 500 por zonas horarias `Unspecified` en PostgreSQL.
  - Verificada la configuración de `JsonOptions` en `ApiClient.cs` con `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` y `PropertyNameCaseInsensitive = true`.

## [1.10.0] - 2026-10-27 18:57:00 -05:00

### Fixed
- **Validación de Fechas UTC y Códigos Duplicados en `WorkshopService.cs` (`SkillHub.Application`)**:
  - Forzado explícito de fechas a UTC con `DateTime.SpecifyKind(request.StartDateTime, DateTimeKind.Utc)` y `DateTime.SpecifyKind(request.EndDateTime, DateTimeKind.Utc)`.
  - Validación de fecha de inicio en el futuro con mensaje exacto `"La fecha de inicio debe ser en el futuro."`.
  - Envoltorio de `_unitOfWork.SaveChangesAsync(ct)` en un bloque `try-catch (DbUpdateException)` que relanza `ApplicationException("El código del taller ya existe en la plataforma.")`.
  - Verificación previa de existencia de instructor mediante `_userService.GetUserIdByEmailAsync(dto.InstructorEmail)`.

## [1.9.0] - 2026-10-27 18:49:00 -05:00

### Fixed
- **Serialización camelCase en `ApiClient.cs` (`SkillHub.Web`)**:
  - Incorporada la política `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` en `JsonOptions` para alinear las peticiones JSON enviadas a la API.
- **Visualización de Errores en `Views/Staff/Create.cshtml` (`SkillHub.Web`)**:
  - Reemplazado el bloque de alerta por `<div asp-validation-summary="All" class="text-danger"></div>` dentro de `@if (!ViewData.ModelState.IsValid)`.
- **Captura sin prefijos en `StaffController.Create` (`SkillHub.Web`)**:
  - Confirmado `ModelState.AddModelError(string.Empty, ex.Message)` en el bloque `catch (ApiException ex)` para desplegar directamente el mensaje real originado en la API.

## [1.8.0] - 2026-10-27 18:46:00 -05:00

### Fixed
- **Envío de Petición y Enlace de Token en `ApiClient.cs` (`SkillHub.Web`)**:
  - Inyección garantizada del header `Authorization: Bearer <token>` en `PostAsync<T>` y `PostAsync` leyendo desde `HttpContext.Session.GetString("JwtToken")`.
  - Extracción detallada del mensaje de error real desde la respuesta JSON (`error`, `message`, `errors`, `title`).
- **Manejo de Fechas y Captura de Errores en `StaffController.Create` (`SkillHub.Web`)**:
  - Conversión a UTC explícita de `StartDateTime` y `EndDateTime` (`ToUniversalTime()` / `DateTime.SpecifyKind`).
  - Registro de errores con `ModelState.AddModelError(string.Empty, $"Error de API: {ex.Message}")` y retorno de la vista conservando el DTO.

## [1.7.0] - 2026-10-27 18:41:00 -05:00

### Fixed
- **Envío de Petición y Enlace de Token en `ApiClient.PostAsync<T>` (`SkillHub.Web`)**:
  - Inyección explícita del token `Bearer` desde `_httpContextAccessor.HttpContext.Session.GetString("JwtToken")` en `PostAsync<TResponse>`.
  - Mejora en `HandleErrorResponseAsync` para deserializar mensajes de error provenientes de propiedades `error`, `message`, `errors` (errores de validación) y `title`.
- **Manejo de Fechas y Captura de Errores en `StaffController.Create` (`SkillHub.Web`)**:
  - Conversión explícita de `StartDateTime` y `EndDateTime` a `DateTimeKind.Utc` mediante `DateTime.SpecifyKind` antes de despachar a la API, previniendo excepciones de tipo en bases de datos PostgreSQL.
  - Captura de excepciones con `ModelState.AddModelError(string.Empty, "Error: " + ex.Message)` y retorno de la vista conservando los datos ingresados en el DTO.
- **Visualización Detallada de Errores en `Views/Staff/Create.cshtml`**:
  - Despliegue dinámico de cada `ErrorMessage` contenido en `ModelState` dentro de la alerta de Bootstrap, asegurando la presentación del mensaje real de error reportado por la API.

## [1.6.0] - 2026-10-27 18:31:00 -05:00

### Fixed
- **Descarga de Constancia PDF en MVC (`SkillHub.Web`)**:
  - En [`Views/Enrollments/Mine.cshtml`](src/SkillHub.Web/Views/Enrollments/Mine.cshtml): Botón de descarga de PDF configurado formalmente como enlace GET directo mediante `<a href="@Url.Action("Certificate", "Enrollments", new { id = enrollment.Id })">`.
  - En [`EnrollmentsController.cs`](src/SkillHub.Web/Controllers/EnrollmentsController.cs): Verificación y consumo mediante `_apiClient.GetByteArrayAsync(...)` retornando `File(pdfBytes, "application/pdf", $"Constancia_{id}.pdf")`.
  - En [`PdfService.cs`](src/SkillHub.Infrastructure/Services/PdfService.cs): Configurado `QuestPDF.Settings.ThrowOnMissingFontFamilies = false` y familia de fuente predeterminada `"Lato"` para evitar excepciones de fuentes no registradas en entornos Linux al generar el documento.

## [1.5.0] - 2026-10-27 18:26:00 -05:00

### Fixed
- **Validación de Identificador y Manejo de Errores en Certificados (`GET /api/v1/enrollments/{id}/certificate`)**:
  - En [`EnrollmentsController.cs`](src/SkillHub.Api/Controllers/EnrollmentsController.cs): Validación para evitar procesar identificadores vacíos (`id == Guid.Empty`), lanzando `ApplicationException("La inscripción no existe o el ID es inválido.")`.
  - En [`EnrollmentService.GetCertificateAsync`](src/SkillHub.Application/Services/EnrollmentService.cs): Si el repositorio retorna `null` o si el ID recibido es `Guid.Empty`, se lanza `ApplicationException("La inscripción no existe o el ID es inválido.")`.
  - En [`ExceptionHandlingMiddleware.cs`](src/SkillHub.Api/Middleware/ExceptionHandlingMiddleware.cs): Captura de `ApplicationException`, `System.ApplicationException`, `BadHttpRequestException` y `FormatException` mapeándolos consistentemente a HTTP 400 Bad Request con `{ "error": "mensaje" }`, evitando que IDs inválidos, inexistentes o malformados generen un HTTP 500 Internal Server Error.

## [1.4.0] - 2026-10-27 18:02:00 -05:00

### Added
- **Datos de Participantes en `EnrollmentDto` (`SkillHub.Application`)**:
  - Incorporadas las propiedades `ParticipantName` y `ParticipantEmail` al record `EnrollmentDto` en [`EnrollmentDtos.cs`](src/SkillHub.Application/DTOs/Enrollments/EnrollmentDtos.cs).
  - Enriquecimiento de datos en `EnrollmentService.GetByWorkshopIdAsync` usando `IUserService` para resolver nombre completo y correo de cada participante inscrito en el taller.
- **Vista Legible de Inscritos en el Panel de Staff (`SkillHub.Web`)**:
  - Actualización de [`Views/Staff/Enrollments.cshtml`](src/SkillHub.Web/Views/Staff/Enrollments.cshtml) para reemplazar la visualización del GUID por columnas dedicadas para Nombre del Participante, Correo Electrónico, Fecha de Inscripción formateada (`dd/MM/yyyy HH:mm`) y badges de estado.

## [1.3.0] - 2026-10-27 17:55:00 -05:00

### Changed
- **Enrutamiento Inicial MVC (`SkillHub.Web/Program.cs`)**:
  - Configuración de la ruta por defecto a `{controller=Workshops}/{action=Index}/{id?}`, permitiendo a cualquier visitante ingresar directamente al catálogo de talleres sin obligar el login al arrancar.
- **Descarga de Constancia en PDF (`EnrollmentsController.Certificate`)**:
  - Nombre de archivo emitido ajustado al formato requerido: `Constancia_{id}.pdf`.
- **Auditoría de Reglas de Negocio (`EnrollmentService.EnrollAsync`)**:
  - Verificada la presencia de las validaciones de negocio en `EnrollAsync`:
    - Detección y rechazo de inscripciones activas duplicadas mediante `HasActiveEnrollmentAsync` lanzando `ApplicationException("Ya cuentas con una inscripción activa en este taller.")`.
    - Detección y rechazo de solapamientos o cruces de horario mediante `HasOverlappingActiveEnrollmentAsync` lanzando `ApplicationException("El horario de este taller se cruza con otra de tus inscripciones activas.")`.

## [1.2.0] - 2026-10-27 17:52:00 -05:00

### Added
- **Panel de Instructor (Staff) en MVC (`SkillHub.Web`)**:
  - Creación del controlador [`StaffController.cs`](src/SkillHub.Web/Controllers/StaffController.cs) con acciones:
    - `Index()`: listado general de talleres para instructores y enlace para consultar inscritos.
    - `Create()` (GET y POST): formulario y publicación de nuevos talleres hacia `POST /api/v1/workshops` consumiendo el `IApiClient`.
    - `Enrollments(Guid id)`: consulta de participantes inscritos por taller consumiendo `GET /api/v1/workshops/{id}/enrollments`.
  - Vistas Razor con diseño Bootstrap 5 y paleta oficial SkillHub:
    - [`Views/Staff/Index.cshtml`](src/SkillHub.Web/Views/Staff/Index.cshtml): tabla interactiva con estado de cupos y acciones rápidas.
    - [`Views/Staff/Create.cshtml`](src/SkillHub.Web/Views/Staff/Create.cshtml): formulario validado para dar de alta nuevos talleres.
    - [`Views/Staff/Enrollments.cshtml`](src/SkillHub.Web/Views/Staff/Enrollments.cshtml): tabla con los detalles e IDs de inscripción.
  - Almacenamiento seguro del rol (`UserRole`) en la sesión tras decodificar el token JWT recibido en [`AccountController.Login`](src/SkillHub.Web/Controllers/AccountController.cs).
  - Actualización condicional del botón de la barra de navegación en [`_Layout.cshtml`](src/SkillHub.Web/Views/Shared/_Layout.cshtml): muestra botón verde "Panel Instructor" si el rol es `Staff`, o "Mis Inscripciones" si es `Participant`.

### Changed
- **Logout en MVC (`AccountController.Logout`)**:
  - Limpieza completa de sesión mediante `HttpContext.Session.Clear()` y redirección inmediata al catálogo público `Workshops/Index`.
- **Descarga de Constancia PDF (`EnrollmentsController.Certificate`)**:
  - Invocación de `_apiClient.GetByteArrayAsync(...)` asegurando la lectura directa de bytes (`ReadAsByteArrayAsync`) y retorno de archivo con nombre `constancia.pdf`.
- **Corrección de Documentación de Credenciales (`README.md`)**:
  - Ajuste exacto de la lista de instructores sembrados desde `SkillHub_Data.xlsx` a los 6 autorizados: `andres.mejia`, `rosa.barrios`, `mauricio.jimenez`, `natalia.herrera`, `hernan.quintero` y `paola.vargas` (`@correo.com` / `Instructor2026*`).

## [1.1.0] - 2026-10-27 17:45:00 -05:00

### Added
- **Regla de Cancelación de 24 Horas (`EnrollmentService.CancelAsync`)**:
  - Implementación de la restricción que impide a los participantes cancelar inscripciones cuando faltan menos de 24 horas para el inicio del taller.
  - Habilitación de cancelación para empleados (`Staff`) en cualquier momento previo al inicio del taller, registrando la auditoría con `CancelledBy` y `CancelledAt`.
- **Inscripción Masiva con ClosedXML (`ExcelService.ImportEnrollmentsAsync`)**:
  - Implementación completa de la lógica de negocio para la lectura e importación masiva de participantes y talleres desde archivos `.xlsx`.
  - Validación rigurosa por fila: verificación de existencia y rol de participante, existencia de taller por código, talleres futuros, cupos disponibles, detección de inscripciones duplicadas y cruces de horario.
  - Persistencia atómica por fila válida con `IUnitOfWork`, garantizando que las filas correctas se guarden aunque otras del mismo archivo fallen.
  - Retorno detallado de `ImportResultDto` con conteo de éxitos y lista de errores con número de fila y mensaje descriptivo.
- **Constancia de Inscripción en PDF (`PdfService.GenerateEnrollmentCertificate`)**:
  - Inclusión del nombre del instructor y correo del participante en la constancia.
  - Adición de fecha de inscripción y fecha de generación del documento.
  - Ajuste del título formal a "Constancia de inscripción" y validación estricta de estado `Active` en `EnrollmentService.GetCertificateAsync`.
- **Documentación de Credenciales de Instructores (`README.md`)**:
  - Tabla completa con los correos y contraseñas de los 8 instructores sembrados desde `SkillHub_Data.xlsx` (`Instructor2026*`).
  - Credenciales de prueba de participantes (`Skillhub2026*`).

## [1.0.0] - 2026-10-27 16:02:45 -05:00

### Added
- **Capa Domain (`SkillHub.Domain`)**:
  - Entidad `Workshop` con reglas de negocio `HasAvailableCapacity` y `OverlapsWith`.
  - Entidad `Enrollment` con regla de negocio encapsulada `Cancel`.
  - Enum `EnrollmentStatus` (`Active = 1`, `Cancelled = 2`).
  - Excepción `DomainException`.
- **Capa Application (`SkillHub.Application`)**:
  - DTOs inmutables con records: `AuthDtos`, `WorkshopDtos`, `EnrollmentDtos`, `ExcelDtos`, `ReportDtos`.
  - Interfaces de persistencia: `IUnitOfWork`, `IWorkshopRepository`, `IEnrollmentRepository`.
  - Interfaces de servicios externos: `IAuthService`, `IUserService`, `IPdfService`, `IExcelService`.
  - Servicios de aplicación: `WorkshopService`, `EnrollmentService`, `ReportService`.
  - Inyección de dependencias `AddApplicationServices()`.
- **Capa Infrastructure (`SkillHub.Infrastructure`)**:
  - Persistencia con `ApplicationDbContext` (PostgreSQL / EF Core).
  - Repositorios `WorkshopRepository`, `EnrollmentRepository` y `UnitOfWork`.
  - `DataSeeder` con creación automática de roles (`Staff`, `Participant`) y usuario administrador por defecto (`staff@skillhub.com`).
  - Identidad con `ApplicationUser` (`FullName`, `Specialty`), `JwtGenerator`, `AuthService`, `UserService`.
  - Generación de certificados PDF con QuestPDF (`PdfService`).
  - Importación masiva de inscripciones Excel con ClosedXML (`ExcelService`).
  - Inyección de dependencias `AddInfrastructureServices()`.
- **Capa API (`SkillHub.Api`)**:
  - Controladores REST: `AuthController`, `WorkshopsController`, `EnrollmentsController`, `ReportsController`.
  - Middleware de excepciones `ExceptionHandlingMiddleware` (HTTP 400 y 500).
  - Documentación interactiva de API con **Scalar** en `/scalar/v1`.
- **Capa Web MVC (`SkillHub.Web`)**:
  - Servicio `IApiClient` / `ApiClient` con adjunto automático de token `Bearer` desde sesión.
  - Filtro `SessionAuthorizeAttribute` para protección de rutas de participantes.
  - Controladores MVC: `AccountController`, `WorkshopsController`, `EnrollmentsController`.
  - Vistas Razor con diseño Stitch/Bootstrap 5: `_Layout.cshtml`, `Login.cshtml`, `Register.cshtml`, `Index.cshtml`, `Mine.cshtml`.
  - Integración del isotipo vectorial SVG en la barra de navegación del Layout maestro (hexágono azul y birrete verde representativos de SkillHub).
  - Configuración de la ruta por defecto a `Account/Login` en `Program.cs`.
  - Protección estricta en `EnrollmentsController` con redirección automática a `Account/Login` si no existe sesión activa.
  - Botón contextual e inteligente en `Workshops/Index.cshtml` ("Inicia sesión para inscribirte" vs "Inscribirme").
- **Diagnóstico y Corrección de Seeder (`SkillHub_Data.xlsx`)**:
  - Configuración explícita de copia del archivo Excel en `SkillHub.Api.csproj` (`CopyToOutputDirectory: Always`).
  - Desenpaquetado de `AggregateException` en el `try-catch` de `Program.cs` para visibilidad de errores con colores en consola.
  - Resolución robusta de rutas relativas y absolutas del Excel con fallbacks en `DataSeeder.cs`.
  - Conversión segura de fechas de Excel (números de serie OADate flotantes como `46361.33`) y asignación estricta de `DateTimeKind.Utc` para evitar excepciones de Npgsql con columnas `timestamp with time zone`.
  - Validación y registro detallado de errores en creación de usuarios Identity, evitando fallos silenciosos al asignar instructores a talleres.
