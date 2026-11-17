import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AttendanceService } from '../../../core/services/attendance.service';
import { AttendanceRecord, AttendanceStatus } from '../../../core/models/attendance.model';

@Component({
  selector: 'app-attendance-tracker',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './attendance-tracker.component.html'
})
export class AttendanceTrackerComponent implements OnInit, OnDestroy {
  private attendanceService = inject(AttendanceService);

  todayStatus: AttendanceStatus | null = null;
  history: AttendanceRecord[] = [];
  currentTime = new Date();
  private timer: any;

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.timer = setInterval(() => {
      this.currentTime = new Date();
    }, 1000);

    this.loadStatus();
    this.loadHistory();
  }

  ngOnDestroy(): void {
    if (this.timer) {
      clearInterval(this.timer);
    }
  }

  loadStatus(): void {
    this.attendanceService.getTodayStatus().subscribe({
      next: (status) => this.todayStatus = status,
      error: (err) => console.error(err)
    });
  }

  loadHistory(): void {
    this.attendanceService.getMyHistory().subscribe({
      next: (data) => this.history = data,
      error: (err) => console.error(err)
    });
  }

  onCheckIn(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.attendanceService.checkIn().subscribe({
      next: (status) => {
        this.todayStatus = status;
        this.successMessage = 'Successfully checked in for today!';
        this.isLoading = false;
        this.loadHistory();
      },
      error: (err) => {
        this.errorMessage = err.message || 'Check-in failed.';
        this.isLoading = false;
      }
    });
  }

  onCheckOut(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.attendanceService.checkOut().subscribe({
      next: (status) => {
        this.todayStatus = status;
        this.successMessage = 'Successfully checked out! Shift logged.';
        this.isLoading = false;
        this.loadHistory();
      },
      error: (err) => {
        this.errorMessage = err.message || 'Check-out failed.';
        this.isLoading = false;
      }
    });
  }
}
