# SkillHub - Plataforma de Gestión de Talleres Formativos

Solución integral basada en **Clean Architecture** con **.NET 10**, **PostgreSQL**, **ASP.NET Core Identity**, **JWT**, **Scalar OpenAPI**, **ClosedXML**, **QuestPDF** y frontend **ASP.NET Core MVC con Bootstrap 5**.

---

## 🏛️ Arquitectura de la Solución

```
SkillHub/
├── src/
│   ├── SkillHub.Domain           # Entidades, Enums y Excepciones puras (Sin dependencias externas)
│   ├── SkillHub.Application      # Casos de uso, DTOs, Interfaces y Servicios de Negocio
│   ├── SkillHub.Infrastructure   # EF Core (PostgreSQL), Identity, JWT, QuestPDF, ClosedXML
│   ├── SkillHub.Api              # Web API REST con Scalar y Controladores desacoplados
│   └── SkillHub.Web              # Aplicación Web MVC (Consumidor HTTP de la API con Sesión JWT)
└── SkillHub.slnx
```

---

## 🚀 Instrucciones de Ejecución

### 1. Iniciar la API REST (`SkillHub.Api`)
```bash
dotnet run --project src/SkillHub.Api
```
- **URL Base:** `http://localhost:5250`
- **Documentación Scalar:** `http://localhost:5250/scalar/v1`

### 2. Iniciar la Aplicación Web MVC (`SkillHub.Web`)
En otra terminal:
```bash
dotnet run --project src/SkillHub.Web
```
- **Acceso:** `http://localhost:5237` (o el puerto configurado por Kestrel en consola).
- **Ruta de inicio por defecto:** `/Account/Login`

---

## 🔑 Credenciales de Acceso (Sembradas en Primer Arranque)

Los instructores y empleados de SkillHub cuentan con cuentas preconfiguradas con el rol **Staff**. Puedes ingresar inmediatamente con cualquiera de las siguientes credenciales:

### 👨‍🏫 Instructores / Empleados (Rol: `Staff`):
| Nombre Completo | Correo Electrónico | Contraseña | Especialidad |
| :--- | :--- | :--- | :--- |
| **Andrés Mejía** | `andres.mejia@correo.com` | `Instructor2026*` | Fotografía Digital |
| **Rosa Barrios** | `rosa.barrios@correo.com` | `Instructor2026*` | Diseño Gráfico e Ilustración |
| **Mauricio Jiménez** | `mauricio.jimenez@correo.com` | `Instructor2026*` | Programación Web y Cloud |
| **Natalia Herrera** | `natalia.herrera@correo.com` | `Instructor2026*` | Marketing Digital y SEO |
| **Hernán Quintero** | `hernan.quintero@correo.com` | `Instructor2026*` | Producción Audiovisual |
| **Paola Vargas** | `paola.vargas@correo.com` | `Instructor2026*` | Finanzas y Emprendimiento |

> **Nota:** También se cuenta con una cuenta administrativa de soporte: `staff@skillhub.com` / `Staff2026*`.

### 🎓 Participantes de Ejemplo (Rol: `Participant`):
| Nombre Completo | Correo Electrónico | Contraseña |
| :--- | :--- | :--- |
| **Carlos Rincón** | `carlos.rincon@correo.com` | `Skillhub2026*` |
| **Valentina Gómez** | `valentina.gomez@correo.com` | `Skillhub2026*` |
| **Juan Restrepo** | `juan.restrepo@correo.com` | `Skillhub2026*` |
| **María Torres** | `maria.torres@correo.com` | `Skillhub2026*` |

Cualquier nuevo usuario que se registre a través del formulario público (`/Account/Register`) será asignado automáticamente con el rol de **`Participant`**.

---

## 📋 Reglas de Negocio Implementadas

1. **Usuarios y Acceso:**
   - Registro público exclusivo con rol `Participant`.
   - Consulta pública del catálogo de talleres disponibles sin necesidad de autenticación.
   - Restricción estricta: los participantes solo pueden ver y gestionar sus propias inscripciones.
   - Los instructores/empleados (`Staff`) pueden publicar talleres, ver los inscritos de cualquier taller y consultar reportes.

2. **Talleres:**
   - Código único alfanumérico (ej. `FOT-001`, `DIS-001`).
   - Todo taller tiene asignado un instructor registrado en la plataforma.
   - Las fechas de inicio deben ser futuras y la fecha de finalización debe ser posterior a la de inicio.
   - El catálogo muestra únicamente talleres futuros con el cálculo en tiempo real de cupos disponibles (`Capacidad - Inscripciones Activas`).

3. **Inscripciones y Validaciones:**
   - **Regla 1 (Capacidad):** Solo se permite la inscripción si el taller cuenta con cupos disponibles (las inscripciones canceladas liberan cupo de inmediato).
   - **Regla 2 (No Duplicados):** Un participante no puede inscribirse más de una vez de forma activa en el mismo taller.
   - **Regla 3 (No Solapamiento):** No se permite la inscripción si el taller coincide o se cruza en horario con otra inscripción activa del mismo participante.
   - **Regla 4 (Talleres Futuros):** No es posible inscribirse en talleres ya iniciados o del pasado.
   - **Regla 5 (Cancelación y Regla de 24 Horas):**
     - Un participante puede cancelar su inscripción **hasta 24 horas antes** del inicio del taller. Pasado ese límite, el sistema bloquea la acción.
     - Un empleado (`Staff`) puede cancelar la inscripción de cualquier participante en cualquier momento previo al inicio del taller, registrándose en base de datos quién canceló (`CancelledBy`) y en qué instante (`CancelledAt`).
   - **Regla 6 (Reinscripción):** Un usuario cuya inscripción fue cancelada puede volver a inscribirse si aún hay cupos disponibles y cumple con las reglas horarias.

4. **Inscripción Masiva vía Excel (`POST /api/v1/enrollments/import`):**
   - Procesamiento de archivos `.xlsx` con columnas `ParticipantEmail` y `WorkshopCode`.
   - Validación individual fila por fila con todas las reglas de negocio.
   - Tolerancia a fallos: las filas válidas se persisten exitosamente aunque otras fallen.
   - Respuesta detallada indicando total de éxitos y listado de errores con número de fila y causa exacta.

5. **Constancia de Inscripción en PDF (`GET /api/v1/enrollments/{id}/certificate`):**
   - Generación bajo demanda usando QuestPDF Community.
   - Válido únicamente para inscripciones en estado `Active`.
   - Incluye: Nombre de SkillHub, título "Constancia de inscripción", nombre y correo del participante, código y título del taller, instructor asignado, fecha y horario del taller (UTC), fecha de inscripción y fecha de generación del documento.

6. **Reportes Administrativos (`GET /api/v1/reports/top-workshops`):**
   - Consulta restringida a `Staff`.
   - Orden descendente por mayor número de inscripciones activas.
