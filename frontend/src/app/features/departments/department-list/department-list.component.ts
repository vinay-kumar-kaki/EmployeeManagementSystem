import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DepartmentService } from '../../../core/services/department.service';
import { Department } from '../../../core/models/department.model';

@Component({
  selector: 'app-department-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './department-list.component.html'
})
export class DepartmentListComponent implements OnInit {
  private deptService = inject(DepartmentService);
  private fb = inject(FormBuilder);

  departments: Department[] = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  showModal = false;
  isEditing = false;
  selectedDeptId: number | null = null;
  deptForm!: FormGroup;

  ngOnInit(): void {
    this.deptForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(250)]],
      isActive: [true]
    });

    this.loadDepartments();
  }

  loadDepartments(): void {
    this.isLoading = true;
    this.deptService.getAll().subscribe({
      next: (data) => {
        this.departments = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load departments.';
        this.isLoading = false;
      }
    });
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.selectedDeptId = null;
    this.deptForm.reset({ isActive: true });
    this.showModal = true;
  }

  openEditModal(dept: Department): void {
    this.isEditing = true;
    this.selectedDeptId = dept.id;
    this.deptForm.patchValue({
      name: dept.name,
      description: dept.description,
      isActive: dept.isActive
    });
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.errorMessage = '';
  }

  onSubmit(): void {
    if (this.deptForm.invalid) {
      this.deptForm.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    const formValue = this.deptForm.value;

    if (this.isEditing && this.selectedDeptId) {
      this.deptService.update(this.selectedDeptId, formValue).subscribe({
        next: () => {
          this.successMessage = 'Department updated successfully.';
          this.closeModal();
          this.loadDepartments();
        },
        error: (err) => this.errorMessage = err.message || 'Update failed.'
      });
    } else {
      this.deptService.create(formValue).subscribe({
        next: () => {
          this.successMessage = 'Department created successfully.';
          this.closeModal();
          this.loadDepartments();
        },
        error: (err) => this.errorMessage = err.message || 'Create failed.'
      });
    }
  }

  onDelete(id: number): void {
    if (confirm('Are you sure you want to deactivate this department?')) {
      this.deptService.delete(id).subscribe({
        next: () => {
          this.successMessage = 'Department deactivated.';
          this.loadDepartments();
        },
        error: (err) => this.errorMessage = err.message || 'Deactivation failed.'
      });
    }
  }
}
