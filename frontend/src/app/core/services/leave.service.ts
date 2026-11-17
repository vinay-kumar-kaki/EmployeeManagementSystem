import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApplyLeaveRequest, LeaveRequest, LeaveType, ReviewLeaveRequest, SaveLeaveTypeRequest } from '../models/leave.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/leave`;

  getLeaveTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(`${this.apiUrl}/types`);
  }

  getAllTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(`${this.apiUrl}/types/all`);
  }

  createType(data: SaveLeaveTypeRequest): Observable<LeaveType> {
    return this.http.post<LeaveType>(`${this.apiUrl}/types`, data);
  }

  updateType(id: number, data: SaveLeaveTypeRequest): Observable<LeaveType> {
    return this.http.put<LeaveType>(`${this.apiUrl}/types/${id}`, data);
  }

  deactivateType(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/types/${id}`);
  }

  apply(data: ApplyLeaveRequest): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(`${this.apiUrl}/apply`, data);
  }

  getMyHistory(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/my-history`);
  }

  getPending(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/pending`);
  }

  getAll(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${this.apiUrl}/all`);
  }

  review(id: number, data: ReviewLeaveRequest): Observable<LeaveRequest> {
    return this.http.put<LeaveRequest>(`${this.apiUrl}/${id}/review`, data);
  }
}
