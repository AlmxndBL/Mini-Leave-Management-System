# จัดการแผนก (Department Management)

> เอกสารออกแบบ — ระบบแนะนำ · ผู้ใช้: **Admin** · สถานะปัจจุบัน: `Department` entity **มีอยู่แล้ว** + seed (IT/HR/Sales) แต่ **ไม่มี API/UI** จัดการเลย

---

## 1. ภาพรวม & วัตถุประสงค์

`Department` ถูกอ้างถึงโดย `User.DepartmentId` และเป็นตัวกำหนดว่า manager คนไหนเห็นคำขอลาของใคร (`Department.ManagerId`) แต่ปัจจุบันแก้ไขได้ทาง seed/DB เท่านั้น
ระบบนี้ให้ admin สร้าง/แก้/ลบแผนก และกำหนด manager ของแต่ละแผนกผ่าน UI — ทำให้ระบบจัดการสมาชิกและการอนุมัติลาทำงานครบ

## 2. ขอบเขต & สิทธิ์

| การกระทำ | Employee | Manager | Admin |
|----------|:---:|:---:|:---:|
| ดูรายชื่อแผนก (สำหรับ dropdown) | ✓* | ✓* | ✓ |
| สร้าง / แก้ / ลบแผนก | ✗ | ✗ | ✓ |
| กำหนด manager ของแผนก | ✗ | ✗ | ✓ |

> *GET list เปิดให้ทุก auth เพื่อใช้เติม dropdown (เช่นในฟอร์มสร้างผู้ใช้) — การเขียน (POST/PUT/DELETE) จำกัด Admin

## 3. Data Model

ใช้ `Department` เดิม (`LeaveManagement.Api/Models/Entities/Department.cs`) — **ไม่ต้องเพิ่ม field**

| Field | ชนิด | หมายเหตุ |
|-------|------|----------|
| `DepartmentId` | int (PK) | |
| `Name` | string (required, unique) | |
| `ManagerId` | int? | FK → `User` (ผู้จัดการแผนก) |

ความสัมพันธ์เดิม: `Manager` (User คนเดียว), `Users` (สมาชิกในแผนก) — on delete: `User.DepartmentId` ถูก `SetNull`

## 4. API Design

`DepartmentsController` ใหม่ → `/api/departments`

| Endpoint | Method | Auth | Role | คำอธิบาย |
|----------|--------|:---:|:---:|----------|
| `/api/departments` | `GET` | ✓ | any | list ทั้งหมด (`DepartmentDto` + ชื่อ manager + จำนวนสมาชิก) |
| `/api/departments/{id}` | `GET` | ✓ | any | รายละเอียดแผนก |
| `/api/departments` | `POST` | ✓ | Admin | สร้างแผนก |
| `/api/departments/{id}` | `PUT` | ✓ | Admin | แก้ชื่อ / กำหนด manager |
| `/api/departments/{id}` | `DELETE` | ✓ | Admin | ลบแผนก (มีเงื่อนไข — ดูข้อ 7) |

### DTOs (ใหม่)

```csharp
public class DepartmentDto
{
    public int DepartmentId { get; set; }
    public required string Name { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public int MemberCount { get; set; }       // จำนวน user ในแผนก
}
public class CreateDepartmentDto { public required string Name { get; set; } public int? ManagerId { get; set; } }
public class UpdateDepartmentDto { public required string Name { get; set; } public int? ManagerId { get; set; } }
```

## 5. Backend (Service Layer)

`IDepartmentService` + `DepartmentService` ใหม่

```csharp
Task<List<DepartmentDto>> GetAllAsync();
Task<DepartmentDto?> GetByIdAsync(int id);
Task<DepartmentDto?> CreateAsync(CreateDepartmentDto dto);   // null ถ้าชื่อซ้ำ
Task<bool> UpdateAsync(int id, UpdateDepartmentDto dto);
Task<bool> DeleteAsync(int id);                              // false ถ้ายังมีสมาชิก active
```

- map `MemberCount` ด้วย `.Count()` ของ `Users`; `ManagerName` จาก `.Include(d => d.Manager)`

## 6. UI/UX Design

เพิ่ม section/tab "แผนก" ใน `AdminComponent`

```
┌─ section "จัดการแผนก" ──────────────────────┐
│                                [+ เพิ่มแผนก] │
│ ┌─ table ──────────────────────────────────┐ │
│ │ ชื่อแผนก | ผู้จัดการ | จำนวนสมาชิก |  …    │ │
│ │ IT       | สมชาย    | 5         [แก้ไข][ลบ] │ │
│ └──────────────────────────────────────────┘ │
└──────────────────────────────────────────────┘
```
- **Modal เพิ่ม/แก้ไข** — ชื่อแผนก + dropdown เลือก manager (โหลดจาก `GET /api/users?role=1`)
- **ลบ** — confirm; ถ้าแผนกยังมีสมาชิก แสดง error ให้ย้ายสมาชิกก่อน
- reuse table/modal/form patterns + tokens; dropdown แผนกนี้ยังถูกใช้ซ้ำในฟอร์มของระบบจัดการสมาชิก

## 7. Business Rules & Validation

- `Name` ห้ามซ้ำ (unique index มีอยู่แล้ว) — service คืน `null` เมื่อซ้ำ → `BadRequest`
- **ลบแผนกที่ยังมี user active ไม่ได้** → ต้องย้ายสมาชิกออกก่อน (หรือ block พร้อมข้อความ)
- `ManagerId` ถ้าระบุ ต้องชี้ไปยัง user ที่ `Role == UserRoles.Manager` และ active
- ลบแผนกได้เมื่อไม่มีสมาชิก: user ที่เคยอยู่จะถูก `SetNull` ที่ `DepartmentId` ตาม config เดิม (แต่กฎข้างต้น block ก่อนถ้ายัง active)

## 8. Flow

**สร้างแผนก + ตั้ง manager:** Admin กด "เพิ่มแผนก" → กรอกชื่อ + เลือก manager → `POST /api/departments` → `201` → refresh
**ลบแผนก:** กด "ลบ" → confirm → `DELETE /api/departments/{id}` → service เช็คสมาชิก active → ลบได้/`BadRequest`

## 9. Edge Cases & Security

- เปลี่ยน manager ของแผนก → คำขอลา pending จะเข้า scope ของ manager คนใหม่ทันที (manager ดูตามแผนกที่ตัวเองดูแล)
- manager ที่ถูกถอด/ปิดใช้งาน แต่ยังผูกเป็น `ManagerId` → ควรเตือน admin / บังคับ reassign
- write endpoints จำกัด Admin ทั้งหมด

## 10. Out of Scope / อนาคต

- โครงสร้างแผนกหลายชั้น (parent/child department)
- manager หลายคนต่อแผนก
- ย้ายสมาชิกหลายคนพร้อมกัน (bulk reassign)
- audit การเปลี่ยนแปลงแผนก → ดู [audit-log.md](audit-log.md)
