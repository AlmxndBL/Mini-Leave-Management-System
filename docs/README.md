# เอกสารออกแบบระบบ — Mini Leave Management System

ชุดเอกสารนี้คือ **พิมพ์เขียว (design blueprint)** สำหรับระบบที่จะพัฒนาต่อยอดจาก Mini Leave Management System
เป็นเอกสารออกแบบล้วน ๆ — ยังไม่มีการเขียนโค้ดจริง — เพื่อให้ทีมเห็นภาพ data model, API, UI และ business rules ก่อนลงมือ implement

> ภาษา: ไทยเป็นหลัก คงศัพท์เทคนิคอังกฤษ (entity, endpoint, DTO ฯลฯ)

---

## สารบัญ (Index)

### ระบบหลัก (ตามที่ร้องขอ)
| เอกสาร | ระบบ | ผู้ใช้หลัก |
|--------|------|-----------|
| [member-management.md](member-management.md) | **ระบบจัดการสมาชิก** — admin จัดการผู้ใช้ (list/search, สร้าง, แก้ไข, เปิด-ปิดใช้งาน, รีเซ็ตรหัสผ่าน) | Admin |
| [profile-management.md](profile-management.md) | **ระบบจัดการข้อมูลส่วนตัว** — ผู้ใช้แก้ข้อมูลตัวเอง + เปลี่ยนรหัสผ่าน | ทุก role |

### ระบบแนะนำเพิ่มเติม
| เอกสาร | ระบบ | ผู้ใช้หลัก |
|--------|------|-----------|
| [department-management.md](department-management.md) | จัดการแผนก (CRUD + กำหนด manager) | Admin |
| [notifications.md](notifications.md) | แจ้งเตือนในแอป (in-app notification) | ทุก role |
| [audit-log.md](audit-log.md) | บันทึกกิจกรรม (audit trail) | Admin |
| [reports.md](reports.md) | รายงาน & สถิติการลา + export | Manager / Admin |

---

## ภาพรวมสถาปัตยกรรมปัจจุบัน (อ้างอิงร่วมทุกเอกสาร)

### Backend — `LeaveManagement.Api/` (ASP.NET Core 8 + EF Core/SQLite)

**ชั้นการทำงาน (layering):** `Controller → Service (interface) → AppDbContext (EF Core) → SQLite`

- **Entities:** `LeaveManagement.Api/Models/Entities/` — `User`, `Department`, `LeaveType`, `LeaveBalance`, `LeaveRequest`
- **DTOs:** `LeaveManagement.Api/Models/DTOs/` (request/response แยกจาก entity)
- **Services:** `LeaveManagement.Api/Services/` + `Services/Interfaces/` ลงทะเบียนแบบ `Scoped` ใน `Program.cs`
- **Auth:** JWT Bearer; claims = `userId`, `ClaimTypes.Email`, `ClaimTypes.Role`; รหัสผ่าน hash ด้วย **BCrypt.Net-Next**
- **Routing convention:** kebab-case ผ่าน `Infrastructure/SlugifyParameterTransformer.cs`
  เช่น `UsersController` → `/api/users`, `LeaveRequestsController` → `/api/leave-requests`
- **Constants:** `LeaveManagement.Api/Helpers/Constants.cs`

```csharp
// Helpers/Constants.cs (ของจริงในโปรเจกต์)
public static class UserRoles            { public const int Employee = 0, Manager = 1, Admin = 2; }
public static class LeaveRequestStatus   { public const int Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3; }
```

