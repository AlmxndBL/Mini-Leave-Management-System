# Documentation — Mini Leave Management System

เอกสารรายละเอียดของระบบจัดการการลา แยกเป็น 2 กลุ่ม:

- **`design/`** — system design ระดับลึก (สถาปัตยกรรม, ฐานข้อมูล, ความปลอดภัย, API)
- **`features/`** — แยกตาม function/feature ของระบบ (แต่ละไฟล์ = 1 ความสามารถ พร้อม flow, business rule, edge case)

> ภาพรวมแบบหน้าเดียวอยู่ที่ [`../LeaveManagement_SystemDesign.md`](../LeaveManagement_SystemDesign.md) — โฟลเดอร์นี้คือเวอร์ชันขยายรายละเอียด

## สารบัญ

### System Design
| ไฟล์ | เนื้อหา |
|---|---|
| [design/architecture.md](design/architecture.md) | Layered architecture, request lifecycle, โครงโปรเจกต์, transaction boundary |
| [design/database.md](design/database.md) | ER diagram, table specs, DDL, index, enum, LeaveBalances lifecycle |
| [design/security.md](design/security.md) | JWT auth, RBAC, สิทธิ์อนุมัติด้วย Departments.ManagerId, authorization matrix |
| [design/api-reference.md](design/api-reference.md) | รายการ endpoint ทั้งหมด พร้อม request/response + error |

### Features (แยกตาม function)
| ไฟล์ | Actor | ความสามารถ |
|---|---|---|
| [features/authentication.md](features/authentication.md) | ทุก role | Login + JWT |
| [features/leave-request.md](features/leave-request.md) | Employee | ยื่นใบลา, ดูประวัติ, ยกเลิก |
| [features/leave-approval.md](features/leave-approval.md) | Manager | ดูคำขอ pending, อนุมัติ, ปฏิเสธ |
| [features/leave-balance.md](features/leave-balance.md) | Employee | ดูวันลาคงเหลือ + lifecycle โควต้า |
| [features/admin.md](features/admin.md) | Admin | จัดการ LeaveTypes, users, สร้างโควต้าปีใหม่ |
| [features/dashboard.md](features/dashboard.md) | Manager | สรุปจำนวนคำขอ pending |

## Glossary (ศัพท์ในระบบ)

| คำ | ความหมาย |
|---|---|
| **Leave Request** | ใบลา 1 ใบ (1 ช่วงวันที่ + 1 ประเภท) — transaction หลัก |
| **Leave Type** | ประเภทการลา (ลาป่วย / ลากิจ / ลาพักร้อน) — master data |
| **Leave Balance** | โควต้าวันลาของคน 1 คน แยกตามประเภท + ปี |
| **Quota / โควต้า** | จำนวนวันลาที่ได้รับต่อปี (`TotalDays`) |
| **RemainingDays** | วันลาคงเหลือ = `TotalDays − UsedDays` (คำนวณใน service ไม่เก็บใน DB) |
| **Approver** | ผู้อนุมัติ = หัวหน้าแผนกของผู้ยื่น (`Departments.ManagerId`) |

## Enum ที่ใช้ทั้งระบบ

**Role** (`Users.Role`)
| ค่า | ความหมาย |
|---|---|
| 0 | Employee |
| 1 | Manager |
| 2 | Admin |

**Status** (`LeaveRequests.Status`)
| ค่า | ความหมาย | ตัดโควต้า? |
|---|---|---|
| 0 | Pending | ยัง |
| 1 | Approved | ✅ ตัดแล้ว |
| 2 | Rejected | ไม่ |
| 3 | Cancelled | ไม่ |

## วิธีอ่าน
- เริ่มที่ [design/architecture.md](design/architecture.md) เพื่อเห็นภาพรวมการไหลของ request
- เวลาจะ implement feature ไหน เปิดไฟล์ใน `features/` ของ feature นั้น — มี flow + rule + endpoint + ตารางที่เกี่ยวข้องครบในที่เดียว
