# บันทึกกิจกรรม (Audit Log)

> เอกสารออกแบบ — ระบบแนะนำ · ผู้ใช้: **Admin** · สถานะปัจจุบัน: **ยังไม่มี** — ไม่มีหลักฐานว่าใครทำอะไรเมื่อไหร่

---

## 1. ภาพรวม & วัตถุประสงค์

ระบบจัดการสมาชิก/แผนก และการอนุมัติลา เป็นการกระทำที่ "อ่อนไหว" (เปลี่ยน role, รีเซ็ตรหัส, อนุมัติลา) แต่ปัจจุบัน **ไม่มีร่องรอย** ว่าใครทำ
ระบบนี้บันทึก **audit trail** ของการกระทำสำคัญ เพื่อความโปร่งใส, สอบสวนย้อนหลัง และความรับผิดชอบ (accountability) — แสดงผลให้ admin ตรวจสอบได้

## 2. ขอบเขต & สิทธิ์

| การกระทำ | Employee | Manager | Admin |
|----------|:---:|:---:|:---:|
| ดู audit log | ✗ | ✗ | ✓ |
| สร้าง audit entry | — เกิดอัตโนมัติจาก service (ไม่มี endpoint สร้างตรง ๆ) — |

audit เป็น **append-only** — ไม่มี endpoint แก้/ลบ

## 3. Data Model

**Entity ใหม่ `AuditLog`** (`Models/Entities/AuditLog.cs`) + `DbSet<AuditLog>`

| Field | ชนิด | หมายเหตุ |
|-------|------|----------|
| `AuditLogId` | int (PK) | |
| `ActorUserId` | int (FK → User) | ผู้กระทำ (ดึงจาก JWT) |
| `Action` | string (max 100) | เช่น `"User.Create"`, `"User.Deactivate"`, `"LeaveRequest.Approve"` |
| `EntityType` | string (max 50) | เช่น `"User"`, `"LeaveRequest"`, `"Department"` |
| `EntityId` | int? | id ของ entity เป้าหมาย |
| `Details` | string? (text) | JSON สรุป (เช่น ค่า before/after, comment) |
| `CreatedAt` | DateTime | (SQL default ตาม pattern เดิม) |

แนะนำ index: `(CreatedAt)`, `(ActorUserId)`, `(EntityType, EntityId)`

**เก็บกริยาอะไรบ้าง (action ที่ควร log):**
- `User.Create` / `User.Update` / `User.Deactivate` / `User.RoleChange` / `User.PasswordReset`
- `Profile.Update` / `Profile.PasswordChange` (self-service)
- `LeaveRequest.Approve` / `LeaveRequest.Reject` / `LeaveRequest.Cancel`
- `LeaveType.Create` / `LeaveType.Update`
- `Department.Create` / `Department.Update` / `Department.Delete`
- (optional) `Auth.Login` / `Auth.LoginFailed`

## 4. API Design

`AuditLogsController` ใหม่ → `/api/audit-logs`

| Endpoint | Method | Auth | Role | คำอธิบาย |
|----------|--------|:---:|:---:|----------|
| `/api/audit-logs` | `GET` | ✓ | Admin | list + filter (`actorUserId`, `action`, `entityType`, `from`, `to`) + pagination |

### DTOs (ใหม่)

```csharp
public class AuditLogDto
{
    public int AuditLogId { get; set; }
    public int ActorUserId { get; set; }
    public string? ActorName { get; set; }      // join จาก User เพื่อแสดงผล
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class AuditLogQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int? ActorUserId { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
```

## 5. Backend (Service Layer)

`IAuditService` + `AuditService` ใหม่

```csharp
Task LogAsync(int actorUserId, string action, string entityType, int? entityId = null, object? details = null);
Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogQuery query);
```

- `LogAsync` serialize `details` เป็น JSON string เก็บใน `Details`
- inject `IAuditService` เข้า service ที่เกี่ยวข้อง (`UserService`, `LeaveRequestService`, `LeaveTypeService`, `DepartmentService`, `ProfileService`) แล้วเรียก `LogAsync` หลังการกระทำสำเร็จ
- การ log **ต้องไม่ทำให้ flow หลักล้ม** (จับ error/log warning แล้วไปต่อ)

