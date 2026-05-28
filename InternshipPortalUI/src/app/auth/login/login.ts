import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.html'
})
export class LoginComponent {
  private formBuilder = inject(NonNullableFormBuilder);
  private httpClient = inject(HttpClient);
  private router = inject(Router);

  public errorMessage: string = '';

  public loginForm = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  public onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    console.log("Form is valid. Dispatching authorization payload to .NET API...");

    this.httpClient.post<any>('http://localhost:5023/api/auth/login', this.loginForm.getRawValue())
      .subscribe({
        next: (response) => {
          console.log(".NET Authentication successful! Server response:", response);
          
          // 1. Cache security contexts to local storage disk securely
          localStorage.setItem('authToken', response.token);
          localStorage.setItem('userRole', response.role);

          // 2. Clear asynchronous execution timing conflicts via macro-task pause
          setTimeout(() => {
            if (response.role === 'Admin') {
              console.log("Navigating to Admin Operations Grid Layout...");
              this.router.navigate(['/dashboard']);
            } else if (response.role === 'Student') {
              console.log("Navigating to Personalized Student Smart Feed Layout...");
              this.router.navigate(['/smart-feed']);
            } else {
              this.errorMessage = `Authorization error: Unrecognized identity role: '${response.role}'`;
            }
          }, 50);
        },
        error: (err) => {
          console.error("HTTP Login Post Request Crashed:", err);
          
          if (err.status === 0) {
            this.errorMessage = "Backend Server Connection Offline! Make sure your .NET Web API is running via 'dotnet run'.";
          } else {
            this.errorMessage = err.error?.message || err.error || 'Invalid credentials or account mismatch.';
          }
        }
      });
  }
}