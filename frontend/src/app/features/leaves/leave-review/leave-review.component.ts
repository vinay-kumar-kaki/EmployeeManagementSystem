import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LeaveService } from '../../../core/services/leave.service';
import { LeaveRequest } from '../../../core/models/leave.model';

type LeaveTab = 'pending' | 'all';

@Component({
  selector: 'app-leave-review',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './leave-review.component.html'
})
export class LeaveReviewComponent implements OnInit {
  private leaveService = inject(LeaveService);

  pendingLeaves: LeaveRequest[] = [];
  allLeaves: LeaveRequest[] = [];
  activeTab: LeaveTab = 'pending';
  statusFilter: number | 'all' = 'all';

  isLoading = true;
  errorMessage = '';
  successMessage = '';

  selectedLeave: LeaveRequest | null = null;
  adminRemarks = '';
  reviewAction: 1 | 2 = 1; // 1 = Approve, 2 = Reject
  showModal = false;

  ngOnInit(): void {
    this.loadPending();
  }

  get displayedLeaves(): LeaveRequest[] {
    if (this.activeTab === 'pending') {
      return this.pendingLeaves;
    }

    if (this.statusFilter === 'all') {
      return this.allLeaves;
    }

    return this.allLeaves.filter(l => l.status === this.statusFilter);
  }

  get approvedCount(): number {
    return this.allLeaves.filter(l => l.status === 1).length;
  }

  get rejectedCount(): number {
    return this.allLeaves.filter(l => l.status === 2).length;
  }

  get pendingCount(): number {
    return this.allLeaves.filter(l => l.status === 0).length;
  }

  selectTab(tab: LeaveTab): void {
    if (this.activeTab === tab) return;

    this.activeTab = tab;
    this.errorMessage = '';

    if (tab === 'pending') {
      this.loadPending();
    } else {
      this.loadAll();
    }
  }

  onStatusFilterChange(): void {
    // Filtering is done client-side through the displayedLeaves getter.
  }

  loadPending(): void {
    this.isLoading = true;
    this.leaveService.getPending().subscribe({
      next: (data) => {
        this.pendingLeaves = data;
        this.allLeaves = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load pending leave requests.';
        this.isLoading = false;
      }
    });
  }

  loadAll(): void {
    this.isLoading = true;
    this.leaveService.getAll().subscribe({
      next: (data) => {
        this.allLeaves = data;
        this.pendingLeaves = data.filter(l => l.status === 0);
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load leave requests.';
        this.isLoading = false;
      }
    });
  }

  refresh(): void {
    if (this.activeTab === 'pending') {
      this.loadPending();
    } else {
      this.loadAll();
    }
  }

  openReviewModal(leave: LeaveRequest, action: 1 | 2): void {
    this.selectedLeave = leave;
    this.reviewAction = action;
    this.adminRemarks = '';
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.selectedLeave = null;
  }

  submitReview(): void {
    if (!this.selectedLeave) return;

    this.leaveService.review(this.selectedLeave.id, {
      status: this.reviewAction,
      adminRemarks: this.adminRemarks
    }).subscribe({
      next: () => {
        this.successMessage = `Leave request marked as ${this.reviewAction === 1 ? 'Approved' : 'Rejected'}.`;
        this.closeModal();
        this.refresh();
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to submit review.';
      }
    });
  }
}
