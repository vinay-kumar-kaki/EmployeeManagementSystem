/**
 * Production API configuration.
 *
 * The Angular SPA is served by the same ASP.NET Core process in production,
 * so a relative base URL keeps all requests same-origin (no CORS needed).
 */
export const environment = {
  production: true,
  apiBaseUrl: '/api'
};