**Pattern การตรวจสิทธิ์ใน controller** (ทุก endpoint ใหม่ต้องทำตามนี้):

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]                              // ต้อง login เสมอ
public class XxxController : ControllerBase
{
    private int GetUserId()   => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    private int GetUserRole() => int.Parse(User.FindFirst(ClaimTypes.Role)?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> Foo()
    {
        if (GetUserRole() != UserRoles.Admin) return Forbid();   // 403 เมื่อ role ไม่ผ่าน
        // ...
    }
}
```

**HTTP status convention ที่ใช้อยู่:** `200 OK`, `201 Created`, `204 No Content`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`

### Frontend — `web/` (Angular 18 standalone)

- **Components:** `web/src/app/components/` — `login`, `employee`, `manager`, `admin` (standalone ทั้งหมด)
- **Services:** `web/src/app/services/`
  - `AuthService` — login, เก็บ token + current user ใน `localStorage`, `currentUser$` (BehaviorSubject)
  - `LeaveService` — เรียก API ทั้งหมด (base `http://localhost:5125/api`)
- **Models:** `web/src/app/models/auth.model.ts`, `leave.model.ts`
- **Guard:** `web/src/app/guards/auth.guard.ts` — ใช้ `route.data['roles']` ตรวจ role
- **Interceptor:** `web/src/app/interceptors/auth.interceptor.ts` — แนบ `Authorization: Bearer <token>`
  (ลงทะเบียนด้วย `provideHttpClient(withInterceptorsFromDi())` ใน `app.config.ts`)
- **Routing:** `web/src/app/app.routes.ts` — route แยกตาม role ผ่าน `data: { roles: [...] }`
- **i18n:** ข้อความ UI ทั้งหมดเป็นภาษาไทย

### Design Tokens — `web/src/styles.css`

UI ใหม่ทั้งหมด **ต้องใช้ CSS variables เหล่านี้** ห้าม hardcode สี/ระยะ/เงา

| กลุ่ม | ตัวแปร | ค่า / ใช้เมื่อ |
|------|--------|----------------|
| พื้นผิว | `--bg` `--surface` `--surface-muted` | พื้นหลังหน้า / การ์ด / header ตาราง, hover |
| เส้นขอบ | `--border` `--border-strong` | เส้นบาง / เส้นเข้ม (input) |
| ตัวอักษร | `--text` `--text-muted` `--text-faint` | หลัก / รอง / placeholder |
| แบรนด์ | `--primary` `--primary-hover` `--primary-soft` `--primary-ring` | ปุ่มหลัก / hover / พื้นอ่อน / focus ring |
| semantic | `--success(-soft)` `--warning(-soft)` `--danger(-soft)` `--neutral(-soft)` | อนุมัติ / รอ / ปฏิเสธ / ยกเลิก |
| radius | `--radius-sm` (8) `--radius` (12) `--radius-lg` (16) `--radius-pill` (999) | input / การ์ด / modal / badge |
| เงา | `--shadow-xs` `--shadow-sm` `--shadow-md` `--shadow-lg` | ความลึกตามลำดับ |
| motion | `--ease` = `cubic-bezier(0.4,0,0.2,1)` | ทุก transition |

**Pattern UI ที่ reuse ได้** (มีอยู่ในทุก component): `.navbar`, `.container`, `.section`, `.btn-primary/.btn-secondary`, `.form-group`, ตาราง (`thead` พื้น `--surface-muted`), status badge `.status-0..3`, `.modal-overlay`+`.modal` (มี keyframe `modal-in`), card grid (`balances-grid` / `summary-card`)

---

## โครงสร้างมาตรฐานของเอกสารแต่ละระบบ

ทุกไฟล์ระบบใช้หัวข้อชุดเดียวกัน 10 ข้อ:
1. ภาพรวม & วัตถุประสงค์ · 2. ขอบเขต & สิทธิ์ · 3. Data Model · 4. API Design · 5. Backend (Service Layer) ·
6. UI/UX Design · 7. Business Rules & Validation · 8. Flow · 9. Edge Cases & Security · 10. Out of Scope

> ในเอกสารจะระบุชัดว่าส่วนใด **(มีแล้ว)** ในโค้ดปัจจุบัน และส่วนใด **(ใหม่)** ที่ต้องเพิ่ม
