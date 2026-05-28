import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.html'
})
export class RegisterComponent {
  private formBuilder = inject(NonNullableFormBuilder);
  private httpClient = inject(HttpClient);
  private router = inject(Router);

  public errorMessage: string = '';
  public successMessage: string = '';

  // Form group definition with explicit client-side validation rules
  public registerForm = this.formBuilder.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    rollNumber: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    role: ['Student', [Validators.required]] // Defaults to Student profile layout
  });

  public onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.httpClient.post('http://localhost:5023/api/auth/register', this.registerForm.getRawValue(), { responseType: 'text' })
      .subscribe({
        next: (response) => {
          this.successMessage = 'Registration successful! Redirecting to auth portal...';
          this.errorMessage = '';
          setTimeout(() => {
            this.router.navigate(['/login']);
          }, 2000); // 2-second delay to show success feedback loop
        },
        error: (err) => {
          this.errorMessage = err.error || 'Registration failed. Parameters rejected by database verification checks.';
          this.successMessage = '';
        }
      });
  }
}