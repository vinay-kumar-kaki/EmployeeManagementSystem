import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { EmployeeService } from '../../../core/services/employee.service';
import { DepartmentService } from '../../../core/services/department.service';
import { Employee } from '../../../core/models/employee.model';
import { Department } from '../../../core/models/department.model';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './employee-list.component.html'
})
export class EmployeeListComponent implements OnInit {
  private employeeService = inject(EmployeeService);
  private deptService = inject(DepartmentService);

  employees: Employee[] = [];
  departments: Department[] = [];
  searchTerm = '';
  selectedDeptId: number = 0;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadDepartments();
    this.loadEmployees();
  }

  loadDepartments(): void {
    this.deptService.getAll(true).subscribe({
      next: (data) => this.departments = data
    });
  }

  loadEmployees(): void {
    this.isLoading = true;
    this.employeeService.getAll(this.searchTerm, this.selectedDeptId || undefined).subscribe({
      next: (data) => {
        this.employees = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load employees.';
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    this.loadEmployees();
  }

  onFilterChange(): void {
    this.loadEmployees();
  }

  onDelete(id: number): void {
    if (confirm('Are you sure you want to deactivate this employee?')) {
      this.employeeService.delete(id).subscribe({
        next: () => {
          this.successMessage = 'Employee deactivated successfully.';
          this.loadEmployees();
        },
        error: (err) => this.errorMessage = err.message || 'Deactivation failed.'
      });
    }
  }
}
