import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateEmployeeRequest, Employee, EmployeeProfile, UpdateEmployeeRequest } from '../models/employee.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/employee`;

  getAll(search = '', departmentId?: number): Observable<Employee[]> {
    let url = `${this.apiUrl}?search=${encodeURIComponent(search)}`;
    if (departmentId) {
      url += `&departmentId=${departmentId}`;
    }
    return this.http.get<Employee[]>(url);
  }

  getById(id: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.apiUrl}/${id}`);
  }

  getMyProfile(): Observable<EmployeeProfile> {
    return this.http.get<EmployeeProfile>(`${this.apiUrl}/profile/me`);
  }

  create(data: CreateEmployeeRequest): Observable<Employee> {
    return this.http.post<Employee>(this.apiUrl, data);
  }

  update(id: number, data: UpdateEmployeeRequest): Observable<Employee> {
    return this.http.put<Employee>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
