import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateDepartmentRequest, Department, UpdateDepartmentRequest } from '../models/department.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DepartmentService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/department`;

  getAll(activeOnly = false): Observable<Department[]> {
    return this.http.get<Department[]>(`${this.apiUrl}?activeOnly=${activeOnly}`);
  }

  getById(id: number): Observable<Department> {
    return this.http.get<Department>(`${this.apiUrl}/${id}`);
  }

  create(data: CreateDepartmentRequest): Observable<Department> {
    return this.http.post<Department>(this.apiUrl, data);
  }

  update(id: number, data: UpdateDepartmentRequest): Observable<Department> {
    return this.http.put<Department>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
