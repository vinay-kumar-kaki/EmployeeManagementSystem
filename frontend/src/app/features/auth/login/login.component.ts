import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AuthResponse } from '../../../core/models/auth.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loginForm!: FormGroup;
  isLoading = false;
  errorMessage = '';
  returnUrl = '/dashboard';

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }

    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';

    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login(this.loginForm.value).subscribe({
      next: (response: AuthResponse) => {
        this.isLoading = false;
        if (response.success) {
          this.router.navigateByUrl(this.returnUrl);
        } else {
          this.errorMessage = response.message || 'Login failed.';
        }
      },
      error: (err: any) => {
        this.isLoading = false;
        this.errorMessage = err.message || 'Invalid email or password.';
      }
    });
  }

  fillDemoCredentials(role: 'admin' | 'employee'): void {
    if (role === 'admin') {
      this.loginForm.patchValue({
        email: 'admin@company.com',
        password: 'admin@123'
      });
    } else {
      this.loginForm.patchValue({
        email: 'employee@company.com',
        password: 'employee@123'
      });
    }
  }
}
