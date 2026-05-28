import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login/login';
import { RegisterComponent } from './auth/register/register';
import { DashboardComponent } from './pages/dashboard/dashboard';
import { StudentFeedComponent } from './pages/student-feed/student-feed';

export const routes: Routes = [
  // 1. Public Access Authentication Routes
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent }, 

  // 2. Protected Dashboard Routes (Always place specific rules on top)
  { path: 'admin/dashboard', component: DashboardComponent },
  { path: 'smart-feed', component: StudentFeedComponent },

  // 3. Root Fallback Redirect Configuration Rules
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  
  // 4. Wildcard Catch-All (CRITICAL: This MUST always be the absolute final entry!)
  { path: '**', redirectTo: '/login' }
];