import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LeaveRequest, LeaveBalance, LeaveType, DashboardSummary, CreateLeaveRequest, ApproveLeaveRequest, RejectLeaveRequest } from '../models/leave.model';

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
}
