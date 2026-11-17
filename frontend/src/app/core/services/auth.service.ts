import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { AuthResponse, CurrentUser, LoginRequest, RegisterAdminRequest } from '../models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiBaseUrl}/auth`;
  private readonly tokenKey = 'ems_auth_token';
  private readonly userKey = 'ems_current_user';

  private currentUserSubject = new BehaviorSubject<CurrentUser | null>(this.getStoredUser());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  public get currentUserValue(): CurrentUser | null {
    return this.currentUserSubject.value;
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        if (response.success && response.token) {
          localStorage.setItem(this.tokenKey, response.token);

          const user: CurrentUser = {
            userId: this.extractUserId(response.token),
            email: response.email || '',
            fullName: response.fullName || '',
            role: response.role || 'Employee',
            employeeId: response.employeeId
          };

          localStorage.setItem(this.userKey, JSON.stringify(user));
          this.currentUserSubject.next(user);
        }
      })
    );
  }

  registerAdmin(data: RegisterAdminRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register-admin`, data);
  }

  /** Re-reads the signed-in user from the API so identity/role state is server-authoritative. */
  getMe(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        localStorage.setItem(this.userKey, JSON.stringify(user));
        this.currentUserSubject.next(user);
      })
    );
  }

  /** Hydrates the session on app startup. Stale 401 sessions are cleared by the error interceptor. */
  hydrateCurrentUser(): void {
    if (!this.isAuthenticated()) return;
    this.getMe().subscribe({ error: () => undefined });
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;

    try {
      const decoded: any = jwtDecode(token);
      const currentTime = Math.floor(Date.now() / 1000);
      return decoded.exp > currentTime;
    } catch {
      return false;
    }
  }

  hasRole(expectedRole: string): boolean {
    const user = this.currentUserValue;
    return !!user && user.role.toLowerCase() === expectedRole.toLowerCase();
  }

  private extractUserId(token: string): string {
    try {
      const decoded: any = jwtDecode(token);
      return decoded.sub
        || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
        || '';
    } catch {
      return '';
    }
  }

  private getStoredUser(): CurrentUser | null {
    const userJson = localStorage.getItem(this.userKey);
    if (!userJson) return null;
    try {
      return JSON.parse(userJson) as CurrentUser;
    } catch {
      return null;
    }
  }
}
