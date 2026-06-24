# ระบบจัดการข้อมูลส่วนตัว (Personal Profile Management)

> เอกสารออกแบบ — ระบบหลัก · ผู้ใช้: **ทุก role (self-service)** · สถานะปัจจุบัน: **ยังไม่มี** — ผู้ใช้แก้ข้อมูลตัวเองหรือเปลี่ยนรหัสผ่านไม่ได้เลย

---

## 1. ภาพรวม & วัตถุประสงค์

ตอนนี้ข้อมูลผู้ใช้แก้ไขได้โดย admin เท่านั้น ผู้ใช้ทั่วไป **เปลี่ยนรหัสผ่านเองไม่ได้** และไม่มีที่เก็บข้อมูลติดต่อ (เบอร์โทร, ที่อยู่)
ระบบนี้เพิ่มหน้า "โปรไฟล์ของฉัน" ให้ผู้ใช้ทุก role:
- ดู/แก้ข้อมูลส่วนตัวของตัวเอง (เฉพาะ field ที่ไม่กระทบสิทธิ์)
- เปลี่ยนรหัสผ่านตัวเอง (ยืนยันรหัสเดิมก่อน)

หลักการความปลอดภัยสำคัญ: **ผู้ใช้แก้ได้เฉพาะข้อมูลของตัวเอง** และ **ห้ามยกระดับสิทธิ์ตัวเอง** (ไม่แตะ `Role`/`DepartmentId`/`IsActive`/`Email`)

## 2. ขอบเขต & สิทธิ์ (Roles & Permissions)

| การกระทำ | Employee | Manager | Admin |
|----------|:---:|:---:|:---:|
| ดูโปรไฟล์ของตัวเอง | ✓ | ✓ | ✓ |
| แก้ไขข้อมูลส่วนตัวของตัวเอง | ✓ | ✓ | ✓ |
| เปลี่ยนรหัสผ่านของตัวเอง | ✓ | ✓ | ✓ |
| แก้ role / department / สถานะ ของตัวเอง | ✗ | ✗ | ✗ (ใช้ระบบจัดการสมาชิกเท่านั้น) |

`userId` ดึงจาก **JWT claim เท่านั้น** (`GetUserId()`) — ไม่รับ id จาก client

## 3. Data Model

**แนะนำ: ขยาย entity `User` ด้วย field optional (nullable)** แทนการสร้างตารางใหม่

| Field ใหม่ | ชนิด | หมายเหตุ |
|-----------|------|----------|
| `PhoneNumber` | string? (max 20) | เบอร์ติดต่อ |
| `AvatarUrl` | string? (max 500) | URL รูปโปรไฟล์ (ดูหมายเหตุ avatar) |
| `DateOfBirth` | DateTime? | วันเกิด |
| `Address` | string? (max 300) | ที่อยู่ |

> **ทำไมขยาย `User` ไม่แยกตาราง?** ระบบ mini มีความสัมพันธ์ 1:1 ตายตัว, field น้อย, ไม่มีความต้องการแยก lifecycle — การขยาย `User` ลด join และ migration ซับซ้อน
> **ทางเลือก (ถ้าต้องการ):** สร้าง entity `UserProfile` (PK = `UserId`, 1:1 กับ `User`) เมื่อคาดว่า field โปรไฟล์จะขยายมากในอนาคต — แลกกับ join เพิ่มทุกครั้ง
> **Avatar:** รอบนี้เก็บเป็น **URL string** เท่านั้น (เช่น ใช้ภาพจาก external/gravatar) — การ upload ไฟล์จริงอยู่ใน Out of Scope

ต้องสร้าง EF Core migration ใหม่ (เพิ่มคอลัมน์ nullable — ไม่กระทบข้อมูลเดิม)

## 4. API Design

`ProfileController` ใหม่ → `/api/profile` (kebab-case อัตโนมัติ)

| Endpoint | Method | Auth | Role | คำอธิบาย |
|----------|--------|:---:|:---:|----------|
| `/api/profile/me` | `GET` | ✓ | any | ดึงโปรไฟล์ของผู้ใช้ที่ login (`ProfileDto`) |
| `/api/profile/me` | `PUT` | ✓ | any | แก้ field ที่อนุญาต (`UpdateProfileDto`) |
| `/api/profile/me/password` | `PUT` | ✓ | any | เปลี่ยนรหัสผ่านตัวเอง (`ChangePasswordDto`) |

### DTOs (ใหม่)

```csharp
public class ProfileDto
{
    public int UserId { get; set; }
    public required string Email { get; set; }       // อ่านอย่างเดียว
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int Role { get; set; }                    // อ่านอย่างเดียว (แสดงผล)
    public int? DepartmentId { get; set; }           // อ่านอย่างเดียว
    public string? DepartmentName { get; set; }
    public DateTime HireDate { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
}

// เฉพาะ field ที่เจ้าของแก้ได้ — จงใจไม่มี Role/DepartmentId/IsActive/Email
public class UpdateProfileDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}
```

## 5. Backend (Service Layer)

`IProfileService` + `ProfileService` ใหม่ (ลงทะเบียน Scoped ใน `Program.cs`)

```csharp
Task<ProfileDto?> GetMyProfileAsync(int userId);
Task<bool> UpdateMyProfileAsync(int userId, UpdateProfileDto dto);
Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);   // false ถ้า currentPassword ผิด
```

