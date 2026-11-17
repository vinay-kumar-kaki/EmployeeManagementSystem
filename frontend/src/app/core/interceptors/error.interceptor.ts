import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Normalises ASP.NET Core error payloads into a single human-readable message.
 * Handles `{ message }`, ProblemDetails `{ title, errors }`, and plain strings.
 */
function extractErrorMessage(error: HttpErrorResponse): string {
  const body: any = error.error;

  if (typeof body === 'string' && body.trim().length > 0) {
    return body;
  }

  if (body && typeof body === 'object') {
    if (typeof body.message === 'string' && body.message.trim().length > 0) {
      return body.message;
    }

    if (body.errors && typeof body.errors === 'object') {
      const messages: string[] = [];
      Object.keys(body.errors).forEach(key => {
        const value = body.errors[key];
        if (Array.isArray(value)) {
          messages.push(...value.filter((v: unknown) => typeof v === 'string'));
        } else if (typeof value === 'string') {
          messages.push(value);
        }
      });
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (typeof body.title === 'string' && body.title.trim().length > 0) {
      return body.title;
    }
  }

  if (error.status === 0) {
    return 'Unable to reach the server. Please make sure the backend API is running.';
  }

  return error.message || 'An unexpected error occurred.';
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isLoginRequest = req.url.toLowerCase().endsWith('/auth/login');

      if (error.status === 401) {
        // Clear stale credentials, but do not redirect away from the login page.
        if (!isLoginRequest && authService.isAuthenticated()) {
          authService.logout();
        }
      } else if (error.status === 403) {
        router.navigate(['/dashboard']);
      }

      return throwError(() => new Error(extractErrorMessage(error)));
    })
  );
};
