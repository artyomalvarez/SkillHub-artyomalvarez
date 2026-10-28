# SkillHub - Workshop Management System

SkillHub is a full-stack web application (REST API + ASP.NET Core MVC) built to manage workshops and enrollments for a training center. It replaces manual tracking in spreadsheets with centralized capacity control, overlap validation, automated PDF certificate generation, and role-based access for participants and staff (instructors).

---

## Technologies

- **.NET 10** (ASP.NET Core Web API and MVC)
- **Entity Framework Core 10 + Npgsql** (PostgreSQL provider)
- **ASP.NET Core Identity + JWT Bearer Authentication**
- **PostgreSQL 16+** (Local instance)
- **Scalar** (OpenAPI Documentation)
- **QuestPDF** (PDF certificate generation)
- **ClosedXML** (Excel import processing)

---

## Architecture

The solution follows Clean Architecture divided into five projects:

- **`src/SkillHub.Domain`**: Entities (`Workshop`, `Enrollment`), enums, and pure domain rules.
- **`src/SkillHub.Application`**: DTOs, repository/service interfaces, and application services (`EnrollmentService`, `WorkshopService`).
- **`src/SkillHub.Infrastructure`**: `ApplicationDbContext`, EF Core configurations, Identity setup (`ApplicationUser`, `AuthService`, `JwtGenerator`), repositories, PDF/Excel services, and the startup seeder.
- **`src/SkillHub.Api`**: REST controllers, `ExceptionHandlingMiddleware`, Dependency Injection setup, and Scalar configuration.
- **`src/SkillHub.Web`**: ASP.NET Core MVC frontend for participants and instructors, consuming the API via HttpClient (`ApiClient`).

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (or .NET 8+)
- [PostgreSQL 16+](https://www.postgresql.org/) (running on localhost port 5432)
- `dotnet-ef` CLI tool (`dotnet tool install --global dotnet-ef`)

---

## How to Run

### 1. Clone the repository
```bash
git clone <YOUR_GITHUB_REPOSITORY_URL>
cd SkillHub
```

### 2. Configure the database
Make sure your local PostgreSQL service is running. Edit `ConnectionStrings:DefaultConnection` in `src/SkillHub.Api/appsettings.json` with your local Postgres credentials (username and password).

### 3. Apply migrations and run the API
```bash
dotnet ef database update --project src/SkillHub.Infrastructure --startup-project src/SkillHub.Api
dotnet run --project src/SkillHub.Api
```

> **Note:** The API automatically executes the `DataSeeder` on startup, which reads the provided `SkillHub_Data.xlsx` file to populate the database with initial instructors, participants, and workshops.

### 4. Run the MVC Web Client
Open a new terminal instance and run:
```bash
dotnet run --project src/SkillHub.Web
```

### 5. Access the applications
- **Frontend Web App:** `http://localhost:5237` (Check your terminal for the exact port assigned by Kestrel).
- **API Documentation (Scalar):** `http://localhost:5250/scalar/v1` (Check your terminal for the exact port).

---

## Default Credentials

### Staff Accounts (Instructors)
Pre-seeded from the Excel file:
- **Email:** `andres.mejia@correo.com`
- **Password:** `Instructor2026*`
- **Role:** `Staff`

Additional pre-configured instructors: `rosa.barrios@correo.com`, `mauricio.jimenez@correo.com`, `staff@skillhub.com` (`Staff2026*`).

### Participant Accounts
Pre-seeded in the database:
- **Email:** `carlos.rincon@correo.com` (or `jorge.perez@correo.com`)
- **Password:** `Skillhub2026*`
- **Role:** `Participant`

*Note: You can also register a new participant directly from the web interface (`/Account/Register`).*

---

## Implemented Business Rules

1. **Authentication:** Public registration creates participants. Only staff can create workshops and view enrolled participants.
2. **Workshops:** Unique alphanumeric code. Start date must be in the future, and end date must be after the start date.
3. **Catalog:** Publicly accessible. Shows only future workshops and calculates available slots in real-time.
4. **Enrollments:**
   - Capacity limit cannot be exceeded.
   - No duplicate active enrollments for the same participant and workshop.
   - No time overlap with other active enrollments.
   - Cannot enroll in workshops that have already started.
5. **Cancellation:**
   - Participants can cancel up to 24 hours before the start time.
   - Staff can cancel at any time before the workshop starts.
   - Cancellations log the actor and timestamp, and free up the slot immediately.
6. **Excel Import:** Staff can upload an Excel file to mass-enroll participants. Valid rows are saved; invalid rows are rejected with detailed error messages.
7. **PDF Certificate:** Participants can download a PDF certificate for active enrollments only.
8. **Reports:** Staff can view the top workshops ordered by the highest number of active enrollments.

---

## API Endpoints Summary

### Auth
- `POST /api/v1/auth/register` (Public)
- `POST /api/v1/auth/login` (Public)

### Workshops
- `GET /api/v1/workshops` (Public)
- `GET /api/v1/workshops/{id}` (Public)
- `POST /api/v1/workshops` (Staff)
- `GET /api/v1/workshops/{id}/enrollments` (Staff)

### Enrollments
- `POST /api/v1/enrollments` (Participant)
- `GET /api/v1/enrollments/mine` (Participant)
- `PATCH /api/v1/enrollments/{id}/cancel` (Owner or Staff)
- `POST /api/v1/enrollments/import` (Staff)
- `GET /api/v1/enrollments/{id}/certificate` (Owner or Staff)

### Reports
- `GET /api/v1/reports/top-workshops` (Staff)

---

## Known Limitations

- No automated unit or integration tests (verification was done manually through the UI and Scalar).
- No pagination on list endpoints.

---

## Author

- **Name:** Juan José Álvarez
- **Clan:** Esthercita
- **Email:** alvarezmanjarrezjuanjose
