import { ApplicationConfig } from '@angular/core';
import { provideRouter, withDebugTracing } from '@angular/router'; // Added withDebugTracing
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { jwtInterceptor } from './core/interceptors/jwt';

export const appConfig: ApplicationConfig = {
  providers: [
    // Turn on deep terminal logging for every single route event attempt
    provideRouter(routes, withDebugTracing()), 
    provideHttpClient(withInterceptors([jwtInterceptor]))
  ]
};