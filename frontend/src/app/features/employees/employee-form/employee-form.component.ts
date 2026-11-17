import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { EmployeeService } from '../../../core/services/employee.service';
import { DepartmentService } from '../../../core/services/department.service';
import { Department } from '../../../core/models/department.model';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './employee-form.component.html'
})
export class EmployeeFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private employeeService = inject(EmployeeService);
  private deptService = inject(DepartmentService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  employeeForm!: FormGroup;
  departments: Department[] = [];
  isEditMode = false;
  employeeId: number | null = null;
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEditMode = true;
      this.employeeId = parseInt(idParam, 10);
    }

    this.initForm();
    this.loadDepartments();

    if (this.isEditMode && this.employeeId) {
      this.loadEmployeeData(this.employeeId);
    }
  }

  initForm(): void {
    this.employeeForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      password: [
        '',
        this.isEditMode
          ? []
          : [
              Validators.required,
              Validators.minLength(8),
              Validators.pattern(/^(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/)
            ]
      ],
      phoneNumber: [''],
      departmentId: [null, [Validators.required]],
      designation: ['', [Validators.required]],
      joiningDate: [new Date().toISOString().substring(0, 10), [Validators.required]],
      salary: [50000, [Validators.required, Validators.min(1)]],
      isActive: [true]
    });
  }

  loadDepartments(): void {
    this.deptService.getAll(true).subscribe({
      next: (data) => this.departments = data
    });
  }

  loadEmployeeData(id: number): void {
    this.isLoading = true;
    this.employeeService.getById(id).subscribe({
      next: (emp) => {
        this.employeeForm.patchValue({
          firstName: emp.firstName,
          lastName: emp.lastName,
          email: emp.email,
          phoneNumber: emp.phoneNumber,
          departmentId: emp.departmentId,
          designation: emp.designation,
          joiningDate: emp.joiningDate.substring(0, 10),
          salary: emp.salary,
          isActive: emp.isActive
        });
        // Email cannot be changed once created
        this.employeeForm.get('email')?.disable();
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load employee details.';
        this.isLoading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    const formValue = this.employeeForm.getRawValue();

    if (this.isEditMode && this.employeeId) {
      this.employeeService.update(this.employeeId, formValue).subscribe({
        next: () => {
          this.isLoading = false;
          this.router.navigate(['/employees']);
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = err.message || 'Update failed.';
        }
      });
    } else {
      this.employeeService.create(formValue).subscribe({
        next: () => {
          this.isLoading = false;
          this.router.navigate(['/employees']);
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = err.message || 'Employee creation failed.';
        }
      });
    }
  }
}