- `ChangePasswordAsync` — `BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash)`; ถ้าไม่ผ่านคืน `false` → controller ตอบ `BadRequest("รหัสผ่านเดิมไม่ถูกต้อง")`; ถ้าผ่าน hash รหัสใหม่แล้ว save
- `UpdateMyProfileAsync` — โหลด user ของตัวเอง แก้เฉพาะ field ใน `UpdateProfileDto` เท่านั้น (controller/service **ไม่อ่าน** Role/Dept จาก body)

## 6. UI/UX Design

- เพิ่ม route ใหม่ `/profile` ใน `app.routes.ts` — `canActivate: [AuthGuard]` แต่ **ไม่ระบุ `data.roles`** (ทุก role ที่ login เข้าได้)
- สร้าง `ProfileComponent` (standalone) ใหม่
- เพิ่มลิงก์ "โปรไฟล์ของฉัน" ใน `.navbar` ของ employee/manager/admin (มี navbar อยู่แล้วทุกหน้า)

**โครงหน้า:**
```
┌─ การ์ดโปรไฟล์ (.section) ─────────────────┐
│ [avatar] ชื่อ-สกุล                          │
│ อีเมล (อ่านอย่างเดียว)   role(badge)  แผนก │
│ เบอร์โทร · วันเกิด · ที่อยู่                │
│                              [แก้ไขข้อมูล]  │
└────────────────────────────────────────────┘
┌─ ความปลอดภัย (.section) ──────────────────┐
│ รหัสผ่าน  ••••••••          [เปลี่ยนรหัสผ่าน]│
└────────────────────────────────────────────┘
```
- **Modal แก้ไขข้อมูล** — form-group: ชื่อ, นามสกุล, เบอร์, วันเกิด (date), ที่อยู่ (textarea), avatar URL → `PUT /api/profile/me`
- **Modal เปลี่ยนรหัสผ่าน** — รหัสเดิม, รหัสใหม่, ยืนยันรหัสใหม่ → `PUT /api/profile/me/password`
- field ที่อ่านอย่างเดียว (email, role, แผนก, วันเริ่มงาน) แสดงแต่แก้ไม่ได้
- reuse `.modal`, `.form-group`, `.btn-primary/secondary`, tokens; สถานะ loading/empty/error ตาม pattern เดิม

**Service ฝั่ง FE:** เพิ่มใน `AuthService` (เหมาะกับเรื่องตัวตน) หรือ `ProfileService` ใหม่: `getMyProfile()`, `updateMyProfile(dto)`, `changePassword(dto)`
หลังแก้ชื่อสำเร็จ ควร refresh `currentUser$`/localStorage ให้ navbar แสดงชื่อใหม่

## 7. Business Rules & Validation

- รหัสใหม่ ≥ 8 ตัว และต้องตรงกับช่อง "ยืนยันรหัสใหม่" (ตรวจฝั่ง FE) ; service ตรวจ currentPassword ด้วย BCrypt
- รหัสใหม่ต้องไม่เท่ารหัสเดิม (แนะนำ)
- `PhoneNumber` รับเฉพาะรูปแบบที่กำหนด (ตัวเลข/`+`/`-`/เว้นวรรค)
- `AvatarUrl` ต้องเป็น URL รูปแบบถูกต้อง (และแนะนำจำกัด scheme `https`)
- ชื่อ/นามสกุล ห้ามว่าง

## 8. Flow

**เปลี่ยนรหัสผ่าน:** ผู้ใช้เปิด `/profile` → กด "เปลี่ยนรหัสผ่าน" → กรอกรหัสเดิม+ใหม่+ยืนยัน → `PUT /api/profile/me/password` → service `Verify` รหัสเดิม → ผ่าน: hash+save → `204` แจ้งสำเร็จ; ไม่ผ่าน: `400` แสดง "รหัสผ่านเดิมไม่ถูกต้อง"

**แก้ข้อมูล:** เปิด modal → แก้ → `PUT /api/profile/me` → `204` → refresh การ์ด + ชื่อบน navbar

## 9. Edge Cases & Security

- **กันยกระดับสิทธิ์:** แม้ client แอบส่ง `role`/`departmentId` มาใน body, `UpdateProfileDto` ไม่มี field เหล่านี้ → ถูกละทิ้งโดยอัตโนมัติ
- userId มาจาก token เท่านั้น — ป้องกัน IDOR (เข้าถึงโปรไฟล์คนอื่น)
- เปลี่ยนรหัสไม่กระทบ token ปัจจุบัน (ยังใช้ได้จนหมดอายุ) — ระบบ mini ไม่ทำ token revocation (ระบุเป็นข้อจำกัด)
- ผู้ใช้ที่ถูก admin ปิดใช้งานระหว่าง session → token เดิมยังเรียก API ได้จนหมดอายุ (ข้อจำกัดเดียวกับทั้งระบบ)

## 10. Out of Scope / อนาคต

- อัปโหลดไฟล์รูป avatar จริง (object storage) — รอบนี้ใช้ URL
- ลืมรหัสผ่าน (forgot password via email) — แยกระบบ, ต้องมี email service
- ยืนยัน/เปลี่ยนอีเมล
- 2FA / token revocation เมื่อเปลี่ยนรหัส
- audit ของการเปลี่ยนข้อมูล/รหัส → ดู [audit-log.md](audit-log.md)
