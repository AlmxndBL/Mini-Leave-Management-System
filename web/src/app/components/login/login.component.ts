import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LoginRequest } from '../../models/auth.model';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  email = '';
  password = '';
  loading = false;
  error = '';

  constructor(private authService: AuthService, private router: Router) { }

  login(): void {
    if (!this.email || !this.password) {
      this.error = 'กรุณากรอก email และ password';
      return;
    }

    this.loading = true;
    this.error = '';

    const credentials: LoginRequest = { email: this.email, password: this.password };

    this.authService.login(credentials).subscribe({
      next: (response) => {
        this.loading = false;
        const roleRoute = this.getRoleRoute(response.role);
        this.router.navigate([roleRoute]);
      },
      error: (err) => {
        this.loading = false;
        this.error = 'Email หรือ password ไม่ถูกต้อง';
      }
    });
  }

  private getRoleRoute(role: number): string {
    switch (role) {
      case 0: return '/employee';
      case 1: return '/manager';
      case 2: return '/admin';
      default: return '/';
    }
  }
}
