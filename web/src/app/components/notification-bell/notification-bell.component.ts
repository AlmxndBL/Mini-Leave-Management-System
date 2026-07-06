import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LeaveService } from '../../services/leave.service';
import { AppNotification } from '../../models/leave.model';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.css']
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  unreadCount = 0;
  notifications: AppNotification[] = [];
  showDropdown = false;
  loading = false;

  private pollInterval: ReturnType<typeof setInterval> | null = null;

  constructor(private leaveService: LeaveService) {}

  ngOnInit(): void {
    this.fetchUnreadCount();
    this.pollInterval = setInterval(() => this.fetchUnreadCount(), 30000);
  }

  ngOnDestroy(): void {
    if (this.pollInterval) clearInterval(this.pollInterval);
  }

  fetchUnreadCount(): void {
    this.leaveService.getUnreadCount().subscribe({
      next: (r) => { this.unreadCount = r.count; },
      error: () => {}
    });
  }

  toggleDropdown(): void {
    this.showDropdown = !this.showDropdown;
    if (this.showDropdown && this.notifications.length === 0) {
      this.loadNotifications();
    }
  }

  loadNotifications(): void {
    this.loading = true;
    this.leaveService.getMyNotifications().subscribe({
      next: (list) => { this.notifications = list; },
      error: () => {},
      complete: () => { this.loading = false; }
    });
  }

  markRead(n: AppNotification): void {
    if (n.isRead) return;
    this.leaveService.markNotificationRead(n.notificationId).subscribe({
      next: () => {
        n.isRead = true;
        this.unreadCount = Math.max(0, this.unreadCount - 1);
      },
      error: () => {}
    });
  }

  markAllRead(): void {
    this.leaveService.markAllNotificationsRead().subscribe({
      next: () => {
        this.notifications.forEach(n => n.isRead = true);
        this.unreadCount = 0;
      },
      error: () => {}
    });
  }

  @HostListener('document:click', ['$event'])
  onOutsideClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.bell-wrapper')) {
      this.showDropdown = false;
    }
  }
}
