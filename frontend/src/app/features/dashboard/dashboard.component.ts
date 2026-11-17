import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { AdminDashboardStats, EmployeeDashboardStats } from '../../core/models/dashboard.model';
import { CurrentUser } from '../../core/models/auth.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  public authService = inject(AuthService);
  private dashboardService = inject(DashboardService);

  currentUser: CurrentUser | null = null;
  isAdmin = false;

  adminStats: AdminDashboardStats = {
    totalEmployees: 0,
    activeDepartments: 0,
    pendingLeaves: 0,
    presentTodayCount: 0,
    todayAttendanceRate: '0%'
  };

  employeeStats: EmployeeDashboardStats = {
    totalLeavesApplied: 0,
    approvedLeaves: 0,
    pendingLeaves: 0,
    rejectedLeaves: 0,
    hasCheckedInToday: false,
    formattedTodayHours: '--'
  };

  isLoading = true;

  ngOnInit(): void {
    this.currentUser = this.authService.currentUserValue;
    this.isAdmin = this.authService.hasRole('Admin');
    this.loadStats();
  }

  loadStats(): void {
    this.isLoading = true;
    if (this.isAdmin) {
      this.dashboardService.getAdminStats().subscribe({
        next: (stats) => {
          this.adminStats = stats;
          this.isLoading = false;
        },
        error: () => this.isLoading = false
      });
    } else {
      this.dashboardService.getEmployeeStats().subscribe({
        next: (stats) => {
          this.employeeStats = stats;
          this.isLoading = false;
        },
        error: () => this.isLoading = false
      });
    }
  }
}
