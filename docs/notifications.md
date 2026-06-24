# แจ้งเตือนในแอป (In-App Notifications)

> เอกสารออกแบบ — ระบบแนะนำ · ผู้ใช้: **ทุก role** · สถานะปัจจุบัน: **ยังไม่มี** — ผู้ใช้ต้อง refresh หน้าเองเพื่อรู้ผลคำขอลา

---

## 1. ภาพรวม & วัตถุประสงค์

ปัจจุบันเมื่อพนักงานยื่นคำขอลา manager ไม่รู้ทันที และเมื่อ manager อนุมัติ/ปฏิเสธ พนักงานก็ไม่รู้จนกว่าจะเปิดหน้าเช็คเอง
ระบบนี้เพิ่ม **การแจ้งเตือนภายในแอป (in-app)** — ไอคอนกระดิ่งบน navbar + จำนวนที่ยังไม่อ่าน + รายการแจ้งเตือน เพื่อให้ผู้ใช้รับรู้เหตุการณ์สำคัญทันทีโดยไม่ต้องส่ง email

## 2. ขอบเขต & สิทธิ์

| การกระทำ | เจ้าของ notification | คนอื่น |
|----------|:---:|:---:|
| ดู/อ่าน notification ของตัวเอง | ✓ | ✗ |
| mark read / read-all ของตัวเอง | ✓ | ✗ |

notification ผูกกับ `UserId` ผู้รับ; ทุก endpoint ดึง `userId` จาก JWT — เห็นเฉพาะของตัวเอง (ทุก role)

## 3. Data Model

**Entity ใหม่ `Notification`** (`Models/Entities/Notification.cs`) + `DbSet<Notification>` ใน `AppDbContext`

| Field | ชนิด | หมายเหตุ |
|-------|------|----------|
| `NotificationId` | int (PK) | |
| `UserId` | int (FK → User) | ผู้รับ |
| `Type` | int | ประเภท (ดูตารางด้านล่าง) |
| `Title` | string (required, max 150) | หัวข้อ (ไทย) |
| `Message` | string (required, max 500) | เนื้อหา |
| `IsRead` | bool (default false) | |
| `RelatedEntityType` | string? (max 50) | เช่น `"LeaveRequest"` |
| `RelatedEntityId` | int? | id ของ entity ที่เกี่ยว เพื่อ deep-link |
| `CreatedAt` | DateTime | (มี SQL default ตาม pattern `LeaveRequest`) |

แนะนำ index: `(UserId, IsRead)` และ `(UserId, CreatedAt)`

**ประเภท (`NotificationType` ใน `Helpers/Constants.cs`):**

| ค่า | ความหมาย | ผู้รับ |
|----|-----------|--------|
| 0 `LeaveRequestSubmitted` | มีคำขอลาใหม่รออนุมัติ | manager ของแผนกผู้ขอ |
| 1 `LeaveRequestApproved` | คำขอลาได้รับอนุมัติ | พนักงานเจ้าของคำขอ |
| 2 `LeaveRequestRejected` | คำขอลาถูกปฏิเสธ | พนักงานเจ้าของคำขอ |
| 3 `LeaveBalanceGenerated` | (optional) สิทธิ์ลาปีใหม่พร้อมใช้ | ผู้ใช้ที่เกี่ยวข้อง |

## 4. API Design

`NotificationsController` ใหม่ → `/api/notifications`

| Endpoint | Method | Auth | Role | คำอธิบาย |
|----------|--------|:---:|:---:|----------|
| `/api/notifications/me` | `GET` | ✓ | any | list ของตัวเอง (query `?unreadOnly=true`, pagination) |
| `/api/notifications/me/unread-count` | `GET` | ✓ | any | จำนวนที่ยังไม่อ่าน (สำหรับ badge) |
| `/api/notifications/{id}/read` | `PUT` | ✓ | any | mark อ่าน (เฉพาะของตัวเอง) |
| `/api/notifications/read-all` | `PUT` | ✓ | any | mark อ่านทั้งหมดของตัวเอง |

### DTOs (ใหม่)

```csharp
public class NotificationDto
{
    public int NotificationId { get; set; }
    public int Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## 5. Backend (Service Layer)

`INotificationService` + `NotificationService` ใหม่

```csharp
Task CreateAsync(int userId, int type, string title, string message,
                 string? relatedType = null, int? relatedId = null);   // เรียกจาก service อื่น
