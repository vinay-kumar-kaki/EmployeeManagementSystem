import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const requiredRole = route.data?.['role'] as string;

  if (authService.isAuthenticated() && (!requiredRole || authService.hasRole(requiredRole))) {
    return true;
  }

  // Not authorized: redirect to dashboard
  router.navigate(['/dashboard']);
  return false;
};
