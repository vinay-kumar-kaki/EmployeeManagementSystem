import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LeaveService } from '../../../core/services/leave.service';
import { LeaveRequest } from '../../../core/models/leave.model';

@Component({
  selector: 'app-leave-history',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './leave-history.component.html'
})
export class LeaveHistoryComponent implements OnInit {
  private leaveService = inject(LeaveService);

  leaves: LeaveRequest[] = [];
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.isLoading = true;
    this.leaveService.getMyHistory().subscribe({
      next: (data) => {
        this.leaves = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load leave history.';
        this.isLoading = false;
      }
    });
  }
}
