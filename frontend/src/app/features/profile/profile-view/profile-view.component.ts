import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmployeeService } from '../../../core/services/employee.service';
import { EmployeeProfile } from '../../../core/models/employee.model';

@Component({
  selector: 'app-profile-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-view.component.html'
})
export class ProfileViewComponent implements OnInit {
  private employeeService = inject(EmployeeService);

  profile: EmployeeProfile | null = null;
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.employeeService.getMyProfile().subscribe({
      next: (data) => {
        this.profile = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load profile.';
        this.isLoading = false;
      }
    });
  }
}