Task<List<NotificationDto>> GetMineAsync(int userId, bool unreadOnly, int page, int pageSize);
Task<int> GetUnreadCountAsync(int userId);
Task<bool> MarkReadAsync(int userId, int notificationId);              // ตรวจ ownership
Task<int> MarkAllReadAsync(int userId);
```

**จุดเชื่อม (เรียก `CreateAsync`) — ใน `LeaveRequestService` ที่มีอยู่:**
- `CreateLeaveRequestAsync` สำเร็จ → แจ้ง manager ของแผนกผู้ขอ (type 0). หา manager จาก `Department.ManagerId` ของแผนกผู้ขอ
- `ApproveLeaveRequestAsync` สำเร็จ → แจ้งเจ้าของคำขอ (type 1)
- `RejectLeaveRequestAsync` สำเร็จ → แจ้งเจ้าของคำขอ (type 2)

> inject `INotificationService` เข้า `LeaveRequestService` (Scoped DI เหมือน service อื่น) — การ create notification ไม่ควร throw ทำให้ flow หลักล้ม (จับ error/log แล้วไปต่อ)

## 6. UI/UX Design

- เพิ่มไอคอน 🔔 ใน `.navbar` (ทุก dashboard) + badge ตัวเลข unread (pill ใช้ `--danger` / `--radius-pill`)
- คลิกแล้วเปิด **dropdown panel** รายการแจ้งเตือนล่าสุด (reuse เงา `--shadow-lg`, `--surface`, `--radius-lg`)
- แต่ละรายการ: ไอคอนตาม type (สี semantic: approved=`--success`, rejected=`--danger`, submitted=`--warning`), title, message, เวลา (relative เช่น "5 นาทีที่แล้ว"); ที่ยังไม่อ่านมีจุด/พื้น `--primary-soft`
- คลิกรายการ → mark read + (ถ้ามี related entity) นำทางไปหน้าเกี่ยวข้อง; ปุ่ม "ทำเครื่องหมายอ่านทั้งหมด"
- **Polling แบบง่าย:** เรียก `unread-count` ทุก ~30–60 วิ (ไม่ทำ real-time/websocket ในรอบนี้)

**Service ฝั่ง FE:** เพิ่มใน `LeaveService` หรือ `NotificationService` ใหม่: `getMyNotifications(unreadOnly?)`, `getUnreadCount()`, `markRead(id)`, `markAllRead()`; โมเดล TS `Notification`

## 7. Business Rules & Validation

- mark read ได้เฉพาะ notification ที่ `UserId == ตัวเอง` (กัน IDOR)
- ถ้าแผนกผู้ขอ **ไม่มี manager** (`ManagerId == null`) → ข้ามการแจ้ง type 0 (ไม่ error)
- ไม่แจ้งซ้ำสำหรับเหตุการณ์เดียว

## 8. Flow

**ยื่นคำขอ → อนุมัติ:**
1. Employee ยื่น → `LeaveRequestService.Create` สำเร็จ → `Notification.CreateAsync(managerId, type=0, ...)`
2. Manager เห็น badge เพิ่ม → เปิด dropdown → คลิก → ไปหน้าคำขอ pending → อนุมัติ
3. `Approve` สำเร็จ → `CreateAsync(employeeId, type=1, ...)` → Employee เห็นแจ้งเตือน "อนุมัติแล้ว"

## 9. Edge Cases & Security

- ผู้รับถูกปิดใช้งานภายหลัง → notification ยังอยู่ แต่เขา login ไม่ได้ (ยอมรับได้)
- ลบ/ยกเลิกคำขอที่เกี่ยวข้อง → notification ยังอยู่ (เป็น log เหตุการณ์ ณ เวลานั้น) ; deep-link ควร handle กรณี entity หาย
- จำกัด `pageSize` กัน query หนัก
- ไม่เก็บข้อมูลอ่อนไหวใน message (เป็นเพียงสรุปเหตุการณ์)

## 10. Out of Scope / อนาคต

- Email / Push / LINE notification
- Real-time ผ่าน WebSocket/SignalR (รอบนี้ใช้ polling)
- ตั้งค่าความชอบการแจ้งเตือน (preferences) ต่อผู้ใช้
- รวมกลุ่ม (digest) แจ้งเตือน
