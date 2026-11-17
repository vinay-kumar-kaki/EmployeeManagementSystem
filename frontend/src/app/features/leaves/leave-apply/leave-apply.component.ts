import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { LeaveService } from '../../../core/services/leave.service';
import { LeaveType } from '../../../core/models/leave.model';

@Component({
  selector: 'app-leave-apply',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './leave-apply.component.html'
})
export class LeaveApplyComponent implements OnInit {
  private fb = inject(FormBuilder);
  private leaveService = inject(LeaveService);
  private router = inject(Router);

  leaveForm!: FormGroup;
  leaveTypes: LeaveType[] = [];
  calculatedDays = 0;
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const dateStr = tomorrow.toISOString().substring(0, 10);

    this.leaveForm = this.fb.group({
      leaveTypeId: [null, [Validators.required]],
      startDate: [dateStr, [Validators.required]],
      endDate: [dateStr, [Validators.required]],
      reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(500)]]
    });

    this.calculateDays();
    this.leaveForm.valueChanges.subscribe(() => this.calculateDays());

    this.leaveService.getLeaveTypes().subscribe({
      next: (types) => this.leaveTypes = types
    });
  }

  calculateDays(): void {
    const start = new Date(this.leaveForm.get('startDate')?.value);
    const end = new Date(this.leaveForm.get('endDate')?.value);

    if (start && end && !isNaN(start.getTime()) && !isNaN(end.getTime()) && end >= start) {
      const diffTime = Math.abs(end.getTime() - start.getTime());
      this.calculatedDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
    } else {
      this.calculatedDays = 0;
    }
  }

  onSubmit(): void {
    if (this.leaveForm.invalid || this.calculatedDays <= 0) {
      this.leaveForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.leaveService.apply(this.leaveForm.value).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/leaves/history']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.message || 'Failed to submit leave application.';
      }
    });
  }
}
