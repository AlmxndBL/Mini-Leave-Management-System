import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { LeaveService } from '../../services/leave.service';
import { LeaveType } from '../../models/leave.model';
import { NotificationBellComponent } from '../notification-bell/notification-bell.component';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, NotificationBellComponent],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css']
})
export class AdminComponent implements OnInit {
  leaveTypes: LeaveType[] = [];
  loading = false;
  showModal = false;

  newType: Partial<LeaveType> = {
    name: '',
    defaultDaysPerYear: 10,
    colorCode: '#999999'
  };

  user: any;

  constructor(
    private authService: AuthService,
    private leaveService: LeaveService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.user = this.authService.getCurrentUser();
    this.loadLeaveTypes();
  }

  loadLeaveTypes(): void {
    this.loading = true;
    this.leaveService.getLeaveTypes().subscribe({
      next: (types) => {
        this.leaveTypes = types;
      },
      error: (err) => console.error('Error loading types', err),
      complete: () => this.loading = false
    });
  }

  openModal(): void {
    this.newType = { name: '', defaultDaysPerYear: 10, colorCode: '#999999' };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  saveLeaveType(): void {
    if (!this.newType.name || !this.newType.defaultDaysPerYear) {
      alert('กรุณากรอกข้อมูลให้ครบถ้วน');
      return;
    }

    this.leaveService.createLeaveType(this.newType as LeaveType).subscribe({
      next: () => {
        alert('บันทึกสำเร็จ');
        this.closeModal();
        this.loadLeaveTypes();
      },
      error: (err) => alert('ข้อผิดพลาด')
    });
  }

  generateLeaveBalances(): void {
    const year = new Date().getFullYear() + 1;
    if (confirm(`สร้างโควต้าวันลาสำหรับปี ${year}?`)) {
      this.leaveService.generateLeaveBalances(year).subscribe({
        next: () => {
          alert('สร้างโควต้าสำเร็จ');
        },
        error: (err) => alert('ข้อผิดพลาด')
      });
    }
  }

  goToReports(): void {
    this.router.navigate(['/reports']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
