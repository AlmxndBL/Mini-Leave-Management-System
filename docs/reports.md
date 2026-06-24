# รายงาน & สถิติ (Reports & Analytics)

> เอกสารออกแบบ — ระบบแนะนำ · ผู้ใช้: **Manager / Admin** · สถานะปัจจุบัน: มีแค่ `DashboardController` (`/api/dashboard/summary`) นับ pending/approved/rejected ของแผนก manager เท่านั้น

---

## 1. ภาพรวม & วัตถุประสงค์

ผู้บริหารต้องการเห็นภาพรวมการลา: ใครลามาก, แผนกไหนลาเยอะ, ประเภทไหนถูกใช้มากสุด, ช่วงไหน peak
ระบบนี้ **ต่อยอดจาก `DashboardController` เดิม** เพิ่มรายงานเชิงสรุป (aggregation) ที่กรองตามช่วงเวลา/แผนก/ประเภทได้ พร้อม export เป็น CSV

## 2. ขอบเขต & สิทธิ์

| รายงาน | Employee | Manager | Admin |
|--------|:---:|:---:|:---:|
| สรุปการลา (leave summary) | ✗ | ✓ (เฉพาะแผนกตัวเอง) | ✓ (ทุกแผนก) |
| สรุปสิทธิ์คงเหลือ (balances) | ✗ | ✓ (เฉพาะแผนก) | ✓ |
| export CSV | ✗ | ✓ (ตาม scope) | ✓ |

**Scope enforcement:** Manager เห็นเฉพาะ user ในแผนกที่ตัวเองเป็น `Department.ManagerId`; Admin เห็นทั้งหมด — บังคับในชั้น service ตาม role

## 3. Data Model

ใช้ entity เดิมล้วน — **ไม่มี entity ใหม่** ใช้ aggregation จาก `LeaveRequest`, `LeaveBalance`, `LeaveType`, `User`, `Department`

อ้างอิงค่าคงที่: `LeaveRequestStatus.Approved == 1` (รายงานการลาที่ "ใช้จริง" นับเฉพาะ approved)

## 4. API Design

`ReportsController` ใหม่ → `/api/reports`

| Endpoint | Method | Auth | Role | คำอธิบาย |
|----------|--------|:---:|:---:|----------|
| `/api/reports/leave-summary` | `GET` | ✓ | Manager/Admin | สรุปจำนวนวัน/คำขอ แยกตาม leave type และ/หรือ แผนก; filter `from`,`to`,`departmentId`,`leaveTypeId` |
| `/api/reports/balances` | `GET` | ✓ | Manager/Admin | สรุปสิทธิ์คงเหลือต่อผู้ใช้/แผนก ปีที่ระบุ (`year`) |
| `/api/reports/leave-summary/export` | `GET` | ✓ | Manager/Admin | เหมือน leave-summary แต่คืนไฟล์ CSV (`text/csv`) |

### DTOs (ใหม่)

```csharp
public class ReportQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? DepartmentId { get; set; }
    public int? LeaveTypeId { get; set; }
    public int? Year { get; set; }
}

public class LeaveSummaryRowDto
{
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int LeaveTypeId { get; set; }
    public required string LeaveTypeName { get; set; }
    public string? ColorCode { get; set; }       // จาก LeaveType.ColorCode เพื่อใช้กับกราฟ
    public int RequestCount { get; set; }
    public decimal TotalDays { get; set; }
}

public class BalanceSummaryRowDto
{
    public int UserId { get; set; }
    public required string EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public required string LeaveTypeName { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays { get; set; }
}
```

## 5. Backend (Service Layer)

`IReportService` + `ReportService` ใหม่ (หรือต่อยอด service ของ dashboard)

```csharp
Task<List<LeaveSummaryRowDto>> GetLeaveSummaryAsync(int actorUserId, int actorRole, ReportQuery q);
Task<List<BalanceSummaryRowDto>> GetBalanceSummaryAsync(int actorUserId, int actorRole, ReportQuery q);
Task<byte[]> ExportLeaveSummaryCsvAsync(int actorUserId, int actorRole, ReportQuery q);
```

- ใช้ EF Core `GroupBy` — เช่น leave-summary:
  `LeaveRequests.Where(status==Approved && ช่วงวันที่).GroupBy(r => new { r.User.DepartmentId, r.LeaveTypeId }).Select(g => new LeaveSummaryRowDto { RequestCount = g.Count(), TotalDays = g.Sum(x => x.TotalDays), ... })`
