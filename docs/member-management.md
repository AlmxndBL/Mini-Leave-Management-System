# ระบบจัดการสมาชิก (Member / User Management)

> เอกสารออกแบบ — ระบบหลัก · ผู้ใช้: **Admin** · สถานะปัจจุบัน: มี API บางส่วน แต่ **ไม่มี UI** และยังขาด list/search, deactivate, reset password

---

## 1. ภาพรวม & วัตถุประสงค์

ปัจจุบัน admin สร้าง/แก้ผู้ใช้ได้ผ่าน API แต่ **ไม่มีหน้าจอ** และไม่มีวิธี:
- ดูรายชื่อผู้ใช้ทั้งหมด / ค้นหา / กรอง
- ปิดการใช้งานผู้ใช้ (offboarding) แทนการลบ
- รีเซ็ตรหัสผ่านให้ผู้ใช้ที่ลืมรหัส

ระบบนี้เติมช่องว่างดังกล่าว ให้ admin บริหารจัดการสมาชิกได้ครบวงจรจากหน้าเดียว โดย **ต่อยอดจาก `UsersController` เดิม** ไม่รื้อของเดิม

## 2. ขอบเขต & สิทธิ์ (Roles & Permissions)

| การกระทำ | Employee (0) | Manager (1) | Admin (2) |
|----------|:---:|:---:|:---:|
| ดูรายชื่อผู้ใช้ / ค้นหา | ✗ | ✗ | ✓ |
| สร้าง / แก้ไขผู้ใช้ | ✗ | ✗ | ✓ |
| เปิด-ปิดการใช้งาน (activate/deactivate) | ✗ | ✗ | ✓ |
| รีเซ็ตรหัสผ่านผู้ใช้ | ✗ | ✗ | ✓ |

ทุก endpoint บังคับ `role == UserRoles.Admin` ด้วย pattern `GetUserRole() != UserRoles.Admin → Forbid()`

## 3. Data Model

ใช้ entity `User` เดิม (`LeaveManagement.Api/Models/Entities/User.cs`) — **ไม่ต้องเพิ่ม field**

| Field | ชนิด | หมายเหตุ |
|-------|------|----------|
| `UserId` | int (PK) | |
| `Email` | string (required, unique) | identifier สำหรับ login |
| `PasswordHash` | string (required) | BCrypt — ไม่ส่งออก API เด็ดขาด |
| `FirstName`, `LastName` | string (required) | |
| `Role` | int | 0=Employee, 1=Manager, 2=Admin |
| `DepartmentId` | int? | FK → `Department` |
| `HireDate` | DateTime (required) | |
| `IsActive` | bool (default true) | ใช้ทำ **soft deactivate** แทนการลบ |

> การ "ลบ" สมาชิก = ตั้ง `IsActive = false` (soft delete) — รักษาประวัติคำขอลา/balance ไว้ และ login จะถูกปฏิเสธอยู่แล้ว (AuthService เช็ค `IsActive`)

## 4. API Design

ทุก route เป็น kebab-case ภายใต้ `/api/users` (จาก `UsersController` + `SlugifyParameterTransformer`)

| Endpoint | Method | Auth | Role | สถานะ | คำอธิบาย |
|----------|--------|:---:|:---:|:---:|----------|
| `/api/users` | `GET` | ✓ | Admin | **ใหม่** | list + filter (`role`, `departmentId`, `isActive`) + search (`q` = ชื่อ/อีเมล) + pagination (`page`, `pageSize`) |
| `/api/users/{id}` | `GET` | ✓ | any | มีแล้ว | รายละเอียดผู้ใช้ (คืน `UserDto`) |
| `/api/users` | `POST` | ✓ | Admin | มีแล้ว | สร้างผู้ใช้ + auto-generate leave balances ปีปัจจุบัน |
| `/api/users/{id}` | `PUT` | ✓ | Admin | มีแล้ว* | แก้ `Role`, `DepartmentId`, `IsActive` |
| `/api/users/{id}/status` | `PATCH` | ✓ | Admin | **ใหม่** | toggle เปิด/ปิดใช้งาน (`UpdateUserStatusDto`) |
| `/api/users/{id}/password` | `PUT` | ✓ | Admin | **ใหม่** | รีเซ็ตรหัสผ่าน (`AdminResetPasswordDto`) |

