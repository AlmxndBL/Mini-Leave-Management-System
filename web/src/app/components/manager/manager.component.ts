import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { LeaveService } from '../../services/leave.service';
import { LeaveRequest, DashboardSummary, ApproveLeaveRequest, RejectLeaveRequest, LeaveRequestStatusText } from '../../models/leave.model';

@Component({
  selector: 'app-manager',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manager.component.html',
  styleUrls: ['./manager.component.css']
})
export class ManagerComponent implements OnInit {
  pendingRequests: LeaveRequest[] = [];
  summary: DashboardSummary | null = null;
  loading = false;
  showApprovalModal = false;
  selectedRequest: LeaveRequest | null = null;
  approvalComment = '';
  rejectionComment = '';
  isRejecting = false;

  user: any;
  LeaveRequestStatusText = LeaveRequestStatusText;

  constructor(
    private authService: AuthService,
    private leaveService: LeaveService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.user = this.authService.getCurrentUser();
    this.loadData();
  }

  getStatusText(status: number): string {
    return LeaveRequestStatusText[status as keyof typeof LeaveRequestStatusText] || 'ไม่ทราบสถานะ';
  }

  loadData(): void {
    this.loading = true;
    this.leaveService.getPendingLeaveRequests().subscribe({
      next: (requests) => {
        this.pendingRequests = requests;
      },
      error: (err) => console.error('Error loading pending requests', err),
      complete: () => this.loading = false
    });

    this.leaveService.getDashboardSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
      },
      error: (err) => console.error('Error loading summary', err)
    });
  }

  openApprovalModal(request: LeaveRequest, isRejecting: boolean = false): void {
    this.selectedRequest = request;
    this.isRejecting = isRejecting;
    this.approvalComment = '';
    this.rejectionComment = '';
    this.showApprovalModal = true;
  }

  closeApprovalModal(): void {
    this.showApprovalModal = false;
    this.selectedRequest = null;
  }

  approve(): void {
    if (!this.selectedRequest) return;

    const dto: ApproveLeaveRequest = { comment: this.approvalComment };
    this.leaveService.approveLeaveRequest(this.selectedRequest.leaveRequestId, dto).subscribe({
      next: () => {
        alert('อนุมัติสำเร็จ');
        this.closeApprovalModal();
        this.loadData();
      },
      error: (err) => alert('ข้อผิดพลาด: ' + (err.error?.message || 'ไม่สามารถอนุมัติได้'))
    });
  }

  reject(): void {
    if (!this.selectedRequest || !this.rejectionComment) {
      alert('กรุณากรอกเหตุผลการปฏิเสธ');
      return;
    }

    const dto: RejectLeaveRequest = { comment: this.rejectionComment };
    this.leaveService.rejectLeaveRequest(this.selectedRequest.leaveRequestId, dto).subscribe({
      next: () => {
        alert('ปฏิเสธสำเร็จ');
        this.closeApprovalModal();
        this.loadData();
      },
      error: (err) => alert('ข้อผิดพลาด: ' + (err.error?.message || 'ไม่สามารถปฏิเสธได้'))
    });
  }

  goToReports(): void {
    this.router.navigate(['/reports']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
