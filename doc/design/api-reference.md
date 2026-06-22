# System Design — API Reference

Base URL: `/api` · Auth: `Authorization: Bearer <JWT>` (ยกเว้น login) · รูปแบบ: JSON

## สรุป endpoint ทั้งหมด

| # | Method | Endpoint | Role | Feature |
|---|---|---|---|---|
| 1 | POST | `/api/auth/login` | public | [authentication](../features/authentication.md) |
| 2 | GET | `/api/leave-balances/me` | Employee | [leave-balance](../features/leave-balance.md) |
| 3 | POST | `/api/leave-requests` | Employee | [leave-request](../features/leave-request.md) |
| 4 | GET | `/api/leave-requests/me` | Employee | [leave-request](../features/leave-request.md) |
| 5 | PUT | `/api/leave-requests/{id}/cancel` | Employee | [leave-request](../features/leave-request.md) |
| 6 | GET | `/api/leave-requests/pending` | Manager | [leave-approval](../features/leave-approval.md) |
| 7 | PUT | `/api/leave-requests/{id}/approve` | Manager | [leave-approval](../features/leave-approval.md) |
| 8 | PUT | `/api/leave-requests/{id}/reject` | Manager | [leave-approval](../features/leave-approval.md) |
| 9 | GET | `/api/dashboard/summary` | Manager | [dashboard](../features/dashboard.md) |
| 10 | POST | `/api/leave-types` | Admin | [admin](../features/admin.md) |
| 11 | PUT | `/api/leave-types/{id}` | Admin | [admin](../features/admin.md) |
| 12 | POST | `/api/users` | Admin | [admin](../features/admin.md) |
| 13 | PUT | `/api/users/{id}` | Admin | [admin](../features/admin.md) |
| 14 | POST | `/api/leave-balances/generate` | Admin | [leave-balance](../features/leave-balance.md) |

---

## 1. POST /api/auth/login `public`
ขอ JWT ด้วย email + password

**Request**
```json
{ "email": "emp@corp.com", "password": "secret123" }
```
**Response 200**
```json
{ "token": "eyJhbGc...", "user": { "userId": 5, "fullName": "สมชาย ใจดี", "role": 0, "departmentId": 2 } }
```
**Errors:** `401` email/password ไม่ถูก หรือ user `IsActive=0`

---

## 2. GET /api/leave-balances/me `Employee`
วันลาคงเหลือของตัวเอง (ปีปัจจุบัน)

**Response 200**
```json
[
  { "leaveTypeId": 1, "name": "ลาป่วย",   "total": 30, "used": 2, "remaining": 28, "colorCode": "#FF5A5A" },
  { "leaveTypeId": 2, "name": "ลากิจ",    "total": 10, "used": 0, "remaining": 10, "colorCode": "#FFA500" },
  { "leaveTypeId": 3, "name": "ลาพักร้อน", "total": 10, "used": 5, "remaining": 5,  "colorCode": "#4CAF50" }
]
```
> `remaining` คำนวณใน service (`total − used`) ไม่ได้เก็บใน DB

---

## 3. POST /api/leave-requests `Employee`
ยื่นใบลา

**Request**
```json
{ "leaveTypeId": 3, "startDate": "2026-07-01", "endDate": "2026-07-03", "reason": "เที่ยวกับครอบครัว" }
```
**Response 201**
```json
{ "leaveRequestId": 41, "leaveTypeId": 3, "startDate": "2026-07-01", "endDate": "2026-07-03",
  "totalDays": 3, "status": 0, "createdAt": "2026-06-21T08:00:00Z" }
```
**Errors:** `400` วันที่ผิด (end < start) / โควต้าไม่พอ (`RemainingDays < TotalDays`)

---

## 4. GET /api/leave-requests/me `Employee`
ประวัติคำขอของตัวเอง (ทุกสถานะ) — รองรับ filter `?status=`

**Response 200**
```json
[
  { "leaveRequestId": 41, "leaveType": "ลาพักร้อน", "startDate": "2026-07-01", "endDate": "2026-07-03",
    "totalDays": 3, "status": 0, "approverComment": null }
]
```

---

## 5. PUT /api/leave-requests/{id}/cancel `Employee`
ยกเลิกใบลาของตัวเอง — **ได้เฉพาะ Status=Pending**

