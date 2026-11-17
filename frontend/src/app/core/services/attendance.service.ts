import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AttendanceRecord, AttendanceStatus } from '../models/attendance.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AttendanceService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/attendance`;

  getTodayStatus(): Observable<AttendanceStatus> {
    return this.http.get<AttendanceStatus>(`${this.apiUrl}/today-status`);
  }

  checkIn(): Observable<AttendanceStatus> {
    return this.http.post<AttendanceStatus>(`${this.apiUrl}/check-in`, {});
  }

  checkOut(): Observable<AttendanceStatus> {
    return this.http.post<AttendanceStatus>(`${this.apiUrl}/check-out`, {});
  }

  getMyHistory(): Observable<AttendanceRecord[]> {
    return this.http.get<AttendanceRecord[]>(`${this.apiUrl}/my-history`);
  }

  getAll(date?: string): Observable<AttendanceRecord[]> {
    const url = date ? `${this.apiUrl}/all?date=${date}` : `${this.apiUrl}/all`;
    return this.http.get<AttendanceRecord[]>(url);
  }
}
