import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LeaveService } from '../../../core/services/leave.service';
import { LeaveType } from '../../../core/models/leave.model';

@Component({
  selector: 'app-leave-type-manager',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './leave-type-manager.component.html'
})
export class LeaveTypeManagerComponent implements OnInit {
  private leaveService = inject(LeaveService);
  private fb = inject(FormBuilder);

  leaveTypes: LeaveType[] = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  showModal = false;
  isEditing = false;
  selectedTypeId: number | null = null;
  typeForm!: FormGroup;

  ngOnInit(): void {
    this.typeForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      defaultDaysPerYear: [12, [Validators.required, Validators.min(1), Validators.max(365)]],
      isActive: [true]
    });

    this.loadTypes();
  }

  loadTypes(): void {
    this.isLoading = true;
    this.leaveService.getAllTypes().subscribe({
      next: (data) => {
        this.leaveTypes = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load leave types.';
        this.isLoading = false;
      }
    });
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.selectedTypeId = null;
    this.typeForm.reset({ name: '', defaultDaysPerYear: 12, isActive: true });
    this.showModal = true;
  }

  openEditModal(leaveType: LeaveType): void {
    this.isEditing = true;
    this.selectedTypeId = leaveType.id;
    this.typeForm.patchValue({
      name: leaveType.name,
      defaultDaysPerYear: leaveType.defaultDaysPerYear,
      isActive: leaveType.isActive
    });
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.errorMessage = '';
  }

  onSubmit(): void {
    if (this.typeForm.invalid) {
      this.typeForm.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    const payload = this.typeForm.value;

    if (this.isEditing && this.selectedTypeId != null) {
      this.leaveService.updateType(this.selectedTypeId, payload).subscribe({
        next: () => {
          this.successMessage = 'Leave type updated successfully.';
          this.closeModal();
          this.loadTypes();
        },
        error: (err) => this.errorMessage = err.message || 'Update failed.'
      });
    } else {
      this.leaveService.createType(payload).subscribe({
        next: () => {
          this.successMessage = 'Leave type created successfully.';
          this.closeModal();
          this.loadTypes();
        },
        error: (err) => this.errorMessage = err.message || 'Create failed.'
      });
    }
  }

  setActive(leaveType: LeaveType, isActive: boolean): void {
    const action = isActive ? 'reactivate' : 'deactivate';
    if (!confirm(`Are you sure you want to ${action} "${leaveType.name}"?`)) return;

    this.leaveService.updateType(leaveType.id, {
      name: leaveType.name,
      defaultDaysPerYear: leaveType.defaultDaysPerYear,
      isActive
    }).subscribe({
      next: () => {
        this.successMessage = `Leave type ${action}d.`;
        this.loadTypes();
      },
      error: (err) => this.errorMessage = err.message || `Failed to ${action} leave type.`
    });
  }
}
