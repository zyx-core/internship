import { HttpInterceptorFn } from '@angular/common/http';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  // Pull your stored token out of browser storage
  const token = localStorage.getItem('authToken');

  // If a token exists, clone the request and inject the Bearer header
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  // Pass the request forward to the backend server pipeline
  return next(req);
};