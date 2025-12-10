# POMS - Prosthetic & Orthotic Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Proprietary-red.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-Production%20Ready-brightgreen.svg)](https://github.com)

**POMS** is an enterprise-grade Patient & Episode Management System designed specifically for prosthetic and orthotic clinics. Built with ASP.NET Core 8.0, it provides comprehensive patient tracking, clinical workflow management, and multi-location support.

---

## 📋 Table of Contents

- [Features Overview](#-features-overview)
- [System Architecture](#-system-architecture)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [User Roles & Permissions](#-user-roles--permissions)
- [Module Documentation](#-module-documentation)
  - [Patient Management](#1-patient-management-module)
  - [Episode Management](#2-episode-management-module)
  - [Clinical Workflow](#3-clinical-workflow-modules)
  - [Multi-Location Support](#4-multi-location-management)
  - [Document Management](#5-document-management)
  - [Reporting & Analytics](#6-reporting--analytics)
  - [Security & Audit](#7-security--audit-features)
- [Database Schema](#-database-schema)
- [API Documentation](#-api-documentation)
- [Development](#-development)
- [Deployment](#-deployment)
- [Troubleshooting](#-troubleshooting)
- [License](#-license)

---

## 🚀 Features Overview

### Core Capabilities

- ✅ **Multi-Location Support** - Manage operations across multiple centers/countries
- ✅ **Patient Management** - Comprehensive patient profiles with mandatory guardian information
- ✅ **Episode Tracking** - Prosthetic, Orthotic, and Spinal Orthosis treatment episodes
- ✅ **Clinical Workflow** - Integrated Assessment → Fitting → Delivery → Follow-up process
- ✅ **Document Management** - Patient and episode-level document storage with multi-file upload
- ✅ **Device Catalog** - Manage prosthetic and orthotic devices with component tracking
- ✅ **Role-Based Access Control** - Admin, Clinician, and Data Entry roles
- ✅ **Comprehensive Reporting** - 20+ pre-built reports with PDF/Excel export
- ✅ **Audit Trail** - Complete tracking of all system changes
- ✅ **Responsive Design** - Mobile-optimized interface for tablets and smartphones

### Key Differentiators

- 🔒 **Security-First Design** - Industry-standard authentication, encryption, and audit logging
- 🌍 **Multi-Location Architecture** - Location-based patient numbering (e.g., SL-RGM-2025-0001)
- 📊 **Real-Time Analytics** - Dashboard with live metrics and KPIs
- 📁 **Episode Documents** - Separate document management per episode (assessments, X-rays, photos)
- 🔄 **Soft Delete** - Data is never permanently deleted, ensuring compliance
- 🎨 **Professional UI** - Bootstrap 5 with intuitive navigation

---

## 🏗️ System Architecture

### Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Backend** | ASP.NET Core 8.0 | Web framework, API, business logic |
| **Database** | SQL Server / PostgreSQL | Data persistence (dual support) |
| **Frontend** | Razor Pages + Bootstrap 5 | Server-side rendering, responsive UI |
| **Authentication** | ASP.NET Identity | User management, role-based access |
| **Logging** | Serilog | Structured logging with daily rotation |
| **ORM** | Entity Framework Core 8.0 | Database access and migrations |
| **File Storage** | Local File System | Document storage with metadata tracking |

### Architecture Pattern

```
┌─────────────────────────────────────────────────────┐
│                  Presentation Layer                  │
│           (Poms.Web - MVC Controllers & Views)       │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────┴──────────────────────────────┐
│               Application Layer                      │
│        (Poms.Application - Business Logic)           │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────┴──────────────────────────────┐
│              Infrastructure Layer                    │
│  (Poms.Infrastructure - Data Access, Services)       │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────┴──────────────────────────────┐
│                 Domain Layer                         │
│       (Poms.Domain - Entities, Enums)                │
└──────────────────────────────────────────────────────┘
```

**Design Principles:**
- Clean Architecture with separated concerns
- Domain-Driven Design (DDD)
- Repository pattern (via EF Core DbContext)
- Dependency Injection throughout

---

## 📦 Installation

### Prerequisites

- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server 2019+** or **PostgreSQL 13+**
- **Visual Studio 2022** (recommended) or **VS Code**
- **Windows Server 2019+** or **Ubuntu 20.04+** (for deployment)

### Quick Start

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourorg/poms.git
   cd poms
   ```

2. **Configure database connection:**
   Edit `POMS/src/Poms.Web/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=PomsDb;Trusted_Connection=true;TrustServerCertificate=true;"
     },
     "UsePostgreSQL": false
   }
   ```

3. **Run database migrations:**
   ```bash
   cd POMS/src/Poms.Web
   dotnet ef database update
   ```

4. **Run the application:**
   ```bash
   dotnet run
   ```

5. **Access the application:**
   - URL: http://localhost:5000
   - Default Admin: (created during seeding)

### Docker Deployment

```bash
docker build -t poms:latest -f Dockerfile .
docker run -d -p 5000:5000 --name poms-app poms:latest
```

---

## ⚙️ Configuration

### Application Settings

**`appsettings.json`** - Base configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PomsDb;..."
  },
  "UsePostgreSQL": false,
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  },
  "FileStorage": {
    "RootPath": "C:\\PomsStorage",
    "MaxFileSizeMB": 10,
    "AllowedExtensions": [".pdf", ".jpg", ".jpeg", ".png", ".docx"]
  }
}
```

**`appsettings.Development.json`** - Development overrides

### Environment Variables

For **Railway/Cloud** deployment:

```bash
DATABASE_URL=postgresql://user:password@host:port/database
PORT=5000
ASPNETCORE_ENVIRONMENT=Production
```

### File Storage Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `RootPath` | `C:\PomsStorage` | Root directory for document storage |
| `MaxFileSizeMB` | `10` | Maximum file upload size in MB |
| `AllowedExtensions` | `.pdf, .jpg, .jpeg, .png, .docx` | Permitted file types |

**Storage Structure:**
```
PomsStorage/
├── patients/
│   ├── SL-RGM-2025-0001/
│   │   ├── 2025/
│   │   │   ├── 01/
│   │   │   │   ├── id-card.pdf
│   │   │   │   └── medical-report.pdf
│   │   │   └── 02/
│   │   └── ...
│   └── ...
└── episodes/
    ├── {episode-id}/
    │   ├── 2025/
    │   │   ├── 01/
    │   │   │   ├── assessment-report.pdf
    │   │   │   ├── fitting-photo.jpg
    │   │   │   └── xray.jpg
    │   │   └── ...
    │   └── ...
    └── ...
```

---

## 👥 User Roles & Permissions

### Role Hierarchy

```
ADMIN
  └─ Full system access
     └─ User management
     └─ System configuration
     └─ All reports

CLINICIAN
  └─ Patient care
     └─ Episode management
     └─ Clinical documentation
     └─ Clinical reports

DATA_ENTRY
  └─ Basic data entry
     └─ Patient registration
     └─ Limited access
```

### Detailed Permission Matrix

| Feature | ADMIN | CLINICIAN | DATA_ENTRY |
|---------|:-----:|:---------:|:----------:|
| **Patient Management** |
| Register Patient | ✓ | ✓ | ✓ |
| Edit Patient | ✓ | ✓ | ✗ |
| Delete Patient (Soft) | ✓ | ✗ | ✗ |
| View All Patients | ✓ | ✓ | ✗ |
| **Episode Management** |
| Create Episode | ✓ | ✓ | ✓ |
| Edit Episode | ✓ | ✓ | ✗ |
| Close Episode | ✓ | ✓ | ✗ |
| **Clinical Workflow** |
| Create Assessment | ✓ | ✓ | ✗ |
| Create Fitting | ✓ | ✓ | ✗ |
| Create Delivery | ✓ | ✓ | ✗ |
| Schedule Follow-up | ✓ | ✓ | ✗ |
| Log Repair | ✓ | ✓ | ✗ |
| **Documents** |
| Upload Documents | ✓ | ✓ | ✓ |
| Download Documents | ✓ | ✓ | ✓ |
| Delete Documents | ✓ | ✓ | ✗ |
| **Reporting** |
| View Dashboard | ✓ | ✓ | ✓ |
| Patient Reports | ✓ | ✓ | ✗ |
| Episode Reports | ✓ | ✓ | ✗ |
| Clinical Reports | ✓ | ✓ | ✗ |
| Audit Reports | ✓ | ✗ | ✗ |
| Export PDF/Excel | ✓ | ✓ | ✗ |
| **Administration** |
| User Management | ✓ | ✗ | ✗ |
| Device Catalog | ✓ | ✗ | ✗ |
| Location Management | ✓ | ✗ | ✗ |
| System Configuration | ✓ | ✗ | ✗ |
| View Audit Logs | ✓ | ✗ | ✗ |

### Default Users (Seeded)

| Username | Password | Role | Email |
|----------|----------|------|-------|
| `admin` | `Admin@123` | ADMIN | admin@poms.com |
| `clinician` | `Clinic@123` | CLINICIAN | clinician@poms.com |
| `dataentry` | `Data@123` | DATA_ENTRY | data@poms.com |

> ⚠️ **Security Note:** Change default passwords immediately after first login!

---

## 📚 Module Documentation

### 1. Patient Management Module

#### Overview
Complete patient lifecycle management from registration to treatment completion.

#### Features

##### 1.1 Patient Registration

**Location:** `/Patients/Create`

**Fields:**
- **Personal Information:**
  - First Name (Required)
  - Last Name (Optional)
  - Date of Birth (Optional)
  - Sex (Required) - Male, Female, Other
  - National ID / NIC (Optional)

- **Contact Information:**
  - Address Line 1 (Required)
  - Address Line 2 (Optional)
  - Province (Required) → District (Required) → Center (Required)
  - Phone 1 (Optional)
  - Phone 2 (Optional)
  - Email (Optional)

- **Guardian Information (MANDATORY):**
  - Guardian Name (Required) - ⭐ NEW
  - Guardian Relationship (Optional)
  - Guardian Address (Optional)
  - Guardian Phone 1 (Required) - ⭐ NEW
  - Guardian Phone 2 (Optional)

- **Registration Details:**
  - Registration Date (Auto-filled with today's date)
  - Referred By (Optional)
  - Remarks (Required)

**Patient Number Generation:**
- Format: `{CenterCode}-{Year}-{SequenceNumber}`
- Examples:
  - `SL-RGM-2025-0001` (Sri Lanka - Ragama - 2025 - Patient 1)
  - `PH-QC-2025-0042` (Philippines - Quezon City - 2025 - Patient 42)
  - `KH-PP-2025-0105` (Cambodia - Phnom Penh - 2025 - Patient 105)

**Auto-Number Logic:**
```csharp
// Generates next sequential number per center per year
// Format: {CenterCode}-{YYYY}-{SequenceNumber:0000}
var patientNumber = await _patientNumberService.GeneratePatientNumberAsync(centerId, registrationDate);
```

##### 1.2 Patient Search & Filtering

**Location:** `/Patients/Index`

**Search Options:**
- Search by: Patient Number, Name, National ID
- Filter by: Center, District, Registration Date Range
- Pagination: 20 patients per page
- Sorting: By registration date (newest first)

**Quick Actions:**
- View Details
- Edit Patient
- Create Episode
- View Episodes

##### 1.3 Patient Details

**Location:** `/Patients/Details/{id}`

**Tabs:**
1. **Overview** - Personal information, guardian details
2. **Conditions** - Medical conditions with diagnosis dates
3. **Episodes** - Treatment history (open and closed)
4. **Documents** - Uploaded files with download/delete options

**Actions Available:**
- Edit Patient
- Create New Episode
- Upload Document
- Add Condition
- View Episode History

##### 1.4 Patient Documents

**Upload Specifications:**
- Max file size: 10 MB per file
- Allowed formats: PDF, JPG, JPEG, PNG, DOCX
- Storage path: `{RootPath}/patients/{PatientNumber}/{Year}/{Month}/`
- Metadata tracked: Title, Upload Date, Uploaded By, Remarks

**Document Operations:**
- Upload (single or multiple files)
- Download (secure file retrieval)
- Delete (soft delete with audit trail)
- Tag/Categorize (using TagsJson field)

##### 1.5 Audit Trail

**All patient operations tracked:**
- Created By / Created At
- Updated By / Updated At
- Deleted By / Deleted At (soft delete)
- Change history preserved

**Example Audit Entry:**
```json
{
  "Action": "UPDATE",
  "EntityType": "Patient",
  "EntityId": "guid-here",
  "UserId": "user-guid",
  "Timestamp": "2025-12-03T10:30:00Z",
  "Changes": {
    "Phone1": {
      "Old": "+94711234567",
      "New": "+94777654321"
    }
  }
}
```

---

### 2. Episode Management Module

#### Overview
Tracks complete treatment episodes from initial assessment to delivery and follow-up.

#### Episode Types

##### 2.1 Prosthetic Episodes

**Use Case:** Artificial limb treatments

**Required Fields:**
- Patient (Required)
- Episode Type: `Prosthetic` (Required)
- Opened Date (Required)
- Closed Date (Optional - for completed treatments)

**Prosthetic-Specific Fields:**
- **Amputation Type** (Dropdown):
  - Below Knee (Trans-tibial)
  - Above Knee (Trans-femoral)
  - Below Elbow (Trans-radial)
  - Above Elbow (Trans-humeral)
  - Partial Hand
  - Partial Foot
  - Other

- **Level** (Text): e.g., "Trans-tibial", "Knee disarticulation"

- **Side** (Dropdown):
  - Left
  - Right
  - Bilateral (both sides)
  - Not Applicable

- **Reason** (Dropdown):
  - Disease
  - Trauma
  - Vascular
  - Diabetic
  - Cancer
  - Congenital
  - Other

**Example Prosthetic Episode:**
```
Patient: SL-RGM-2025-0001 (John Doe)
Type: Prosthetic
Amputation Type: Below Knee
Level: Trans-tibial
Side: Right
Reason: Diabetic
Opened: 2025-01-15
Status: Open
```

##### 2.2 Orthotic Episodes ⭐ ENHANCED

**Use Case:** Braces and support devices

**Required Fields:**
- Patient (Required)
- Episode Type: `Orthotic` (Required)
- Opened Date (Required)

**Orthotic-Specific Fields:**

- **Main Problem** (Text Field) - ⭐ NEW
  - Free text to describe primary issue
  - Examples: "Foot drop", "Knee instability", "Wrist weakness"

- **Body Region** (Dropdown):
  - Upper Limb
  - Lower Limb
  - Spine
  - Other

- **Side** (Dropdown):
  - Left
  - Right
  - Bilateral
  - Not Applicable

- **Orthosis Type** (Dropdown from Device Catalog) - ⭐ NEW
  - Ankle-Foot Orthosis (AFO)
  - Knee-Ankle-Foot Orthosis (KAFO)
  - Thoraco-Lumbo-Sacral Orthosis (TLSO)
  - Lumbo-Sacral Orthosis (LSO)
  - Wrist-Hand Orthosis (WHO)
  - Custom types from catalog

- **Reason for Problem** (Text Field) - ⭐ NEW
  - Free text to explain cause
  - Examples: "Stroke (CVA)", "Spinal cord injury", "Birth injury"

**Example Orthotic Episode:**
```
Patient: PH-QC-2025-0015 (Maria Santos)
Type: Orthotic
Main Problem: Foot drop affecting gait
Body Region: Lower Limb
Side: Left
Orthosis Type: Ankle-Foot Orthosis (AFO)
Reason for Problem: Stroke (CVA) in 2024
Opened: 2025-02-01
Status: Open
```

##### 2.3 Spinal Orthosis Episodes ⭐ NEW

**Use Case:** Back and spine support devices

**Required Fields:**
- Patient (Required)
- Episode Type: `SpinalOrthosis` (Required)
- Opened Date (Required)

**Spinal-Specific Fields:**

- **Pathological Condition** (Text Field)
  - Free text to describe spinal condition
  - Examples: "Scoliosis (40° curve)", "Post-surgical stabilization", "Compression fracture L1"

- **Orthotic Design** (Text Field)
  - Free text to specify brace type
  - Examples: "TLSO (Thoraco-Lumbo-Sacral Orthosis)", "Milwaukee brace", "Jewett hyperextension brace"

> ⚠️ **Important:** Spinal episodes do NOT have a "Side" field (spine is central)

**Example Spinal Episode:**
```
Patient: KH-PP-2025-0008 (Sok Vannak)
Type: SpinalOrthosis
Pathological Condition: Adolescent idiopathic scoliosis (35° thoracic curve)
Orthotic Design: TLSO custom-molded rigid brace
Opened: 2025-03-10
Status: Open
```

#### Episode Workflow Features

##### 2.4 Integrated Clinical Workflow ⭐ MAJOR ENHANCEMENT

All three sections (Assessment, Fitting, Delivery) can be filled during episode creation!

**Location:** `/Episodes/Create`

###### Assessment Section (Optional)

**When to Use:** Document initial evaluation

**Fields:**
- **Date of Assessment** (Date Picker)
  - Defaults to episode opened date
  - Can be set to any date

- **Assessment Findings** (Rich Text Area)
  - Physical examination results
  - Measurements (stump length, circumference, ROM)
  - Clinical observations
  - Recommendations for device type/components

**Example Assessment Entry:**
```
Date of Assessment: 2025-01-20
Findings:
- Residual limb length: 15cm below knee
- Circumference: Proximal 38cm, Mid 32cm, Distal 28cm
- Skin condition: Healthy, no adhesions
- ROM: Full knee flexion/extension
- Muscle strength: 4/5 quadriceps, 4/5 hamstrings
- Recommendation: Trans-tibial PTB socket with SACH foot
```

**Automatic Record Creation:**
- If assessment date or findings provided → Assessment record created automatically
- Linked to episode via `EpisodeId`
- Clinician auto-populated from logged-in user

###### Fitting Section (Optional)

**When to Use:** Document device fitting process

**Fields:**
- **Fitting Date** (Date Picker)
  - Typically 2-4 weeks after assessment

- **Fitting Notes** (Rich Text Area)
  - Fitting process documentation
  - Adjustments made
  - Patient feedback on comfort
  - Alignment notes
  - Issues encountered and resolved

**Example Fitting Entry:**
```
Fitting Date: 2025-02-10
Notes:
- Initial fitting of PTB socket with SACH foot
- Minor adjustments to socket trim lines for comfort
- Heel height adjusted for optimal alignment
- Patient able to bear weight comfortably
- Gait training initiated - patient walking 50m with walker
- Some minor pressure at fibular head - will monitor
- Follow-up scheduled in 1 week for fine-tuning
```

**Automatic Record Creation:**
- If fitting date or notes provided → Fitting record created
- Multiple fittings can be added later for adjustments

###### Delivery Section (Optional)

**When to Use:** Record final device delivery

**Fields:**
- **Delivery Date** (Date Picker)
  - Date when device handed over to patient

**Automatic Fields:**
- **Delivered By:** Auto-populated with current user's name
- **Remarks:** "Created with episode" (auto-generated)

**Example Delivery Entry:**
```
Delivery Date: 2025-02-24
Delivered By: Dr. Sarah Johnson (Clinician)
Remarks: Patient satisfied with final fit and function
```

**Automatic Record Creation:**
- If delivery date provided → Delivery record created
- Links episode to delivered device

##### 2.5 Episode Status Management

**Episode States:**
- **Open** - Active treatment in progress (`ClosedOn` is NULL)
- **Closed** - Treatment completed (`ClosedOn` has a date)

**Status Change:**
- Set `ClosedOn` date to close episode
- Closed episodes still visible in history
- Cannot be deleted (soft delete only)

**Episode Lifecycle:**
```
Registration → Assessment → Fitting → Delivery → Follow-up → Closed
    ↓              ↓           ↓          ↓          ↓
  (Open)       (Open)      (Open)     (Open)    (Closed)
```

##### 2.6 Episode Documents ⭐ NEW FEATURE

**Separate from Patient Documents**

**Location:** `/Episodes/Details/{id}` → Documents Tab

**Document Types:**
- Assessment reports (PDF)
- X-rays and imaging (JPG, PNG)
- Fitting photographs (JPG, PNG)
- Delivery checklists (PDF, DOCX)
- Custom forms and notes

**Upload Specifications:**
- Max file size: 10 MB per file
- Multiple files supported (batch upload)
- Storage path: `{RootPath}/episodes/{EpisodeId}/{Year}/{Month}/`
- Metadata: Title, Upload Date, Uploaded By, Remarks, File Size

**Database Schema:**
```sql
CREATE TABLE EpisodeDocuments (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    EpisodeId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    StoragePath NVARCHAR(500) NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,
    FileSize BIGINT NOT NULL,
    TagsJson NVARCHAR(MAX),
    Remarks NVARCHAR(MAX),
    UploadedBy NVARCHAR(256) NOT NULL,
    UploadedAt DATETIME2 NOT NULL,
    FOREIGN KEY (EpisodeId) REFERENCES Episodes(Id)
);
```

##### 2.7 Episode History & Tracking

**View All Episodes:** `/Episodes/Index`

**Filters:**
- By Patient
- By Episode Type
- By Status (Open/Closed)
- By Date Range
- By Center/Location

**Episode Details View:**

**Sections Displayed:**
1. **Episode Information**
   - Episode type, dates, status
   - Type-specific details (prosthetic/orthotic/spinal)

2. **Assessments List**
   - All assessments chronologically
   - View findings, date, clinician

3. **Fittings List**
   - All fitting sessions
   - View notes, date, adjustments

4. **Delivery Information**
   - Delivery date, device details
   - Serial number, components

5. **Follow-ups List**
   - Scheduled and completed follow-ups
   - Next appointment alerts

6. **Repairs List**
   - Repair history
   - Category, date, details

7. **Documents Tab**
   - All episode-related documents
   - Upload, download, delete

---

### 3. Clinical Workflow Modules

#### 3.1 Assessment Module

**Purpose:** Document initial patient evaluation

**Location:**
- During episode creation (optional inline)
- Separate CRUD: `/Assessments/*`

**Fields:**
- **Episode** (Required) - Links to parent episode
- **Assessed On** (Required) - Date of assessment
- **Clinician ID** (Auto) - Logged-in clinician
- **Findings** (Required) - Detailed clinical notes
- **Attachments JSON** (Optional) - Array of attached files metadata
- **Remarks** (Optional) - Additional notes

**Example JSON Structure for Attachments:**
```json
{
  "attachments": [
    {
      "fileName": "assessment-measurements.pdf",
      "uploadDate": "2025-01-20T10:30:00Z",
      "uploadedBy": "Dr. Sarah Johnson"
    },
    {
      "fileName": "stump-photo.jpg",
      "uploadDate": "2025-01-20T10:35:00Z",
      "uploadedBy": "Dr. Sarah Johnson"
    }
  ]
}
```

**Multiple Assessments:**
- Episodes can have multiple assessments
- Re-assessments after adjustments
- Progress documentation

#### 3.2 Fitting Module

**Purpose:** Track device fitting sessions

**Location:**
- During episode creation (optional inline)
- Separate CRUD: `/Fittings/*`

**Fields:**
- **Episode** (Required)
- **Fitting Date** (Required)
- **Notes** (Required) - Fitting process details
- **Remarks** (Optional)

**Common Fitting Notes Include:**
- Socket fit and comfort
- Alignment corrections
- Suspension adjustments
- Cosmetic finishing
- Patient comfort level
- Gait observations
- Issues and resolutions

**Multiple Fittings:**
- Initial fitting
- Adjustment sessions (typically 2-3)
- Final fitting

#### 3.3 Delivery Module

**Purpose:** Record device handover

**Location:**
- During episode creation (optional inline)
- Separate CRUD: `/Deliveries/*`

**Fields:**
- **Episode** (Required)
- **Delivery Date** (Required)
- **Device ID** (Optional) - Links to device catalog
- **Serial Number** (Optional) - For device tracking
- **Components JSON** (Optional) - List of delivered components
- **Delivered By** (Required) - Staff member name
- **Remarks** (Optional)

**Components JSON Example:**
```json
{
  "components": [
    {
      "type": "Socket",
      "code": "PTB-001",
      "name": "PTB Socket Trans-tibial"
    },
    {
      "type": "Foot",
      "code": "SACH-002",
      "name": "SACH Foot Size 27cm"
    },
    {
      "type": "Pylon",
      "code": "PYL-AL-30",
      "name": "Aluminum Pylon 30cm"
    }
  ]
}
```

**Delivery Checklist:**
- Device inspection
- Patient education (care, donning/doffing)
- Warranty information
- Follow-up schedule
- Emergency contact

#### 3.4 Follow-up Module

**Purpose:** Schedule and track post-delivery appointments

**Location:** `/FollowUps/*`

**Fields:**
- **Episode** (Required)
- **Follow-up Date** (Required) - Date of visit
- **Action Taken** (Required) - What was done during visit
- **Next Appointment Date** (Optional) - Schedule next visit
- **Next Plan** (Optional) - Plan for next appointment

**Follow-up Types:**
- Initial follow-up (1 week post-delivery)
- Regular check-ups (1 month, 3 months, 6 months)
- Issue-based visits (discomfort, repairs)

**Dashboard Alert:**
- System shows "Pending Follow-ups" for next 7 days
- Helps clinicians stay on top of appointments

**Example Follow-up Entry:**
```
Follow-up Date: 2025-03-15
Action Taken:
- Inspected prosthetic socket for wear
- Minor adjustment to strap tension
- Patient walking 2km daily without pain
- Stump condition healthy, no skin issues
- Gait pattern improved, minimal limp

Next Appointment Date: 2025-06-15
Next Plan: 3-month routine check-up
```

#### 3.5 Repair Module

**Purpose:** Log device repairs and maintenance

**Location:** `/Repairs/*`

**Fields:**
- **Episode** (Required)
- **Repair Date** (Required)
- **Category** (Required):
  - Foot Repair
  - Socket Repair
  - Liner Repair
  - Brace Repair
  - Joint Repair
  - Other
- **Details** (Required) - Description of repair
- **Remarks** (Optional)

**Repair Tracking Benefits:**
- Warranty management
- Quality assurance
- Device lifecycle analysis
- Common issue identification

**Example Repair Entry:**
```
Repair Date: 2025-05-20
Category: Foot Repair
Details: SACH foot heel cushion replacement due to wear.
         Normal wear after 4 months of daily use.
         Replaced with new heel cushion (Part #HC-001).
         Patient advised on proper shoe usage.
Remarks: Under warranty, no charge to patient
```

---

### 4. Multi-Location Management

#### Overview
POMS supports operations across multiple centers in different countries with centralized data management.

#### 4.1 Location Hierarchy

**Structure:**
```
Province
  └── District
       └── Center
```

**Example Setup:**
```
Sri Lanka (Province)
  └── Western Province (Province)
       └── Gampaha District (District)
            └── Ragama Center (Center)

Philippines (Province)
  └── Metro Manila (Province)
       └── Quezon City (District)
            └── Aurora Boulevard Clinic (Center)

Cambodia (Province)
  └── Phnom Penh (Province)
       └── Mean Chey (District)
            └── Stung Mean Chey Clinic (Center)
```

#### 4.2 Location-Based Patient Numbering

**Format:** `{CenterCode}-{Year}-{SequenceNumber}`

**Configuration:**
```sql
INSERT INTO Centers (Name, Code, DistrictId) VALUES
('Ragama Center', 'SL-RGM', 1),
('Aurora Boulevard Clinic', 'PH-QC', 2),
('Stung Mean Chey Clinic', 'KH-PP', 3);
```

**Auto-Generation Logic:**
```csharp
public async Task<string> GeneratePatientNumberAsync(int centerId, DateOnly registrationDate)
{
    var center = await _context.Centers.FindAsync(centerId);
    var year = registrationDate.Year;

    var series = await _context.NumberSeries
        .FirstOrDefaultAsync(s => s.CenterId == centerId && s.Year == year);

    if (series == null)
    {
        series = new NumberSeries { CenterId = centerId, Year = year, LastNumber = 0 };
        _context.NumberSeries.Add(series);
    }

    series.LastNumber++;
    await _context.SaveChangesAsync();

    return $"{center.Code}-{year}-{series.LastNumber:D4}";
}
```

**Example Generated Numbers:**
- `SL-RGM-2025-0001` - First patient in Ragama for 2025
- `SL-RGM-2025-0150` - 150th patient in Ragama for 2025
- `PH-QC-2025-0001` - First patient in Quezon City for 2025

#### 4.3 Location-Based Filtering

**All Reports Support Location Filtering:**
- Filter by Province
- Filter by District
- Filter by Center

**Dashboard Metrics by Location:**
```csharp
var totalPatientsByCenter = await _context.Patients
    .Where(p => p.CenterId == centerId)
    .CountAsync();

var activeEpisodesByCenter = await _context.Episodes
    .Include(e => e.Patient)
    .Where(e => e.Patient.CenterId == centerId && !e.ClosedOn.HasValue)
    .CountAsync();
```

#### 4.4 Multi-Location Reporting

**Centralized Reports:**
- View metrics across all locations
- Compare performance between centers
- Identify location-specific trends

**Example: Deliveries by Location**
```sql
SELECT
    c.Name AS Center,
    COUNT(d.Id) AS TotalDeliveries,
    COUNT(CASE WHEN MONTH(d.DeliveryDate) = @CurrentMonth THEN 1 END) AS ThisMonth
FROM Deliveries d
INNER JOIN Episodes e ON d.EpisodeId = e.Id
INNER JOIN Patients p ON e.PatientId = p.Id
INNER JOIN Centers c ON p.CenterId = c.Id
GROUP BY c.Name
ORDER BY TotalDeliveries DESC;
```

#### 4.5 Regional Admin Access (Future Enhancement)

**Planned Feature:**
- Location-specific admin roles
- Data isolation by location (optional)
- Regional performance dashboards

---

### 5. Document Management

#### 5.1 Patient Documents

**Purpose:** Store general patient files (ID cards, medical reports, insurance)

**Location:** `/Patients/Details/{id}` → Documents Tab

**Features:**
- Single or multiple file upload
- Download with secure token
- Soft delete (data retained)
- Search and filter by title/date

**Supported File Types:**
- PDF Documents
- Images (JPG, JPEG, PNG)
- Word Documents (DOCX)

**Storage Path:**
```
{RootPath}/patients/{PatientNumber}/{Year}/{Month}/{FileName}
```

**Example:**
```
C:\PomsStorage\patients\SL-RGM-2025-0001\2025\01\national-id-card.pdf
```

**Database Record:**
```sql
INSERT INTO PatientDocuments (
    PatientId,
    Title,
    FileName,
    StoragePath,
    ContentType,
    UploadedBy,
    UploadedAt
) VALUES (
    'patient-guid',
    'National ID Card',
    'national-id-card.pdf',
    'patients/SL-RGM-2025-0001/2025/01/national-id-card.pdf',
    'application/pdf',
    'admin@poms.com',
    GETDATE()
);
```

#### 5.2 Episode Documents ⭐ NEW

**Purpose:** Store episode-specific files (assessments, X-rays, fitting photos)

**Location:** `/Episodes/Details/{id}` → Documents Tab

**Features:**
- Multiple file batch upload
- Episode-specific organization
- File size tracking
- Metadata tagging (TagsJson)

**Storage Path:**
```
{RootPath}/episodes/{EpisodeId}/{Year}/{Month}/{FileName}
```

**Example:**
```
C:\PomsStorage\episodes\abc123-guid\2025\02\assessment-report.pdf
C:\PomsStorage\episodes\abc123-guid\2025\02\xray-left-leg.jpg
C:\PomsStorage\episodes\abc123-guid\2025\03\fitting-photo-1.jpg
```

**Upload UI Features:**
- Drag-and-drop support
- Progress bar for each file
- Preview for images
- Batch operations (select multiple)

#### 5.3 File Storage Service

**Implementation:** `Poms.Infrastructure.Services.FileStorageService`

**Interface:**
```csharp
public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string subPath);
    Task<(Stream fileStream, string contentType)> GetFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
}
```

**Security Features:**
- File type validation (whitelist only)
- File size limits enforced
- Virus scanning integration point (future)
- Access control via authentication

**Configuration:**
```json
"FileStorage": {
    "RootPath": "C:\\PomsStorage",
    "MaxFileSizeMB": 10,
    "AllowedExtensions": [".pdf", ".jpg", ".jpeg", ".png", ".docx"]
}
```

---

### 6. Reporting & Analytics

#### 6.1 Dashboard (Home Page)

**Location:** `/Home/Index`

**Real-Time Metrics:**
1. **Total Patients** - All-time patient count
2. **Active Episodes** - Currently open treatment episodes
3. **Deliveries This Month** - Devices delivered in current month
4. **Pending Follow-ups** - Appointments in next 7 days

**SQL Queries:**
```sql
-- Total Patients
SELECT COUNT(*) FROM Patients WHERE IsDeleted = 0;

-- Active Episodes
SELECT COUNT(*) FROM Episodes WHERE ClosedOn IS NULL AND IsDeleted = 0;

-- Deliveries This Month
SELECT COUNT(*) FROM Deliveries
WHERE MONTH(DeliveryDate) = MONTH(GETDATE())
  AND YEAR(DeliveryDate) = YEAR(GETDATE());

-- Pending Follow-ups (Next 7 Days)
SELECT COUNT(*) FROM FollowUps
WHERE NextAppointmentDate BETWEEN GETDATE() AND DATEADD(DAY, 7, GETDATE());
```

**Charts & Visualizations:**
- Episode types distribution (Pie chart)
- Monthly deliveries trend (Line chart)
- Patients by location (Bar chart)

#### 6.2 Patient Reports

##### Report 1: Patient Registration Report

**Filters:**
- Date Range (From/To)
- Center
- District
- Province

**Columns:**
- Patient Number
- Name
- DOB / Age
- Sex
- Registration Date
- Center
- Guardian Contact

**Export:** PDF, Excel

##### Report 2: Patient Demographics Report

**Metrics:**
- Age distribution (grouped: 0-10, 11-20, 21-30, etc.)
- Gender breakdown
- Location distribution
- Guardian relationship types

**Visualizations:**
- Age pyramid
- Gender pie chart
- Location heat map

##### Report 3: Guardian Contact List

**Purpose:** Emergency contact reference

**Columns:**
- Patient Number
- Patient Name
- Guardian Name
- Guardian Relationship
- Guardian Phone 1
- Guardian Phone 2

**Use Cases:**
- Emergency situations
- Follow-up reminders
- Community outreach

#### 6.3 Episode Reports

##### Report 1: Episode Summary Report

**Filters:**
- Episode Type (Prosthetic/Orthotic/Spinal)
- Status (Open/Closed)
- Center
- Date Range

**Columns:**
- Episode ID
- Patient Number
- Patient Name
- Type
- Opened Date
- Closed Date
- Duration (days)
- Status

**Aggregations:**
- Total episodes by type
- Average treatment duration
- Open vs Closed ratio

##### Report 2: Episode Duration Report

**Purpose:** Identify bottlenecks in treatment process

**Metrics:**
- Average time from assessment to fitting
- Average time from fitting to delivery
- Total episode duration
- Longest open episodes

**Example Output:**
```
Average Treatment Times:
- Assessment to Fitting: 14 days
- Fitting to Delivery: 21 days
- Total Episode Duration: 42 days

Longest Open Episodes:
1. EP-001 - 180 days (Patient awaiting custom component)
2. EP-023 - 95 days (Multiple fitting adjustments)
```

##### Report 3: Device Usage Report

**Purpose:** Track most common devices and components

**Metrics:**
- Top 10 devices delivered
- Device type distribution
- Component usage frequency
- Device by location

**Example:**
```
Most Delivered Devices (2025):
1. SACH Foot (Size 26cm) - 42 deliveries
2. PTB Socket Trans-tibial - 38 deliveries
3. AFO Articulated - 29 deliveries
4. KAFO Custom - 15 deliveries
```

#### 6.4 Clinical Reports

##### Report 1: Assessment Report

**Columns:**
- Episode ID
- Patient Name
- Assessed On
- Assessed By (Clinician)
- Findings Summary (truncated)

**Filters:**
- Date Range
- Clinician
- Center

##### Report 2: Fitting Report

**Metrics:**
- Total fittings performed
- Average fittings per episode
- Fitting success rate (delivered within X fittings)
- Most common adjustments

##### Report 3: Delivery Report

**Columns:**
- Delivery Date
- Patient Number
- Patient Name
- Episode Type
- Device Delivered
- Serial Number
- Delivered By
- Center

**Aggregations:**
- Deliveries by month
- Deliveries by center
- Deliveries by device type

**Chart:** Monthly delivery trend

##### Report 4: Follow-up Compliance Report

**Purpose:** Track patient adherence to follow-up schedule

**Metrics:**
- Scheduled follow-ups
- Completed follow-ups
- Missed appointments
- Compliance rate (%)

**Example:**
```
Follow-up Compliance (Q1 2025):
- Scheduled: 120 appointments
- Completed: 108 appointments
- Missed: 12 appointments
- Compliance Rate: 90%
```

#### 6.5 Financial Reports (Ready for Integration)

**Note:** Financial tracking is prepared but not fully implemented.

**Planned Reports:**
1. Revenue by Episode Type
2. Revenue by Center
3. Device Cost Tracking
4. Insurance Claims Report

**Database Preparation:**
```sql
-- Future fields in Delivery table
ALTER TABLE Deliveries ADD Cost DECIMAL(10,2);
ALTER TABLE Deliveries ADD Revenue DECIMAL(10,2);
ALTER TABLE Deliveries ADD InsuranceClaimNumber NVARCHAR(50);
```

#### 6.6 Audit Reports

##### Report 1: User Activity Log

**Columns:**
- Timestamp
- User
- Action (CREATE/UPDATE/DELETE)
- Entity Type
- Entity ID
- Changes (JSON diff)

**Filters:**
- Date Range
- User
- Action Type
- Entity Type

##### Report 2: Data Modification History

**Purpose:** Track all changes to critical data

**Example Entry:**
```
Date: 2025-03-15 14:30:00
User: clinician@poms.com
Action: UPDATE
Entity: Patient (SL-RGM-2025-0001)
Changes:
  Phone1: +94711234567 → +94777654321
  UpdatedBy: NULL → clinician@poms.com
  UpdatedAt: 2025-03-10 → 2025-03-15
```

##### Report 3: Deleted Records Report

**Purpose:** View soft-deleted records for recovery

**Columns:**
- Entity Type
- Entity ID
- Original Data (JSON)
- Deleted By
- Deleted At
- Reason (if provided)

**Recovery Option:**
- Admin can restore soft-deleted records

##### Report 4: Login/Logout History

**Purpose:** Security auditing

**Tracked Events:**
- Successful logins
- Failed login attempts
- Logout events
- Session timeouts

**Alert Triggers:**
- 5+ failed login attempts from same IP
- Login from new location
- After-hours access

#### 6.7 Export Options

**PDF Export:**
- Professional formatting
- Company logo header
- Page numbers and timestamps
- Print-optimized layout

**Excel Export:**
- Structured data in tables
- Formulas for aggregations
- Charts included
- Filterable columns

**Implementation:**
```csharp
// PDF Export using Rotativa
public ActionResult ExportPatientReportPdf(DateTime fromDate, DateTime toDate)
{
    var patients = _context.Patients
        .Where(p => p.RegistrationDate >= DateOnly.FromDateTime(fromDate)
                 && p.RegistrationDate <= DateOnly.FromDateTime(toDate))
        .ToList();

    return new ViewAsPdf("PatientReportPdf", patients)
    {
        FileName = $"PatientReport_{DateTime.Now:yyyyMMdd}.pdf"
    };
}

// Excel Export using EPPlus
public ActionResult ExportPatientReportExcel(DateTime fromDate, DateTime toDate)
{
    var patients = _context.Patients
        .Where(p => p.RegistrationDate >= DateOnly.FromDateTime(fromDate)
                 && p.RegistrationDate <= DateOnly.FromDateTime(toDate))
        .ToList();

    using var package = new ExcelPackage();
    var worksheet = package.Workbook.Worksheets.Add("Patients");

    // Headers
    worksheet.Cells[1, 1].Value = "Patient Number";
    worksheet.Cells[1, 2].Value = "Name";
    // ... more columns

    // Data
    int row = 2;
    foreach (var patient in patients)
    {
        worksheet.Cells[row, 1].Value = patient.PatientNumber;
        worksheet.Cells[row, 2].Value = $"{patient.FirstName} {patient.LastName}";
        // ... more columns
        row++;
    }

    return File(package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"PatientReport_{DateTime.Now:yyyyMMdd}.xlsx");
}
```

---

### 7. Security & Audit Features

#### 7.1 Authentication

**Technology:** ASP.NET Core Identity

**Features:**
- Secure password hashing (PBKDF2)
- Account lockout (5 failed attempts)
- Password reset via email
- Email confirmation (optional)
- Two-factor authentication (future)

**Password Policy:**
```csharp
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequiredLength = 8;
```

**Session Management:**
- Sliding expiration (30 minutes inactivity)
- Secure cookies (HttpOnly, Secure flags)
- Anti-forgery tokens on all forms

#### 7.2 Authorization

**Implementation:** Policy-based authorization

**Policies Defined:**
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",
        policy => policy.RequireRole("ADMIN"));

    options.AddPolicy("ClinicianOrAdmin",
        policy => policy.RequireRole("CLINICIAN", "ADMIN"));

    options.AddPolicy("DataEntry",
        policy => policy.RequireRole("DATA_ENTRY", "ADMIN"));

    options.AddPolicy("AnyAuthenticatedUser",
        policy => policy.RequireAuthenticatedUser());
});
```

**Usage in Controllers:**
```csharp
[Authorize(Policy = "AdminOnly")]
public class UsersController : Controller
{
    // Only admins can access
}

[Authorize(Policy = "ClinicianOrAdmin")]
public async Task<IActionResult> CreateAssessment(Guid episodeId)
{
    // Clinicians and admins can create assessments
}
```

#### 7.3 Audit Logging

**Automatic Audit Trail:**

All entities inherit from `BaseEntity`:
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

**SaveChanges Override:**
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var entries = ChangeTracker.Entries<BaseEntity>();
    var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

    foreach (var entry in entries)
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedAt = DateTime.UtcNow;
            entry.Entity.CreatedBy = currentUser;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
            entry.Entity.UpdatedBy = currentUser;
        }
    }

    return await base.SaveChangesAsync(cancellationToken);
}
```

**Audit Log Table:**
```sql
CREATE TABLE AuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId NVARCHAR(256),
    UserName NVARCHAR(256),
    Action NVARCHAR(50), -- CREATE, UPDATE, DELETE, LOGIN, LOGOUT
    EntityType NVARCHAR(100),
    EntityId NVARCHAR(128),
    Timestamp DATETIME2,
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    OldValues NVARCHAR(MAX), -- JSON
    NewValues NVARCHAR(MAX), -- JSON
    AffectedColumns NVARCHAR(MAX) -- Comma-separated
);
```

#### 7.4 Data Encryption

**At Rest:**
- Database: Transparent Data Encryption (TDE) - SQL Server feature
- Files: File system encryption (BitLocker/LUKS)

**In Transit:**
- HTTPS enforced for all connections
- SSL/TLS 1.2+ required
- Secure WebSocket connections

**Connection String Encryption:**
```xml
<configuration>
  <connectionStrings configProtectionProvider="DataProtectionConfigurationProvider">
    <EncryptedData>
      <!-- Encrypted connection string -->
    </EncryptedData>
  </connectionStrings>
</configuration>
```

#### 7.5 Soft Delete

**Implementation:**
```csharp
public async Task<IActionResult> DeletePatient(Guid id)
{
    var patient = await _context.Patients.FindAsync(id);
    if (patient != null)
    {
        patient.IsDeleted = true;
        patient.DeletedAt = DateTime.UtcNow;
        patient.DeletedBy = User.Identity?.Name;

        _context.Update(patient);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Patient {PatientNumber} soft deleted by {User}",
                          patient.PatientNumber, User.Identity?.Name);
    }

    return RedirectToAction(nameof(Index));
}
```

**Query Filter:**
```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    // Global query filter: Exclude soft-deleted records
    builder.Entity<Patient>().HasQueryFilter(p => !p.IsDeleted);
    builder.Entity<Episode>().HasQueryFilter(e => !e.IsDeleted);
    // ... repeat for all entities
}
```

**Recovery:**
```csharp
// Admin can view deleted records
var deletedPatients = _context.Patients
    .IgnoreQueryFilters()
    .Where(p => p.IsDeleted)
    .ToList();

// Restore deleted record
patient.IsDeleted = false;
patient.DeletedAt = null;
patient.DeletedBy = null;
await _context.SaveChangesAsync();
```

#### 7.6 Input Validation

**Server-Side Validation:**
```csharp
[Required(ErrorMessage = "First name is required")]
[StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
public string FirstName { get; set; }

[Required(ErrorMessage = "Guardian name is required")]
[StringLength(100, ErrorMessage = "Guardian name cannot exceed 100 characters")]
public string GuardianName { get; set; }

[Required(ErrorMessage = "Guardian phone is required")]
[Phone(ErrorMessage = "Invalid phone number format")]
public string GuardianPhone1 { get; set; }
```

**Client-Side Validation:**
- jQuery Validation Unobtrusive
- Real-time feedback on form fields
- Custom validation rules

**Anti-XSS:**
```csharp
// Automatic HTML encoding in Razor views
@Model.PatientName  // Encoded automatically

// Raw HTML (use with caution)
@Html.Raw(Model.RichTextContent)
```

**SQL Injection Prevention:**
- Entity Framework parameterized queries
- No raw SQL concatenation
- Stored procedures with parameters

#### 7.7 File Upload Security

**Validation:**
```csharp
public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string subPath)
{
    // 1. File size check
    if (fileStream.Length > _maxFileSizeBytes)
        throw new InvalidOperationException($"File size exceeds {_maxFileSizeMB}MB limit");

    // 2. File extension whitelist
    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    if (!_allowedExtensions.Contains(extension))
        throw new InvalidOperationException($"File type {extension} not allowed");

    // 3. Content type validation
    var contentType = GetContentType(fileName);
    if (string.IsNullOrEmpty(contentType))
        throw new InvalidOperationException("Unknown content type");

    // 4. Sanitize filename
    var safeFileName = Path.GetFileNameWithoutExtension(fileName);
    safeFileName = Regex.Replace(safeFileName, @"[^a-zA-Z0-9_-]", "_");
    var uniqueFileName = $"{safeFileName}_{Guid.NewGuid()}{extension}";

    // 5. Save to isolated directory
    var fullPath = Path.Combine(_rootPath, subPath, uniqueFileName);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

    using var fileStreamOut = new FileStream(fullPath, FileMode.Create);
    await fileStream.CopyToAsync(fileStreamOut);

    return Path.Combine(subPath, uniqueFileName);
}
```

**Download Security:**
```csharp
public async Task<IActionResult> DownloadDocument(Guid id)
{
    var document = await _context.PatientDocuments.FindAsync(id);
    if (document == null)
        return NotFound();

    // Authorization check
    var patient = await _context.Patients.FindAsync(document.PatientId);
    if (!User.IsInRole("ADMIN") && patient.CenterId != CurrentUserCenterId)
        return Forbid();

    // Retrieve file securely
    var (fileStream, contentType) = await _fileStorageService.GetFileAsync(document.StoragePath);

    return File(fileStream, contentType, document.FileName);
}
```

---

## 🗄️ Database Schema

### Entity Relationship Diagram (ERD)

```
Patients (1) ───< (M) Episodes
    │                   │
    │                   ├───< Assessments
    │                   ├───< Fittings
    │                   ├───< Deliveries
    │                   ├───< FollowUps
    │                   ├───< Repairs
    │                   └───< EpisodeDocuments (NEW)
    │
    ├───< PatientDocuments
    ├───< PatientConditions ───> Conditions
    │
    └───> Centers ───> Districts ───> Provinces

Episodes ───> ProstheticEpisodes (1:1)
Episodes ───> OrthoticEpisodes (1:1)
Episodes ───> SpinalEpisodes (1:1)

Deliveries ───> DeviceCatalogs ───> DeviceTypes
DeviceCatalogs ───> ComponentCatalogs
```

### Table Schemas

#### Core Tables

**Patients**
```sql
CREATE TABLE Patients (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PatientNumber NVARCHAR(50) UNIQUE NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100),
    Dob DATE,
    Sex NVARCHAR(10) NOT NULL,
    NationalId NVARCHAR(50),
    Address1 NVARCHAR(255) NOT NULL,
    Address2 NVARCHAR(255),
    ProvinceId INT NOT NULL,
    DistrictId INT NOT NULL,
    Phone1 NVARCHAR(20),
    Phone2 NVARCHAR(20),
    Email NVARCHAR(255),
    CenterId INT NOT NULL,
    RegistrationDate DATE NOT NULL,
    ReferredBy NVARCHAR(255),
    Remarks NVARCHAR(MAX) NOT NULL,
    GuardianName NVARCHAR(100) NOT NULL,
    GuardianRelationship NVARCHAR(50),
    GuardianAddress NVARCHAR(255),
    GuardianPhone1 NVARCHAR(20) NOT NULL,
    GuardianPhone2 NVARCHAR(20),
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(256),
    UpdatedAt DATETIME2,
    UpdatedBy NVARCHAR(256),
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2,
    DeletedBy NVARCHAR(256),
    FOREIGN KEY (ProvinceId) REFERENCES Provinces(Id),
    FOREIGN KEY (DistrictId) REFERENCES Districts(Id),
    FOREIGN KEY (CenterId) REFERENCES Centers(Id)
);

CREATE INDEX IX_Patients_PatientNumber ON Patients(PatientNumber);
CREATE INDEX IX_Patients_CenterId ON Patients(CenterId);
CREATE INDEX IX_Patients_RegistrationDate ON Patients(RegistrationDate);
```

**Episodes**
```sql
CREATE TABLE Episodes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PatientId UNIQUEIDENTIFIER NOT NULL,
    Type NVARCHAR(20) NOT NULL, -- Prosthetic, Orthotic, SpinalOrthosis
    OpenedOn DATE NOT NULL,
    ClosedOn DATE,
    Remarks NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(256),
    UpdatedAt DATETIME2,
    UpdatedBy NVARCHAR(256),
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2,
    DeletedBy NVARCHAR(256),
    FOREIGN KEY (PatientId) REFERENCES Patients(Id)
);

CREATE INDEX IX_Episodes_PatientId ON Episodes(PatientId);
CREATE INDEX IX_Episodes_Type ON Episodes(Type);
CREATE INDEX IX_Episodes_OpenedOn ON Episodes(OpenedOn);
```

**ProstheticEpisodes**
```sql
CREATE TABLE ProstheticEpisodes (
    EpisodeId UNIQUEIDENTIFIER PRIMARY KEY,
    AmputationType NVARCHAR(50) NOT NULL,
    Level NVARCHAR(100) NOT NULL,
    Side NVARCHAR(20) NOT NULL,
    Reason NVARCHAR(50) NOT NULL,
    DateOfAmputation DATE,
    ReasonOther NVARCHAR(255),
    DesiredDeviceId INT,
    SelectedComponentsJson NVARCHAR(MAX),
    Remarks NVARCHAR(MAX),
    FOREIGN KEY (EpisodeId) REFERENCES Episodes(Id) ON DELETE CASCADE,
    FOREIGN KEY (DesiredDeviceId) REFERENCES DeviceCatalogs(Id)
);
```

**OrthoticEpisodes**
```sql
CREATE TABLE OrthoticEpisodes (
    EpisodeId UNIQUEIDENTIFIER PRIMARY KEY,
    MainProblem NVARCHAR(255) NOT NULL,
    BodyRegion NVARCHAR(20) NOT NULL,
    Side NVARCHAR(20) NOT NULL,
    OrthosisTypeId INT,
    ReasonForProblem NVARCHAR(255) NOT NULL,
    ReasonOther NVARCHAR(255),
    Remarks NVARCHAR(MAX),
    FOREIGN KEY (EpisodeId) REFERENCES Episodes(Id) ON DELETE CASCADE,
    FOREIGN KEY (OrthosisTypeId) REFERENCES DeviceCatalogs(Id)
);
```

**SpinalEpisodes**
```sql
CREATE TABLE SpinalEpisodes (
    EpisodeId UNIQUEIDENTIFIER PRIMARY KEY,
    PathologicalCondition NVARCHAR(255) NOT NULL,
    OrthoticDesign NVARCHAR(255) NOT NULL,
    Remarks NVARCHAR(MAX),
    FOREIGN KEY (EpisodeId) REFERENCES Episodes(Id) ON DELETE CASCADE
);
```

#### Clinical Workflow Tables

**Assessments, Fittings, Deliveries, FollowUps, Repairs** - See code for full schemas

#### Document Tables

**PatientDocuments**
```sql
CREATE TABLE PatientDocuments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PatientId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    StoragePath NVARCHAR(500) NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,
    TagsJson NVARCHAR(MAX),
    Remarks NVARCHAR(MAX),
    UploadedBy NVARCHAR(256) NOT NULL,
    UploadedAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(256),
    UpdatedAt DATETIME2,
    UpdatedBy NVARCHAR(256),
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2,
    DeletedBy NVARCHAR(256),
    FOREIGN KEY (PatientId) REFERENCES Patients(Id)
);
```

**EpisodeDocuments** ⭐ NEW
```sql
CREATE TABLE EpisodeDocuments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EpisodeId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    StoragePath NVARCHAR(500) NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,
    FileSize BIGINT NOT NULL,
    TagsJson NVARCHAR(MAX),
    Remarks NVARCHAR(MAX),
    UploadedBy NVARCHAR(256) NOT NULL,
    UploadedAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    CreatedBy NVARCHAR(256),
    UpdatedAt DATETIME2,
    UpdatedBy NVARCHAR(256),
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2,
    DeletedBy NVARCHAR(256),
    FOREIGN KEY (EpisodeId) REFERENCES Episodes(Id)
);
```

### Database Migrations

**List Migrations:**
```bash
dotnet ef migrations list
```

**Create Migration:**
```bash
dotnet ef migrations add MigrationName --project ../Poms.Infrastructure
```

**Apply Migration:**
```bash
dotnet ef database update
```

**Rollback:**
```bash
dotnet ef database update PreviousMigrationName
```

---

## 🔌 API Documentation

### RESTful Endpoints

**Note:** POMS currently uses MVC pattern (server-side rendering). REST API endpoints are planned for future mobile app integration.

**Planned API Structure:**
```
/api/v1/patients
  GET    /api/v1/patients              - List patients
  GET    /api/v1/patients/{id}         - Get patient details
  POST   /api/v1/patients              - Create patient
  PUT    /api/v1/patients/{id}         - Update patient
  DELETE /api/v1/patients/{id}         - Soft delete patient

/api/v1/episodes
  GET    /api/v1/episodes              - List episodes
  GET    /api/v1/episodes/{id}         - Get episode details
  POST   /api/v1/episodes              - Create episode
  PUT    /api/v1/episodes/{id}         - Update episode
  POST   /api/v1/episodes/{id}/close   - Close episode

/api/v1/documents
  POST   /api/v1/documents/upload      - Upload document
  GET    /api/v1/documents/{id}        - Download document
  DELETE /api/v1/documents/{id}        - Delete document
```

### AJAX Endpoints (Currently Implemented)

**Get Districts by Province:**
```javascript
GET /Patients/GetDistrictsByProvince?provinceId={id}

Response:
[
  { "id": 1, "name": "Gampaha" },
  { "id": 2, "name": "Colombo" }
]
```

**Get Centers by District:**
```javascript
GET /Patients/GetCentersByDistrict?districtId={id}

Response:
[
  { "id": 1, "name": "Ragama Center" },
  { "id": 2, "name": "Negombo Clinic" }
]
```

---

## 💻 Development

### Setting Up Development Environment

1. **Clone the repository**
2. **Open solution in Visual Studio 2022**
3. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```
4. **Set startup project:** `Poms.Web`
5. **Configure database** in `appsettings.json`
6. **Run migrations:**
   ```bash
   dotnet ef database update
   ```
7. **Run the application:** Press F5

### Project Structure

```
POMS/
├── src/
│   ├── Poms.Domain/              # Entities, Enums, Interfaces
│   │   ├── Common/               # BaseEntity
│   │   ├── Entities/             # All entity classes
│   │   └── Enums/                # Enumerations
│   ├── Poms.Application/         # Business Logic (future)
│   ├── Poms.Infrastructure/      # Data Access, Services
│   │   ├── Data/                 # DbContext, Migrations
│   │   └── Services/             # FileStorageService, etc.
│   ├── Poms.Web/                 # MVC Application
│   │   ├── Controllers/          # MVC Controllers
│   │   ├── Views/                # Razor Views
│   │   ├── ViewModels/           # View Models
│   │   ├── wwwroot/              # Static files
│   │   └── Program.cs            # Application entry point
│   └── Poms.Reporting/           # Reporting (future)
└── tests/
    └── Poms.Tests/               # Unit & Integration Tests
```

### Coding Standards

**Naming Conventions:**
- PascalCase for classes, methods, properties
- camelCase for local variables, parameters
- UPPER_CASE for constants
- Prefix interfaces with `I` (e.g., `IFileStorageService`)

**Code Organization:**
- One class per file
- File name matches class name
- Group related functionality in folders

**Comments:**
- XML documentation for public APIs
- Inline comments for complex logic
- TODO comments for future work

**Example:**
```csharp
/// <summary>
/// Generates a unique patient number based on center and registration date.
/// Format: {CenterCode}-{Year}-{SequenceNumber}
/// </summary>
/// <param name="centerId">The center where patient is registering</param>
/// <param name="registrationDate">Date of registration</param>
/// <returns>Generated patient number string</returns>
public async Task<string> GeneratePatientNumberAsync(int centerId, DateOnly registrationDate)
{
    // Implementation
}
```

### Running Tests

```bash
cd POMS/tests/Poms.Tests
dotnet test
```

**Test Coverage:**
- Unit tests for services
- Integration tests for controllers
- End-to-end tests for critical workflows

---

## 🚀 Deployment

### Production Deployment Checklist

- [ ] Change default passwords
- [ ] Update connection strings
- [ ] Configure HTTPS/SSL certificate
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Enable error logging (Serilog)
- [ ] Configure backup strategy
- [ ] Test database connectivity
- [ ] Verify file storage paths
- [ ] Run security scan
- [ ] Load test (optional)

### Windows Server Deployment

**Requirements:**
- Windows Server 2019 or later
- IIS 10 with ASP.NET Core Module
- .NET 8.0 Runtime (Hosting Bundle)
- SQL Server 2019+

**Steps:**
1. Install .NET 8.0 Hosting Bundle
2. Publish application:
   ```bash
   dotnet publish -c Release -o ./publish
   ```
3. Copy files to IIS webroot (e.g., `C:\inetpub\wwwroot\poms`)
4. Create IIS Application Pool (.NET CLR Version: No Managed Code)
5. Create IIS Website pointing to publish folder
6. Configure application settings
7. Test application

### Linux Deployment (Ubuntu)

**Requirements:**
- Ubuntu 20.04 LTS or later
- .NET 8.0 Runtime
- Nginx (reverse proxy)
- PostgreSQL 13+

**Steps:**
1. Install .NET 8.0 Runtime:
   ```bash
   wget https://dot.net/v1/dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 8.0 --runtime aspnetcore
   ```

2. Publish application:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

3. Copy to server:
   ```bash
   scp -r ./publish user@server:/var/www/poms
   ```

4. Configure systemd service (`/etc/systemd/system/poms.service`):
   ```ini
   [Unit]
   Description=POMS Application
   After=network.target

   [Service]
   WorkingDirectory=/var/www/poms
   ExecStart=/usr/bin/dotnet /var/www/poms/Poms.Web.dll
   Restart=always
   RestartSec=10
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

   [Install]
   WantedBy=multi-user.target
   ```

5. Enable and start service:
   ```bash
   sudo systemctl enable poms
   sudo systemctl start poms
   ```

6. Configure Nginx:
   ```nginx
   server {
       listen 80;
       server_name your-domain.com;

       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
       }
   }
   ```

### Docker Deployment

**Dockerfile included in repository**

**Build Image:**
```bash
docker build -t poms:1.0 .
```

**Run Container:**
```bash
docker run -d \
  -p 5000:5000 \
  -e DATABASE_URL="Server=db;Database=PomsDb;..." \
  -e UsePostgreSQL=false \
  -v /data/poms-storage:/app/PomsStorage \
  --name poms-app \
  poms:1.0
```

### Railway Deployment

**Configuration:** `railway.json` or `nixpacks.toml`

**Environment Variables:**
```bash
DATABASE_URL=postgresql://user:pass@host:5432/dbname
PORT=5000
ASPNETCORE_ENVIRONMENT=Production
```

**Deploy:**
```bash
railway up
```

---

## 🔧 Troubleshooting

### Common Issues

#### Issue: Database Connection Failed

**Symptoms:** Application throws `SqlException` or `NpgsqlException`

**Solutions:**
1. Verify connection string in `appsettings.json`
2. Check database server is running
3. Verify firewall allows connection
4. Test connection with SQL Server Management Studio / pgAdmin
5. Ensure database user has proper permissions

#### Issue: File Upload Fails

**Symptoms:** "File size exceeds limit" or "File type not allowed"

**Solutions:**
1. Check `FileStorage:MaxFileSizeMB` in `appsettings.json`
2. Verify file extension in `FileStorage:AllowedExtensions`
3. Ensure `RootPath` directory exists and is writable
4. Check disk space availability

#### Issue: Login Not Working

**Symptoms:** Incorrect credentials or account locked

**Solutions:**
1. Verify user exists in `AspNetUsers` table
2. Check if account is locked (`LockoutEnd` column)
3. Reset password via database:
   ```sql
   UPDATE AspNetUsers
   SET LockoutEnd = NULL, AccessFailedCount = 0
   WHERE UserName = 'username';
   ```
4. Use default seeded users (see User Roles section)

#### Issue: Reports Not Generating

**Symptoms:** Blank reports or PDF export fails

**Solutions:**
1. Check date range filters (ensure data exists)
2. Verify user has permission for report type
3. Check browser console for JavaScript errors
4. Test with smaller dataset

#### Issue: Migration Failed

**Symptoms:** `dotnet ef database update` fails

**Solutions:**
1. Check database server is accessible
2. Verify user has CREATE/ALTER permissions
3. Drop database and recreate:
   ```bash
   dotnet ef database drop --force
   dotnet ef database update
   ```
4. Check migration files for errors

### Logging

**Log Location:** `POMS/src/Poms.Web/logs/poms-{date}.txt`

**Log Levels:**
- **Information:** Normal operations
- **Warning:** Soft deletes, unusual events
- **Error:** Exceptions, failures
- **Critical:** Application crashes

**View Logs:**
```bash
tail -f logs/poms-20251203.txt
```

**Example Log Entry:**
```
2025-12-03 10:30:15 [INF] Patient SL-RGM-2025-0001 created by admin@poms.com
2025-12-03 10:31:42 [WRN] Patient SL-RGM-2025-0005 soft deleted by admin@poms.com
2025-12-03 10:35:10 [ERR] Error creating episode: SqlException...
```

### Performance Optimization

**Slow Queries:**
1. Add database indexes on frequently queried columns
2. Use `.AsNoTracking()` for read-only queries
3. Implement pagination for large datasets
4. Cache frequently accessed data

**Example Optimization:**
```csharp
// Before (slow)
var patients = await _context.Patients.ToListAsync();

// After (fast)
var patients = await _context.Patients
    .AsNoTracking()
    .Where(p => p.CenterId == centerId)
    .OrderByDescending(p => p.RegistrationDate)
    .Take(20)
    .ToListAsync();
```

### Support

**For Technical Support:**
- Email: support@neuralsedge.com
- GitHub Issues: [Create Issue](https://github.com/neuralsedge/poms/issues)
- Documentation: This README file

---

## 📄 License

**Proprietary Software**

Copyright © 2025 NeurAlsEdge. All rights reserved.

This software is licensed to Exceed Prosthetics & Orthotics under the terms specified in the Software License Agreement dated December 3, 2025.

**Restrictions:**
- Source code ownership transferred to Exceed Clinics upon final payment
- No redistribution without written permission
- Modifications allowed for internal use only
- Third-party libraries used under their respective licenses

**Third-Party Licenses:**
- ASP.NET Core - MIT License
- Entity Framework Core - MIT License
- Bootstrap - MIT License
- jQuery - MIT License
- Serilog - Apache 2.0 License

---

## 🙏 Acknowledgments

**Developed By:** Ranga Nanayakkara, NeurAlsEdge

**Client:** Exceed Prosthetics & Orthotics

**Technologies:** ASP.NET Core, Entity Framework Core, SQL Server, Bootstrap

**Special Thanks:**
- Exceed Clinics team for domain expertise and requirements
- Microsoft for excellent documentation
- Open-source community for libraries and tools

---

## 📞 Contact

**Developer:** Ranga Nanayakkara
**Company:** NeurAlsEdge
**Website:** www.neuralsedge.com
**Email:** ranga@neuralsedge.com

**For Support:**
- Technical Issues: support@neuralsedge.com
- Feature Requests: features@neuralsedge.com
- General Inquiries: info@neuralsedge.com

---

**Last Updated:** December 3, 2025
**Version:** 1.0.0
**Document Revision:** 1.0
