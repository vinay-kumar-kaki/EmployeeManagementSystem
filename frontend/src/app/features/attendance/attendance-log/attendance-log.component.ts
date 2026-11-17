import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AttendanceService } from '../../../core/services/attendance.service';
import { AttendanceRecord } from '../../../core/models/attendance.model';

@Component({
  selector: 'app-attendance-log',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './attendance-log.component.html'
})
export class AttendanceLogComponent implements OnInit {
  private attendanceService = inject(AttendanceService);

  records: AttendanceRecord[] = [];
  selectedDate = new Date().toISOString().substring(0, 10);
  isLoading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.loadRecords();
  }

  loadRecords(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.attendanceService.getAll(this.selectedDate).subscribe({
      next: (data) => {
        this.records = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message || 'Failed to load attendance records.';
        this.isLoading = false;
      }
    });
  }

  onDateChange(): void {
    this.loadRecords();
  }

  get checkedOutCount(): number {
    return this.records.filter(r => !!r.checkOutTime).length;
  }

  get currentlyInCount(): number {
    return this.records.filter(r => !r.checkOutTime).length;
  }

  get averageDuration(): string {
    const completed = this.records.filter(r => r.workDurationMinutes != null);
    if (completed.length === 0) return '--';

    const totalMinutes = completed.reduce((sum, r) => sum + (r.workDurationMinutes || 0), 0);
    const average = Math.round(totalMinutes / completed.length);
    return this.formatMinutes(average);
  }

  formatMinutes(minutes?: number): string {
    if (minutes == null) return '--';
    return `${Math.floor(minutes / 60)}h ${minutes % 60}m`;
  }
}
