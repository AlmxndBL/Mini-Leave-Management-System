export interface AppNotification {
  notificationId: number;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  relatedEntityType?: string;
  relatedEntityId?: number;
  createdAt: string;
}

export interface LeaveRequest {
  leaveRequestId: number;
  employeeName: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason?: string;
  status: number;
  createdAt: string;
  approverComment?: string;
}

export interface CreateLeaveRequest {
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  reason?: string;
}

export interface ApproveLeaveRequest {
  comment?: string;
}

export interface RejectLeaveRequest {
  comment: string;
}

export interface LeaveBalance {
  leaveBalanceId: number;
  leaveTypeName: string;
  colorCode?: string;
  totalDays: number;
  usedDays: number;
  remainingDays: number;
  year: number;
}

export interface LeaveType {
  leaveTypeId: number;
  name: string;
  defaultDaysPerYear: number;
  colorCode?: string;
}

export interface DashboardSummary {
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
}

export interface LeaveSummaryRow {
  departmentName: string;
  leaveTypeName: string;
  leaveTypeColor: string;
  requestCount: number;
  employeeCount: number;
  totalDays: number;
}

export interface ReportQuery {
  year?: number;
  startDate?: string;
  endDate?: string;
  departmentId?: number;
  leaveTypeId?: number;
}

export const LeaveRequestStatus = {
  Pending: 0,
  Approved: 1,
  Rejected: 2,
  Cancelled: 3
};

export const LeaveRequestStatusText = {
  0: 'รอการอนุมัติ',
  1: 'อนุมัติแล้ว',
  2: 'ปฏิเสธ',
  3: 'ยกเลิก'
};
