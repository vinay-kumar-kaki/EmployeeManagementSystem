import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { DepartmentListComponent } from './features/departments/department-list/department-list.component';
import { EmployeeListComponent } from './features/employees/employee-list/employee-list.component';
import { EmployeeFormComponent } from './features/employees/employee-form/employee-form.component';
import { ProfileViewComponent } from './features/profile/profile-view/profile-view.component';
import { LeaveApplyComponent } from './features/leaves/leave-apply/leave-apply.component';
import { LeaveHistoryComponent } from './features/leaves/leave-history/leave-history.component';
import { LeaveReviewComponent } from './features/leaves/leave-review/leave-review.component';
import { LeaveTypeManagerComponent } from './features/leaves/leave-type-manager/leave-type-manager.component';
import { RegisterAdminComponent } from './features/auth/register-admin/register-admin.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { AttendanceTrackerComponent } from './features/attendance/attendance-tracker/attendance-tracker.component';
import { AttendanceLogComponent } from './features/attendance/attendance-log/attendance-log.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  
  // Admin Only Routes
  { 
    path: 'departments', 
    component: DepartmentListComponent, 
    canActivate: [authGuard, roleGuard], 
    data: { role: 'Admin' } 
  },
  { 
    path: 'employees', 
    component: EmployeeListComponent, 
    canActivate: [authGuard, roleGuard], 
    data: { role: 'Admin' } 
  },
  { 
    path: 'employees/new', 
    component: EmployeeFormComponent, 
    canActivate: [authGuard, roleGuard], 
    data: { role: 'Admin' } 
  },
  { 
    path: 'employees/:id/edit', 
    component: EmployeeFormComponent, 
    canActivate: [authGuard, roleGuard], 
    data: { role: 'Admin' } 
  },
  { 
    path: 'leaves/review', 
    component: LeaveReviewComponent, 
    canActivate: [authGuard, roleGuard], 
    data: { role: 'Admin' } 
  },
  { 
    path: 'attendance/log', 
    component: AttendanceLogComponent, 
    canActivate: [authGuard, roleGuard], 
    data: { role: 'Admin' } 
  },

  {
    path: 'leave-types',
    component: LeaveTypeManagerComponent,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'admin/register',
    component: RegisterAdminComponent,
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },

  // Employee & General Authenticated Routes
  { path: 'leaves/apply', component: LeaveApplyComponent, canActivate: [authGuard] },
  { path: 'leaves/history', component: LeaveHistoryComponent, canActivate: [authGuard] },
  { path: 'attendance', component: AttendanceTrackerComponent, canActivate: [authGuard] },
  { path: 'profile', component: ProfileViewComponent, canActivate: [authGuard] },

  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: '**', redirectTo: '/dashboard' }
];
