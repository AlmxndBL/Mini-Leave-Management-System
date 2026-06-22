# Mini Leave Management System

ระบบจัดการการลาของพนักงาน (Employee Leave Management System)

Portfolio project สำหรับสัมภาษณ์งาน Gofive — แสดงถึงความเข้าใจ full-stack development (Angular + .NET) พร้อม role-based access control, approval workflow, และ business logic

## 📋 Features

### Employee (พนักงาน)
- ✅ ดูวันลาคงเหลือแยกตามประเภท (ลาป่วย / ลากิจ / ลาพักร้อน)
- ✅ ยื่นใบลา (เลือกประเภท, ช่วงวันที่, เหตุผล)
- ✅ ดูประวัติคำขอของตัวเอง + สถานะ
- ✅ ยกเลิกคำขอที่ยัง Pending

### Manager (หัวหน้า)
- ✅ ดูคำขอลาของลูกทีม (เฉพาะแผนกที่ตนเป็น manager)
- ✅ อนุมัติ / ปฏิเสธ พร้อมใส่ comment
- ✅ ระบบตัดวันลาอัตโนมัติเมื่ออนุมัติ
- ✅ Dashboard สรุปจำนวนคำขอ Pending/อนุมัติ/ปฏิเสธ

### Admin
- ✅ จัดการประเภทการลา (เพิ่ม/แก้)
- ✅ สั่งสร้างโควต้าวันลาสำหรับปีใหม่

## 🛠 Tech Stack

