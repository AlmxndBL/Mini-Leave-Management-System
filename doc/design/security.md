# System Design — Security

ระบบมี 2 ชั้นความปลอดภัยที่**แยกหน้าที่กันชัดเจน**: Authentication (คุณเป็นใคร) และ Authorization (คุณทำอะไรได้)

## 1. Authentication (JWT)

- Login ด้วย **Email + Password** → ได้ **JWT access token** (Bearer)
- Password เก็บเป็น **BCrypt hash** (`Users.PasswordHash`) — ไม่เก็บ plaintext
- Token มีอายุ ~**60 นาที**; **ไม่มี refresh token** (out of scope โดยตั้งใจ — หมดอายุแล้ว login ใหม่)
- ฝั่ง Angular: interceptor แนบ `Authorization: Bearer <token>` ทุก request
- ฝั่ง API: middleware ตรวจ signature + วันหมดอายุของ token ก่อนเข้า controller

### JWT Claims
token บรรจุข้อมูลพอให้ตัดสิน authorization ได้โดยไม่ต้อง query DB ซ้ำทุกครั้ง:

| Claim | ใช้ทำอะไร |
|---|---|
| `sub` / UserId | ระบุผู้เรียก (เช่น "คำขอของฉัน") |
| `role` | คุม access ระดับ endpoint (`[Authorize(Roles=...)]`) |
| `departmentId` | ช่วยกรองข้อมูลระดับแผนก |
| `exp` | วันหมดอายุ |

> หมายเหตุ: สิทธิ์ "อนุมัติรายใบ" ไม่ได้อิงแค่ claim — ต้องเช็คกับ `Departments.ManagerId` ใน DB (ดูข้อ 3)

## 2. Authorization ชั้นที่ 1 — Role (RBAC)

`Users.Role` คุม access **ระดับ endpoint/เมนู** เท่านั้น ผ่าน `[Authorize(Roles="...")]`

| Role | เข้าถึงกลุ่ม endpoint |
|---|---|
| Employee (0) | leave-requests (ของตัวเอง), leave-balances/me |
| Manager (1) | + leave-requests/pending, approve, reject, dashboard |
| Admin (2) | + leave-types, users, leave-balances/generate |

## 3. Authorization ชั้นที่ 2 — Approval Authority (Departments.ManagerId)

นี่คือจุดที่ต้องเข้าใจให้ชัด: **Role=Manager แค่ผ่านด่านเข้า endpoint ได้** แต่ "ใครอนุมัติใบนี้ได้จริง" ตัดสินด้วย `Departments.ManagerId`

> **กฎ:** ผู้เรียก `approve`/`reject`/`pending` จะดำเนินการกับใบลาได้ก็ต่อเมื่อ
> `approver.UserId == Departments.ManagerId` ของแผนกผู้ยื่น (`request.User.DepartmentId`)

เหตุผลที่แยก 2 ชั้น:
- ป้องกันความกำกวม ถ้าในแผนกมีคน Role=Manager หลายคน — มีแค่ `ManagerId` ที่อนุมัติของแผนกนั้นได้
- Role ใช้คุมเมนู (ใครเห็นหน้า approval), ManagerId ใช้คุมข้อมูล (เห็น/กดของแผนกไหน)

## 4. Authorization Matrix

| Endpoint | Role gate | เงื่อนไขระดับข้อมูล (เพิ่ม) |
|---|---|---|
| `POST /api/auth/login` | public | — |
| `GET /api/leave-balances/me` | Employee+ | เฉพาะของตัวเอง (UserId จาก token) |
| `POST /api/leave-requests` | Employee+ | สร้างในชื่อตัวเอง |
| `GET /api/leave-requests/me` | Employee+ | เฉพาะของตัวเอง |
| `PUT /api/leave-requests/{id}/cancel` | Employee+ | เฉพาะใบของตัวเอง + Status=Pending |
| `GET /api/leave-requests/pending` | Manager+ | เฉพาะแผนกที่ตนเป็น ManagerId |
| `PUT /api/leave-requests/{id}/approve` | Manager+ | ใบของแผนกที่ตนเป็น ManagerId |
| `PUT /api/leave-requests/{id}/reject` | Manager+ | ใบของแผนกที่ตนเป็น ManagerId |
| `GET /api/dashboard/summary` | Manager+ | สรุปเฉพาะแผนกตน |
| `POST/PUT /api/leave-types/**` | Admin | — |
| `POST/PUT /api/users/**` | Admin | — |
| `POST /api/leave-balances/generate` | Admin | — |

## 5. การจัดการ Password

- ตอนสร้าง user: hash ด้วย BCrypt ก่อนเก็บ
- ตอน login: เทียบ `BCrypt.Verify(input, PasswordHash)`
- ไม่มี endpoint ส่ง PasswordHash กลับออกไป (DTO ไม่มี field นี้)

## 6. ภัยที่พิจารณา / Out of Scope

| ประเด็น | สถานะ |
|---|---|
| Password hashing | ✅ BCrypt |
| Token หมดอายุ | ✅ ~60 นาที |
| ป้องกัน over-posting | ✅ ใช้ DTO ไม่รับ entity ตรง |
| ตรวจสิทธิ์อนุมัติรายใบ | ✅ ManagerId |
| Refresh token | ❌ out of scope |
| Rate limiting / lockout | ❌ out of scope |
| Multi-level approval | ❌ out of scope |
| Audit log การกระทำ | ❌ out of scope (มีแค่ CreatedAt/UpdatedAt) |
