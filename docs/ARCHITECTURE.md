# Architecture

## Request flow

1. **Browser** — Angular standalone components call typed services in `frontend/src/app/core/services`.
2. **Guards/interceptors** — `authGuard` + `roleGuard` protect routes; `jwtInterceptor` attaches the bearer token; `errorInterceptor` normalises `401/403/ProblemDetails` responses and clears stale sessions.
3. **API** — Controllers in `backend/.../Controllers` validate models, enforce `[Authorize]` / role policies, and delegate to services.
4. **Services** — Implementations in `backend/.../Services/Implementations` contain business rules (duplicate checks, overlap detection, soft deletes, metric aggregation) and return DTOs.
5. **Persistence** — `ApplicationDbContext` maps domain entities plus ASP.NET Identity tables to SQLite (or SQL Server). `DbInitializer` seeds roles, accounts, departments, and leave types on first run.

## Authorization model

- `Admin` — full CRUD for departments, employees, leave types; leave review; attendance log; dashboards; admin registration.
- `Employee` — self-service attendance, leave application/history, profile, employee dashboard.
- Both roles — `/api/auth/me` and active-leave-type listing.

JWTs carry `ClaimTypes.Role` and a custom `EmployeeId` claim. On every authenticated request, `JwtBearerEvents.OnTokenValidated` looks up the employee record and fails validation if that profile has been deactivated.

## Data model (core entities)

- `ApplicationUser` (Identity) → `Employee` → `Department`
- `Employee` → `Attendance` (one record per local calendar day, with check-in/out and work duration)
- `Employee` → `LeaveRequest` → `LeaveType` (status: Pending / Approved / Rejected; admin remarks + reviewed timestamp)

Deactivation is a soft delete (`IsActive = false`) for departments, employees, and leave types, preserving historical attendance and leave records.

## Deleting/resetting data

The project uses `EnsureCreated()` rather than EF migrations. To reset the schema, stop the API, delete `backend/EmployeeManagementSystem.API/EmployeeManagement.db`, and restart.
