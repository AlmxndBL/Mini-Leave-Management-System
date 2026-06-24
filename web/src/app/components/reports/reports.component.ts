import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { LeaveService } from '../../services/leave.service';
import { LeaveType, LeaveSummaryRow, ReportQuery } from '../../models/leave.model';
import { UserRoles } from '../../models/auth.model';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.css']
})
export class ReportsComponent implements OnInit {
  user: ReturnType<AuthService['getCurrentUser']> = null;
  role = UserRoles.Employee;

  leaveTypes: LeaveType[] = [];
  rows: LeaveSummaryRow[] = [];

  query: ReportQuery = { year: new Date().getFullYear() };

  loading = false;
  searched = false;

  get isAdmin(): boolean { return this.role === UserRoles.Admin; }
  get totalRequests(): number { return this.rows.reduce((s, r) => s + r.requestCount, 0); }
  get totalDays(): number { return this.rows.reduce((s, r) => s + r.totalDays, 0); }

  constructor(
    private authService: AuthService,
    private leaveService: LeaveService,
    private router: Router
  ) {
    this.user = authService.getCurrentUser();
    this.role = this.user?.role ?? UserRoles.Employee;
  }

  ngOnInit(): void {
    this.leaveService.getLeaveTypes().subscribe({ next: t => this.leaveTypes = t });
    this.search();
  }

  search(): void {
    this.loading = true;
    this.leaveService.getLeaveSummary(this.query).subscribe({
      next: rows => { this.rows = rows; this.loading = false; this.searched = true; },
      error: () => { this.loading = false; this.searched = true; }
    });
  }

  export(): void {
    this.leaveService.exportLeaveSummary(this.query);
  }

  clearYear(): void {
    this.query.year = undefined;
  }

  goBack(): void {
    const dest: Record<number, string> = {
      [UserRoles.Manager]: '/manager',
      [UserRoles.Admin]: '/admin'
    };
    this.router.navigate([dest[this.role] ?? '/login']);
  }
}
