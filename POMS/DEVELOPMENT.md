# POMS Development Guide

This document is for developers/AI agents continuing work on the POMS system.

## Project Overview

**POMS** (Prosthetic & Orthotic Management System) - ASP.NET Core 8.0 MVC application for managing prosthetic and orthotic patient care.

## Architecture

```
POMS/
├── src/
│   ├── Poms.Domain/           # Entities, Enums (no dependencies)
│   ├── Poms.Application/      # Business logic layer
│   ├── Poms.Infrastructure/   # Data access, services, EF Core
│   ├── Poms.Web/              # MVC Controllers, Views, ViewModels
│   └── Poms.Reporting/        # Report generation (QuestPDF, ClosedXML)
└── tests/
    └── Poms.Tests/            # Unit tests
```

## Key Technologies

- **Framework:** ASP.NET Core 8.0
- **ORM:** Entity Framework Core 8.0
- **Database:** SQL Server / PostgreSQL (dual support)
- **Identity:** ASP.NET Core Identity with ApplicationUser
- **UI:** Bootstrap 5, jQuery, Chart.js
- **PDF:** QuestPDF
- **Excel:** ClosedXML
- **OCR:** Tesseract.NET (for document text extraction)
- **Image Processing:** SixLabors.ImageSharp

## Database

### Connection String
Located in `src/Poms.Web/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "UsePostgreSQL": false
}
```

### Migrations
```bash
# Create migration
cd src/Poms.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../Poms.Web

# Apply migration
dotnet ef database update --startup-project ../Poms.Web

# List migrations
dotnet ef migrations list --startup-project ../Poms.Web
```

### Current Migrations
1. `InitialCreateWithEnhancements` - Base schema
2. `EnhancedEntities` - Added in March 2026:
   - `AmputationTypeOther` on ProstheticEpisodes
   - `IsActive`, `PhotoPath` on Patients
   - Enhanced Fitting fields (FittingNumber, Status, Adjustments, etc.)
   - OCR fields on PatientDocuments/EpisodeDocuments
   - `IsActive` on Provinces, Districts, Centers
   - `Category` on ComponentCatalogs
   - ApplicationUser fields (FirstName, LastName, IsActive, CenterId)
   - SystemSettings table

## Key Entities

### Patient (`Poms.Domain/Entities/Patient.cs`)
- `IsActive` - Active/Inactive status toggle
- `PhotoPath` - Patient photo storage path
- Guardian fields are mandatory

### Episode Types
- **ProstheticEpisode** - Has `AmputationTypeOther` for custom amputation types
- **OrthoticEpisode** - Body region, orthosis type
- **SpinalEpisode** - Pathological condition, orthotic design

### Fitting (`Poms.Domain/Entities/Fitting.cs`)
Enhanced with:
- `FittingNumber` - Sequential fitting number
- `Status` - FittingStatus enum (Scheduled, InProgress, Completed, Cancelled)
- `Adjustments`, `PatientFeedback`, `NextSteps`
- `NextFittingDate`, `PerformedBy`

### Documents
Both PatientDocument and EpisodeDocument have OCR fields:
- `ExtractedText` - OCR extracted text
- `OcrProcessedAt` - When OCR was processed
- `OcrLanguage` - Language used (eng, sin for Sinhala)
- `OcrStatus` - OcrStatus enum

### ApplicationUser
Custom Identity user in `Poms.Domain/Entities/ApplicationUser.cs`:
- `FirstName`, `LastName`
- `IsActive` - User active status
- `CenterId` - Assigned center
- `CreatedAt`, `LastLoginAt`

## Controllers

### Key Controllers
- `PatientsController` - Patient CRUD, toggle status, photo upload
- `EpisodesController` - Episode management with type-specific handling
- `FittingsController` - Enhanced fitting management
- `DocumentsController` - Document upload with OCR processing
- `AdminController` - Admin dashboard, settings
- `UsersController` - User management with roles
- `ReportsController` - Report generation (PDF/Excel)

### Master Data Controllers (Admin area)
- `ProvincesController`, `DistrictsController`, `CentersController`
- `DeviceTypesController`, `DeviceCatalogController`, `ComponentCatalogController`

### API Controllers
- `Api/LookupController` - AJAX endpoints for cascading dropdowns

## ViewModels

Located in `src/Poms.Web/ViewModels/`:
- `DashboardViewModel.cs` - Home dashboard with KPIs, charts
- `AdminDashboardViewModel.cs` - Admin dashboard
- `UserViewModels.cs` - User list, create, edit
- `MasterDataViewModels.cs` - Province, District, Center, DeviceType, etc.
- `ReportViewModels.cs` - Report filter ViewModels
- `FittingViewModel.cs` - Fitting management
- `DocumentViewModels.cs` - Document list and details

## Services

Located in `src/Poms.Infrastructure/Services/`:
- `DashboardService` - KPIs, chart data, recent activities
- `OcrService` - Tesseract OCR with Sinhala support
- `ReportService` - PDF/Excel report generation
- `FileStorageService` - File upload/download handling

## Views Structure