- **Scope:** ถ้า `actorRole == Manager` → จำกัดเฉพาะ user ในแผนกที่ `Department.ManagerId == actorUserId` (เช่นเดียวกับ logic ของ `GetPendingLeaveRequestsAsync` เดิม) ; Admin ไม่จำกัด
- CSV: ประกอบ string ด้วย header ภาษาไทย + แถวข้อมูล, คืน `byte[]` (UTF-8 with BOM เพื่อ Excel ไทยอ่านได้) — controller ตอบ `File(bytes, "text/csv", "leave-summary.csv")`

## 6. UI/UX Design

หน้า Reports ใหม่ (route `/reports`, `data.roles: [Manager, Admin]`) หรือเป็น tab ใน manager/admin dashboard

```
┌─ ตัวกรอง ──────────────────────────────────────────┐
│ [จากวันที่] [ถึงวันที่] [แผนก ▾] [ประเภทลา ▾] [ดูรายงาน][Export CSV]│
└─────────────────────────────────────────────────────┘
┌─ สรุป (summary cards) ─┐  ┌─ กราฟแท่งตามประเภทลา ──┐
│ คำขอทั้งหมด · วันลารวม │  │ ▓▓▓ ลาป่วย  ▓ ลากิจ ... │
└────────────────────────┘  └────────────────────────┘
┌─ ตารางรายละเอียด ─────────────────────────────────┐
│ แผนก | ประเภทลา | จำนวนคำขอ | วันลารวม              │
└────────────────────────────────────────────────────┘
```
- **กราฟ:** เริ่มด้วย **CSS bar ง่าย ๆ** (div ความกว้างตามสัดส่วน) ใช้สีจาก `LeaveType.ColorCode` (มี seed: `#FF5A5A`/`#FFA500`/`#4CAF50`) — chart library (เช่น ng2-charts) เป็น **optional** ในอนาคต
- reuse `summary-card` (มี accent bar), table, filter pattern, tokens
- สถานะ loading/empty ("ไม่มีข้อมูลในช่วงที่เลือก")

**Service ฝั่ง FE:** เพิ่มใน `LeaveService` หรือ `ReportService` ใหม่: `getLeaveSummary(query)`, `getBalanceSummary(query)`, `exportLeaveSummary(query)` (รับ blob แล้ว trigger download); โมเดล TS `LeaveSummaryRow`, `BalanceSummaryRow`, `ReportQuery`

## 7. Business Rules & Validation

- รายงานการลา **นับเฉพาะ `Status == Approved`** (สะท้อนการใช้จริง); ระบุชัดในหัวรายงาน
- ถ้า `from > to` → `BadRequest`
- default ช่วงเวลา: ถ้าไม่ระบุ ใช้ปีปัจจุบัน
- Manager ส่ง `departmentId` ที่ไม่ใช่แผนกตัวเอง → service ยังคง clamp ให้เหลือเฉพาะแผนกตัวเอง (กันการ bypass scope)

## 8. Flow

**Manager ดูรายงานแผนก:** เปิด `/reports` → เลือกช่วงเวลา → "ดูรายงาน" → `GET /api/reports/leave-summary?from=..&to=..` → service จำกัด scope ตามแผนก manager → คืนแถวสรุป → FE วาดการ์ด+กราฟ+ตาราง → กด "Export CSV" → ดาวน์โหลดไฟล์

## 9. Edge Cases & Security

- คำขอที่ครอบช่วงเวลารอยต่อ (ลาคร่อมเดือน/ปี) → กำหนดเกณฑ์นับชัด (แนะนำนับตาม `StartDate` อยู่ในช่วง หรือคำนวณวันทับซ้อน — ระบุวิธีที่เลือกในหัวรายงาน)
- ชุดข้อมูลใหญ่ → aggregation ทำที่ DB (`GroupBy`) ไม่ดึงทุกแถวมา loop ในแอป
- export ต้อง enforce scope เดียวกับ GET (manager export ได้เฉพาะแผนกตัวเอง)
- ระวัง CSV injection: prefix ค่า cell ที่ขึ้นต้นด้วย `= + - @` ด้วย `'`

## 10. Out of Scope / อนาคต

- Dashboard กราฟแบบ interactive / drill-down
- Export PDF / Excel (.xlsx)
- รายงานตามรายบุคคลเชิงลึก, แนวโน้มข้ามปี (trend)
- กำหนดรายงานส่งอัตโนมัติตามรอบ (scheduled report ทาง email)