**Response 200** → `{ "leaveRequestId": 41, "status": 3 }`
**Errors:** `403` ไม่ใช่ใบของตัวเอง · `409` สถานะไม่ใช่ Pending

---

## 6. GET /api/leave-requests/pending `Manager`
คำขอที่รออนุมัติ **เฉพาะแผนกที่ผู้เรียกเป็น `Departments.ManagerId`**

**Response 200**
```json
[
  { "leaveRequestId": 41, "employeeName": "สมชาย ใจดี", "leaveType": "ลาพักร้อน",
    "startDate": "2026-07-01", "endDate": "2026-07-03", "totalDays": 3, "reason": "เที่ยว" }
]
```

---

## 7. PUT /api/leave-requests/{id}/approve `Manager`
อนุมัติ — ตัดโควต้าอัตโนมัติใน transaction เดียว

**Response 200** → `{ "leaveRequestId": 41, "status": 1, "approverId": 3 }`
**Errors:**
- `403` ผู้เรียกไม่ใช่ ManagerId ของแผนกผู้ยื่น
- `409` สถานะไม่ใช่ Pending
- `409` โควต้าไม่พอ ณ ตอน approve (re-check ล้มเหลว)

---

## 8. PUT /api/leave-requests/{id}/reject `Manager`
ปฏิเสธ + ใส่ comment (ไม่แตะโควต้า)

**Request** → `{ "comment": "ช่วงนี้งานเยอะ ขอเลื่อน" }`
**Response 200** → `{ "leaveRequestId": 41, "status": 2, "approverComment": "..." }`
**Errors:** `403` ไม่ใช่ ManagerId · `409` ไม่ใช่ Pending

---

## 9. GET /api/dashboard/summary `Manager`
สรุปจำนวนคำขอ pending ของแผนกตน

**Response 200**
```json
{ "pendingCount": 3, "byType": [ { "leaveType": "ลาป่วย", "count": 1 }, { "leaveType": "ลาพักร้อน", "count": 2 } ] }
```

---

## 10. POST /api/leave-types `Admin`
เพิ่มประเภทการลา

**Request** → `{ "name": "ลาคลอด", "defaultDaysPerYear": 90, "colorCode": "#9C27B0" }`
**Response 201** → object ที่สร้าง

## 11. PUT /api/leave-types/{id} `Admin`
แก้ชื่อ / โควต้า default / สี (ไม่กระทบ balance ปีที่ generate ไปแล้ว)

---

## 12. POST /api/users `Admin`
สร้าง user — **สร้าง `LeaveBalances` ของปีปัจจุบันให้อัตโนมัติ** (1 row/LeaveType)

**Request**
```json
{ "email": "new@corp.com", "password": "init123", "firstName": "ก", "lastName": "ข",
  "role": 0, "departmentId": 2, "hireDate": "2026-06-01" }
```
**Response 201** → user object (ไม่มี PasswordHash)

## 13. PUT /api/users/{id} `Admin`
แก้ `role` / `departmentId` / `isActive` (รวมการตั้งใครเป็นหัวหน้าแผนกผ่าน `Departments.ManagerId`)

---

## 14. POST /api/leave-balances/generate?year=YYYY `Admin`
สร้างโควต้าปีใหม่ให้ทุก user ที่ `IsActive=1` (reset ใหม่ ไม่ carry-over)

**Response 200** → `{ "year": 2027, "usersAffected": 12, "rowsCreated": 36 }`

---

## รหัส HTTP ที่ใช้

| Code | ความหมายในระบบนี้ |
|---|---|
| 200 | สำเร็จ |
| 201 | สร้าง resource สำเร็จ |
| 400 | input ผิดรูปแบบ / business rule ไม่ผ่าน (โควต้าไม่พอ, วันที่ผิด) |
| 401 | ไม่ได้ login / token หมดอายุ |
| 403 | login แล้วแต่ไม่มีสิทธิ์ (role หรือไม่ใช่ ManagerId) |
| 404 | ไม่พบ resource |
| 409 | conflict กับสถานะปัจจุบัน (เช่น cancel ใบที่ไม่ใช่ Pending) |