**ทางเลือกการ implement (ระบุข้อดี/ข้อเสีย):**

| แนวทาง | ข้อดี | ข้อเสีย | คำแนะนำ |
|--------|-------|---------|---------|
| **เรียก `LogAsync` ตรง ๆ ใน service** | ควบคุมได้ว่า log อะไร + ใส่ context/before-after ได้ครบ | ต้องแก้หลาย service / อาจลืม log บางจุด | ✅ แนะนำสำหรับระบบ mini — ชัดเจน, ตรงไปตรงมา |
| **EF Core `SaveChanges` interceptor** | ดักทุกการเปลี่ยน entity อัตโนมัติ | รู้แค่ "ข้อมูลเปลี่ยน" ไม่รู้ "เจตนา/ใคร" ง่าย ๆ, business action เช่น approve อธิบายยาก | ใช้เสริมสำหรับ data-change log ละเอียด |

## 6. UI/UX Design

เพิ่ม section/tab "บันทึกกิจกรรม" ใน `AdminComponent`

```
┌─ section "บันทึกกิจกรรม" ────────────────────────────┐
│ [ผู้ทำ ▾] [action ▾] [ประเภท ▾] [จากวันที่] [ถึงวันที่] │
│ ┌─ table ────────────────────────────────────────────┐ │
│ │ เวลา | ผู้ทำ | การกระทำ | เป้าหมาย | รายละเอียด     │ │
│ └─────────────────────────────────────────────────────┘ │
│ [‹]  หน้า 1/N  [›]                                       │
└──────────────────────────────────────────────────────────┘
```
- `Action` แสดงเป็นภาษาไทยผ่าน map (เช่น `User.Deactivate` → "ปิดใช้งานผู้ใช้")
- `Details` แสดงย่อ + คลิกเพื่อดู JSON เต็มใน modal
- reuse table/filter/modal patterns + tokens; วันที่/เวลา format ไทย

## 7. Business Rules & Validation

- audit เป็น **read-only + append-only** — ไม่มี API แก้/ลบ
- `ActorUserId` มาจาก token เท่านั้น (ปลอมไม่ได้)
- `Details` ห้ามเก็บความลับ เช่น **รหัสผ่าน/hash** (สำหรับ password reset/change เก็บแค่ "เปลี่ยนรหัสผ่าน" ไม่เก็บค่า)
- จำกัด `pageSize` (≤ 200)

## 8. Flow

**ตัวอย่าง deactivate ผู้ใช้:**
1. Admin กดปิดใช้งาน → `UserService.SetUserStatusAsync` สำเร็จ
2. service เรียก `AuditService.LogAsync(adminId, "User.Deactivate", "User", targetId, new { isActive=false })`
3. admin เปิดหน้า "บันทึกกิจกรรม" → กรอง `action=User.Deactivate` → เห็นรายการพร้อมเวลา/ผู้ทำ

## 9. Edge Cases & Security

- การกระทำที่ล้มเหลว (เช่น approve ไม่ผ่าน) → ตัดสินใจว่าจะ log หรือไม่ (แนะนำ log เฉพาะที่สำเร็จ + optional login-failed)
- ปริมาณ log โตเร็ว → ใส่หมายเหตุ retention (เช่น เก็บ 1 ปี แล้ว archive) — รอบนี้ยังไม่ทำ auto-purge
- เฉพาะ Admin เข้าถึง — เพราะ log อาจเผยพฤติกรรมผู้ใช้

## 10. Out of Scope / อนาคต

- Export audit (CSV/PDF)
- Auto-retention / archive / purge
- Tamper-proofing (hash chain / WORM storage)
- before/after diff อัตโนมัติทุก field (รอบนี้เก็บ summary ที่เลือกเอง)
- แจ้งเตือน admin เมื่อมี action เสี่ยงสูง