> *หมายเหตุ:* `UpdateUserDto` ปัจจุบันมีแค่ `Role / DepartmentId / IsActive` — **ยังแก้ชื่อไม่ได้** ถ้าต้องการให้ admin แก้ `FirstName/LastName` ด้วย ต้องเพิ่ม 2 field นี้เข้า `UpdateUserDto` (ระบุไว้ใน Out of Scope ว่าเป็น optional)

### DTOs

**มีแล้ว** (`Models/DTOs/UserDtos.cs`): `CreateUserDto`, `UpdateUserDto`, `UserDto`

**ใหม่ที่ต้องเพิ่ม:**

```csharp
// query string ของ GET /api/users
public class UserListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? Role { get; set; }
    public int? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
    public string? Q { get; set; }          // ค้นหาจาก FirstName/LastName/Email
}

public class PagedResult<T>
{
    public required List<T> Items { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// แถวในตาราง (เพิ่ม DepartmentName เพื่อแสดงผล โดยไม่ต้องยิงซ้ำ)
public class UserListItemDto
{
    public int UserId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int Role { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateUserStatusDto   { public bool IsActive { get; set; } }
public class AdminResetPasswordDto { public required string NewPassword { get; set; } }
```

## 5. Backend (Service Layer)

ขยาย `IUserService` (`Services/Interfaces/`) + `UserService` (`Services/`)

```csharp
Task<PagedResult<UserListItemDto>> GetUsersAsync(UserListQuery query);   // ใหม่
Task<bool> SetUserStatusAsync(int id, bool isActive);                     // ใหม่
Task<bool> AdminResetPasswordAsync(int id, string newPassword);          // ใหม่
```

- `GetUsersAsync` — สร้าง `IQueryable<User>` แล้ว `.Where(...)` ตาม filter ที่ไม่ null, `.Include(u => u.Department)`, นับ `Total` ก่อน `.Skip().Take()` แล้ว map เป็น `UserListItemDto`
- `SetUserStatusAsync` — โหลด user, ตั้ง `IsActive`, `SaveChangesAsync()` (ใส่ business rule ในข้อ 7)
- `AdminResetPasswordAsync` — `user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword)` (เหมือน flow ใน `CreateUserAsync`) — ไม่คืน plaintext
- คืน `null`/`false` เมื่อไม่พบ (ให้ controller แปลงเป็น `NotFound()`) — ตาม pattern เดิม

## 6. UI/UX Design

เพิ่มส่วน "สมาชิก / ผู้ใช้งาน" ใน `AdminComponent` (`web/src/app/components/admin/`) — แนะนำทำเป็น **tab** ภายในหน้า admin (tab เดิม "ประเภทการลา" + tab ใหม่ "สมาชิก")

**โครงหน้า:**
```
[ Tabs: ประเภทการลา | สมาชิก ]
┌─ section "จัดการสมาชิก" ─────────────────────────────┐
│ [ค้นหา ชื่อ/อีเมล...] [role ▾] [แผนก ▾] [สถานะ ▾]  [+ เพิ่มสมาชิก] │
│ ┌─ table ───────────────────────────────────────────┐ │
│ │ ชื่อ-สกุล | อีเมล | role(badge) | แผนก | สถานะ | วันเริ่มงาน | … │ │
│ │ ...                                       [แก้ไข][รีเซ็ตรหัส][ปิด] │ │
│ └────────────────────────────────────────────────────┘ │
│ [‹ ก่อนหน้า]  หน้า 1/3  [ถัดไป ›]                       │
└──────────────────────────────────────────────────────┘
```

**Modal (reuse `.modal-overlay`+`.modal`):**
- **เพิ่มสมาชิก** — ฟอร์ม: อีเมล, รหัสผ่าน, ชื่อ, นามสกุล, role (select), แผนก (select), วันเริ่มงาน (date) → `POST /api/users`
- **แก้ไข** — role, แผนก, สถานะ (+ ชื่อ ถ้าขยาย DTO) → `PUT /api/users/{id}`
- **รีเซ็ตรหัสผ่าน** — กรอกรหัสใหม่ + ยืนยัน → `PUT /api/users/{id}/password`
- **เปิด/ปิดใช้งาน** — confirm dialog → `PATCH /api/users/{id}/status`

