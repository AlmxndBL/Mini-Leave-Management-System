import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { LeaveService } from '../../services/leave.service';
import { LeaveRequest, LeaveBalance, LeaveType, LeaveRequestStatus, LeaveRequestStatusText, CreateLeaveRequest } from '../../models/leave.model';

@Component({
  selector: 'app-employee',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './employee.component.html',
  styleUrls: ['./employee.component.css']
})
export class EmployeeComponent implements OnInit {
  leaveRequests: LeaveRequest[] = [];
  leaveBalances: LeaveBalance[] = [];
  leaveTypes: LeaveType[] = [];
  loading = false;
  showModal = false;

  newRequest: CreateLeaveRequest = {
    leaveTypeId: 0,
    startDate: '',
    endDate: '',
    reason: ''
  };

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
    this.leaveService.getMyLeaveBalances().subscribe({
      next: (balances) => {
        this.leaveBalances = balances;
      },
      error: (err) => console.error('Error loading balances', err),
      complete: () => this.loading = false
    });

    this.leaveService.getMyLeaveRequests().subscribe({
      next: (requests) => {
        this.leaveRequests = requests;
      },
      error: (err) => console.error('Error loading requests', err)
    });

    this.leaveService.getLeaveTypes().subscribe({
      next: (types) => {
        this.leaveTypes = types;
      },
      error: (err) => console.error('Error loading types', err)
    });
  }

  openModal(): void {
    this.showModal = true;
    // Self-heal: if leave types failed to load on init (e.g. backend not ready),
    // fetch them again when the form is actually opened.
    if (this.leaveTypes.length === 0) {
      this.leaveService.getLeaveTypes().subscribe({
        next: (types) => this.leaveTypes = types,
        error: (err) => console.error('Error loading types', err)
      });
    }
  }

  closeModal(): void {
    this.showModal = false;
    this.newRequest = { leaveTypeId: 0, startDate: '', endDate: '', reason: '' };
  }

  submitRequest(): void {
    if (!this.newRequest.leaveTypeId || !this.newRequest.startDate || !this.newRequest.endDate) {
      alert('กรุณากรอกข้อมูลให้ครบถ้วน');
      return;
    }

    this.leaveService.createLeaveRequest(this.newRequest).subscribe({
      next: () => {
        alert('ยื่นใบลาสำเร็จ');
        this.closeModal();
        this.loadData();
      },
      error: (err) => alert('ข้อผิดพลาด: ' + (err.error?.message || 'ไม่สามารถยื่นใบลาได้'))
    });
  }

  cancelRequest(id: number): void {
    if (confirm('ยืนยันการยกเลิก?')) {
      this.leaveService.cancelLeaveRequest(id).subscribe({
        next: () => {
          alert('ยกเลิกสำเร็จ');
          this.loadData();
        },
        error: (err) => alert('ข้อผิดพลาด')
      });
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