| Layer | Technology |
|-------|------------|
| **Backend** | .NET 8 Web API (C#) |
| **Database** | SQLite (Entity Framework Core) |
| **Frontend** | Angular 18 + TypeScript |
| **Auth** | JWT (Bearer Token) |

## 📁 Project Structure

```
Mini-Leave-Management-System/
├── LeaveManagement.Api/          # Backend (.NET 8)
│   ├── Controllers/              # API endpoints
│   ├── Services/                 # Business logic
│   ├── Models/                   # Entities, DTOs
│   ├── Data/                     # DbContext, Migrations
│   └── Program.cs               # Configuration
├── web/                          # Frontend (Angular 18)
│   ├── src/app/
│   │   ├── components/          # Pages (Login, Employee, Manager, Admin)
│   │   ├── services/            # API communication
│   │   ├── models/              # Interfaces
│   │   ├── guards/              # Auth guard
│   │   └── interceptors/        # JWT interceptor
│   └── package.json
└── doc/                          # Detailed documentation
```

## 🚀 วิธีรัน

### Prerequisites
- .NET 8 SDK
- Node.js 24.x
- npm

### Backend Setup

```bash
cd LeaveManagement.Api

# Restore NuGet packages
dotnet restore

# Create database (SQLite)
dotnet ef database update

# Run the API
dotnet run
```

Backend จะรันที่ `http://localhost:5000`

Swagger API documentation: `http://localhost:5000/swagger`

### Frontend Setup

```bash
cd web

# Install dependencies
npm install

# Run development server
ng serve
```

Frontend จะรันที่ `http://localhost:4200`

## 🔐 Demo Accounts

| Role | Email | Password |
|------|-------|----------|
| **Employee** | employee@example.com | Employee@123 |
| **Manager** | manager@example.com | Manager@123 |
| **Admin** | admin@example.com | Admin@123 |

## 💡 Key Implementation Details

### Authentication & Authorization
- JWT token generation on login (60 minutes expiry)
- HTTP interceptor แนบ token ในทุก request
- Role-based access control (RBAC) with auth guard
- Approval rights based on `Departments.ManagerId`, not just Role

### Business Logic
- ✅ วันลาคำนวณตัดเสาร์-อาทิตย์ออก
- ✅ Re-check quota on approval (protect against concurrent requests)
- ✅ Database transaction ครอบ "check → approve → deduct days" เป็น atomic
- ✅ Auto-deduct leave balance when request is approved
- ✅ Cannot cancel approved/rejected requests

### Database Design
- Foreign keys with appropriate cascade behavior
- UNIQUE constraint on (UserId, LeaveTypeId, Year) ใน LeaveBalances
- Indexes on frequently queried fields (UserId, Status)

## 📸 Screenshots

### Login Page
ล็อกอินด้วย email/password พร้อม demo accounts

### Employee Dashboard
- วันลาคงเหลือแสดงเป็นการ์ด
- ฟอร์มยื่นใบลา (date picker, leave type selector)
- ประวัติการยื่นลาแสดงเป็นตาราง

### Manager Dashboard
- สรุปจำนวน Pending/Approved/Rejected
- รายการคำขอ Pending พร้อมปุ่มอนุมัติ/ปฏิเสธ
- Modal สำหรับใส่ comment ก่อนอนุมัติ/ปฏิเสธ

### Admin Dashboard
- ตารางประเภทการลา
- ฟอร์มเพิ่มประเภทการลาใหม่
- ปุ่มสร้างโควต้าสำหรับปีใหม่

## 🎯 Architecture Highlights

```
Angular SPA
    ↓ HTTP + JWT
API Controller Layer       ← รับ request, validate, return DTO
    ↓
Service Layer            ← business logic (ตัดวันลา, re-check quota)
    ↓
Repository/DbContext    ← EF Core query + transaction
    ↓
SQLite Database
```

**Separation of Concerns:**
- Controllers: request handling & validation
- Services: business logic & workflow
- Repository: data access
- Models: DTOs for API, Entities for DB

## 📝 API Endpoints

| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/login` | Public | Login & get JWT |
| GET | `/api/leave-balances/me` | Employee | My leave balances |
| POST | `/api/leave-requests` | Employee | Submit leave request |
| GET | `/api/leave-requests/me` | Employee | My requests history |
| PUT | `/api/leave-requests/{id}/cancel` | Employee | Cancel pending request |
| GET | `/api/leave-requests/pending` | Manager | Pending requests of team |
| PUT | `/api/leave-requests/{id}/approve` | Manager | Approve request |
| PUT | `/api/leave-requests/{id}/reject` | Manager | Reject request |
| GET | `/api/dashboard/summary` | Manager | Summary statistics |
| POST | `/api/leave-types` | Admin | Create leave type |
| POST | `/api/leave-balances/generate` | Admin | Generate year's balances |

## 🔍 Key Code Locations

- **Leave Request Approval Logic**: [LeaveManagement.Api/Services/LeaveRequestService.cs:87](LeaveManagement.Api/Services/LeaveRequestService.cs#L87) - database transaction implementation
- **Authorization Check**: [LeaveManagement.Api/Services/LeaveRequestService.cs:99](LeaveManagement.Api/Services/LeaveRequestService.cs#L99) - verify approver is department manager
- **Re-check Quota**: [LeaveManagement.Api/Services/LeaveRequestService.cs:108](LeaveManagement.Api/Services/LeaveRequestService.cs#L108) - prevent over-allocation
- **Employee Dashboard**: [web/src/app/components/employee/employee.component.ts](web/src/app/components/employee/employee.component.ts)
- **Manager Approval**: [web/src/app/components/manager/manager.component.ts](web/src/app/components/manager/manager.component.ts)

## 🎓 Learning Value

This project demonstrates:
- **Backend**: .NET layered architecture, EF Core with transactions, JWT auth, RBAC
- **Frontend**: Angular standalone components, services, route guards, HTTP interceptors
- **Database**: Relational design, foreign keys, unique constraints, indexes
- **API Design**: RESTful endpoints, DTOs, proper HTTP status codes
- **Business Logic**: Workflow automation, concurrent request handling, data integrity

## 📖 Documentation

See [`doc/`](doc/) folder for detailed documentation:
- [Architecture](doc/design/architecture.md)
- [Database Design](doc/design/database.md)
- [Security & Authorization](doc/design/security.md)
- [API Reference](doc/design/api-reference.md)
- Feature details for each role

## 🐛 Troubleshooting

### Backend won't start
- Verify .NET 8 SDK is installed: `dotnet --version`
- Check database: `dotnet ef database update`
- Check logs for errors

### Frontend won't compile
- Verify Node.js 24.x: `node --version`
- Clear node_modules: `rm -r node_modules && npm install`
- Check Angular CLI: `ng version`

### Cannot login
- Verify backend is running on port 5000
- Check browser console for CORS errors
- Verify demo account credentials

### Leave balance not updating
- Check backend logs for transaction errors
- Verify quota is sufficient before approving
- Check API response: `PUT /api/leave-requests/{id}/approve`

## 📌 Notes

- Database: SQLite for development (easily switchable to SQL Server)
- CORS: Allow all origins (configure in production)
- JWT: 60-minute expiry (no refresh token implemented)
- Date calculations: Business days only (weekends excluded)

## ©️ Author

Created as portfolio project for Gofive interview - February 2026

---

**ศรุตาฟ้อ** (StxrFxll) - Full Stack Developer