**สถานะหน้า:** `loading` (ระหว่างดึง), empty ("ยังไม่มีสมาชิก" / "ไม่พบผลลัพธ์"), error (alert ตาม pattern เดิม)

**Tokens/patterns:** role badge ใช้สี semantic (`--neutral-soft` employee, `--primary-soft` manager, `--warning-soft` admin — เลือกตามจริง); สถานะ active/inactive ใช้ `.status` pill (`--success-soft` / `--neutral-soft`); ตาราง/ฟอร์ม/ปุ่ม reuse class เดิมทั้งหมด

**Service ฝั่ง FE:** เพิ่มเมธอดใน `LeaveService` หรือสร้าง `AdminUserService` ใหม่:
`getUsers(query)`, `createUser(dto)`, `updateUser(id, dto)`, `setUserStatus(id, isActive)`, `resetUserPassword(id, newPassword)` — โมเดล TS ใหม่: `UserListItem`, `PagedResult<T>`, `UserListQuery`

## 7. Business Rules & Validation

- อีเมลห้ามซ้ำ (มี unique index อยู่แล้ว — `CreateUserAsync` คืน `null` เมื่อซ้ำ → `BadRequest`)
- **ห้าม admin deactivate ตัวเอง** (`id == GetUserId()` → `BadRequest`)
- **ห้าม deactivate / ลดสิทธิ์ Admin คนสุดท้ายที่ active อยู่** (กัน lockout ทั้งระบบ)
- รหัสผ่านใหม่ต้องผ่านเกณฑ์ขั้นต่ำ (เช่น ≥ 8 ตัว) — ตรวจทั้ง FE และ service
- `Role` ต้องอยู่ใน {0,1,2}; `DepartmentId` ถ้าระบุต้องมีอยู่จริง
- การรีเซ็ตรหัส **ไม่คืน plaintext** ใน response (คืน `204`)

## 8. Flow

**สร้างสมาชิก:** Admin กด "เพิ่มสมาชิก" → กรอกฟอร์ม → `POST /api/users` → service hash รหัส + สร้าง user + auto-gen `LeaveBalance` ทุก leave type ของปีปัจจุบัน → `201` → refresh ตาราง

**ปิดใช้งาน:** Admin กด "ปิด" → confirm → `PATCH /api/users/{id}/status {isActive:false}` → ตรวจ rule (ไม่ใช่ตัวเอง / ไม่ใช่ admin คนสุดท้าย) → `204` → user ตัวนั้น login ไม่ได้อีก (AuthService ปฏิเสธ inactive)

**รีเซ็ตรหัส:** Admin กด "รีเซ็ตรหัส" → กรอกรหัสใหม่ → `PUT /api/users/{id}/password` → hash + save → `204` → แจ้งผู้ใช้นอกระบบ

## 9. Edge Cases & Security

- ปิดใช้งานผู้ใช้ที่ **ยังมีคำขอลา pending** → คำขอยังอยู่ในระบบ; manager เห็น/จัดการได้ปกติ (ไม่ลบ)
- `PasswordHash` ห้ามรั่วออกทุก endpoint (DTO ไม่มี field นี้)
- pagination ต้อง clamp `pageSize` (เช่น ≤ 100) กัน query หนัก
- ค้นหา `q` ใช้ parameterized query ของ EF (ป้องกัน injection โดยปริยาย)
- การเปลี่ยน role ของผู้ใช้ระหว่างที่เขา login อยู่ → token เดิมยัง role เก่าจนหมดอายุ (60 นาที) — ยอมรับได้ในระบบ mini, ระบุเป็นข้อจำกัด

## 10. Out of Scope / อนาคต

- แก้ชื่อผ่าน admin (ต้องเพิ่ม `FirstName/LastName` ใน `UpdateUserDto`) — *optional, ทำได้ง่าย*
- ลบถาวร (hard delete) — ตั้งใจไม่ทำ ใช้ soft deactivate แทน
- bulk import / export ผู้ใช้ (CSV)
- บังคับเปลี่ยนรหัสครั้งแรกหลังถูก admin รีเซ็ต
- audit ของการกระทำเหล่านี้ → ดู [audit-log.md](audit-log.md)