```
Views/
├── Home/Index.cshtml          # Main dashboard with charts
├── Admin/                     # Admin dashboard, settings, audit logs
├── Users/                     # User management CRUD
├── Reports/                   # Report generation pages
├── Fittings/                  # Fitting management with details
├── Documents/                 # Document management
├── Patients/                  # Patient CRUD with photo upload
├── Episodes/                  # Episode management
└── MasterData/               # Provinces, Districts, Centers, etc.
```

## CSS Theme

Light professional theme in `wwwroot/css/poms-theme.css`:
- CSS variables for consistent colors
- KPI card styles
- Fitting card styles
- Document card styles
- Light navbar theme

## Authentication & Authorization

### Roles
- `ADMIN` - Full system access
- `CLINICIAN` - Patient care, episodes, clinical workflow
- `DATA_ENTRY` - Basic data entry
- `VIEWER` - Read-only access

### Policies (defined in Program.cs)
- `AdminOnly` - ADMIN role required
- `ClinicianOrAdmin` - CLINICIAN or ADMIN
- `DataEntry` - DATA_ENTRY or higher

## Common Tasks

### Adding a New Entity Field
1. Add property to entity in `Poms.Domain/Entities/`
2. Update DbContext if configuration needed
3. Create migration: `dotnet ef migrations add FieldName`
4. Apply migration: `dotnet ef database update`
5. Update ViewModel if displayed in UI
6. Update View to show/edit the field
7. Update Controller to handle the field

### Adding a New Report
1. Add filter ViewModel in `ReportViewModels.cs`
2. Add action methods in `ReportsController`
3. Create view in `Views/Reports/`
4. Add PDF/Excel generation in `ReportService`

### Adding Master Data
1. Create entity if new
2. Add DbSet in PomsDbContext
3. Create Controller (copy from existing, e.g., ProvincesController)
4. Create Views (Index, Create, Edit)
5. Add navigation link in _Layout.cshtml

## Build & Run

```bash
# Build
cd POMS
dotnet build

# Run tests
dotnet test

# Run application
cd src/Poms.Web
dotnet run

# Application runs at http://localhost:5000
```

## Deployment

### Railway (Production)
- **URL:** https://popms.up.railway.app
- **Environment Variables:**
  - `DATABASE_URL` - PostgreSQL connection (auto-set by Railway)
  - `PORT=8080` - Railway expects port 8080
  - `ASPNETCORE_ENVIRONMENT=Production`
- **Important:** PostgreSQL uses `EnsureCreated()` instead of migrations (SQL Server migrations don't work on PostgreSQL)
- Uses `ForwardedHeaders` middleware for HTTPS behind Railway's reverse proxy

### Default Login Credentials
| Role | Email | Password |
|------|-------|----------|
| Admin | admin@poms.lk | Admin@123 |
| Clinician | clinician@poms.lk | Clinic@123 |
| Data Entry | registrar@poms.lk | Data@123 |
| Viewer | viewer@poms.lk | View@123 |

### Docker
Dockerfile is at project root.
- Uses port 8080 by default
- Creates `/app/storage` directory for file uploads

## Known Issues / Warnings

1. **SixLabors.ImageSharp vulnerabilities** - Package has known vulnerabilities, consider upgrading to latest version
2. **Query filter warnings** - EF Core warns about global query filters on related entities (soft delete pattern) - this is expected behavior
3. **Data Protection Keys** - Keys not persisted outside container, sessions expire on redeploy

## Pending Features / TODOs

1. **File Upload on Patient Creation** - Add document/photo upload fields to Patient Create form
   - Files: `Views/Patients/Create.cshtml`, `Controllers/PatientsController.cs`
   - Use `FileStorageService.SavePatientPhotoAsync()` and `SaveFileAsync()`

2. **PostgreSQL Migrations** - Create separate PostgreSQL migration assembly for proper schema management

3. **Data Protection Configuration** - Configure persistent data protection keys for Railway

## Recent Changes (March 2026)

### Railway Production Deployment
- Fixed HTTPS redirect loop with ForwardedHeaders middleware
- Fixed PORT binding (8080 instead of 5000)
- Fixed PostgreSQL schema creation (EnsureCreated vs Migrate)
- Fixed ApplicationUser Identity configuration
- Fixed _LoginPartial.cshtml to use ApplicationUser
- Fixed ViewModel type conflicts (DashboardViewModel, DocumentListItem)

### EnhancedEntities Migration
- Added `AmputationTypeOther` handling in EpisodesController
- Updated Episodes/Edit.cshtml with AmputationTypeOther toggle
- Created all missing ViewModels
- Fixed build errors in DashboardService and ReportService
- Added Category to ComponentCatalog entity

## ViewModel Guidelines

**Important:** Do NOT create duplicate ViewModel classes in Controllers. Always use classes from `Poms.Web.ViewModels` namespace. If a controller-specific ViewModel is needed (like `DashboardViewModel` in `HomeController`), ensure it uses types from `Poms.Infrastructure.Services`.

Example of correct usage:
```csharp
// In Controller - use Infrastructure types
using Poms.Infrastructure.Services; // For DashboardData, ChartDataPoint
// NOT using Poms.Web.ViewModels for these

public class DashboardViewModel
{
    public DashboardData Data { get; set; } = new();  // From Infrastructure
}
```

## Contact

For questions about this codebase:
- Developer: Ranga Nanayakkara
- Company: NeurAlsEdge
- Email: ranga@neuralsedge.com

---
Last Updated: March 14, 2026
