import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LeaveRequest, LeaveBalance, LeaveType, DashboardSummary, CreateLeaveRequest, ApproveLeaveRequest, RejectLeaveRequest, LeaveSummaryRow, ReportQuery } from '../models/leave.model';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {
  private apiUrl = 'http://localhost:5125/api';

  constructor(private http: HttpClient) { }

  // Leave Requests
  createLeaveRequest(request: CreateLeaveRequest): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(`${this.apiUrl}/leave-requests`, request);
  }

  getLeaveRequest(id: number): Observable<LeaveRequest> {
    return this.http.get<LeaveRequest>(`${this.apiUrl}/leave-requests/${id}`);
  }

  getMyLeaveRequests(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/leave-requests/me`);
  }

  getPendingLeaveRequests(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/leave-requests/pending`);
  }

  approveLeaveRequest(id: number, dto: ApproveLeaveRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/leave-requests/${id}/approve`, dto);
  }

  rejectLeaveRequest(id: number, dto: RejectLeaveRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/leave-requests/${id}/reject`, dto);
  }

  cancelLeaveRequest(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/leave-requests/${id}/cancel`, {});
  }

  // Leave Balances
  getMyLeaveBalances(): Observable<LeaveBalance[]> {
    return this.http.get<LeaveBalance[]>(`${this.apiUrl}/leave-balances/me`);
  }

  generateLeaveBalances(year: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/leave-balances/generate`, null, { params: { year } });
  }

  // Leave Types
  getLeaveTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(`${this.apiUrl}/leave-types`);
  }

  createLeaveType(type: LeaveType): Observable<LeaveType> {
    return this.http.post<LeaveType>(`${this.apiUrl}/leave-types`, type);
  }

  updateLeaveType(id: number, type: Partial<LeaveType>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/leave-types/${id}`, type);
  }

  // Dashboard
  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/dashboard/summary`);
  }

  // Reports
  getLeaveSummary(query: ReportQuery): Observable<LeaveSummaryRow[]> {
    const params: Record<string, string> = {};
    if (query.year) params['year'] = query.year.toString();
    if (query.startDate) params['startDate'] = query.startDate;
    if (query.endDate) params['endDate'] = query.endDate;
    if (query.departmentId) params['departmentId'] = query.departmentId.toString();
    if (query.leaveTypeId) params['leaveTypeId'] = query.leaveTypeId.toString();
    return this.http.get<LeaveSummaryRow[]>(`${this.apiUrl}/reports/leave-summary`, { params });
  }

  exportLeaveSummary(query: ReportQuery): void {
    const params = new URLSearchParams();
    if (query.year) params.append('year', query.year.toString());
    if (query.startDate) params.append('startDate', query.startDate);
    if (query.endDate) params.append('endDate', query.endDate);
    if (query.departmentId) params.append('departmentId', query.departmentId.toString());
    if (query.leaveTypeId) params.append('leaveTypeId', query.leaveTypeId.toString());
    const token = localStorage.getItem('token');
    const qs = params.toString();
    const url = `${this.apiUrl}/reports/leave-summary/export${qs ? '?' + qs : ''}`;
    fetch(url, { headers: { Authorization: `Bearer ${token}` } })
      .then(r => r.blob())
      .then(blob => {
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = `leave-summary-${new Date().toISOString().slice(0, 10)}.csv`;
        a.click();
        URL.revokeObjectURL(a.href);
      });
  }
}
