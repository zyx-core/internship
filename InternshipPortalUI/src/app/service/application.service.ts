import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { jwtDecode } from 'jwt-decode'; // Import the decoder engine

@Injectable({
  providedIn: 'root'
})
export class ApplicationService {
  private httpClient = inject(HttpClient);
  private baseUrl = 'http://localhost:5023/api/admin/applications';

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('authToken');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  // Decodes the local storage JWT to read the user's Primary Key ID claim
  public getLoggedInUserId(): number {
    const token = localStorage.getItem('authToken');
    
    // SAFEGUARD: If no token is stored yet, exit immediately instead of crashing jwtDecode!
    if (!token || token.trim() === '') {
      return 0;
    }

    try {
      const decoded: any = jwtDecode(token);
      const userId = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || decoded['id'];
      return userId ? parseInt(userId, 10) : 0;
    } catch (error) {
      console.error('Failed parsing token mapping payload identity context', error);
      return 0;
    }
  }

  // 0. Fetch Eligible Smart Feed for Students (Safeguarded against unauthenticated boot cycles)
  public getSmartFeed(): Observable<any[]> {
    const studentId = this.getLoggedInUserId();
    
    if (studentId === 0) {
      console.warn('Smart Feed requested before an identity context was resolved.');
    }
    
    const headers = this.getAuthHeaders();
    return this.httpClient.get<any[]>(`${this.baseUrl}/student/${studentId}/smart-feed`, { headers });
  }

  // 0. Fetch Eligible Smart Feed for Students (Drives ID from Token dynamically)
  

  // 1. Fetch Paginated Applications Grid Rows (For Admin Panel)
  public getApplications(page: number, size: number): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.httpClient.get<any>(`${this.baseUrl}?pageNumber=${page}&pageSize=${size}`, { headers });
  }

  // 2. Process Status State Overrides (Approve / Reject)
  public updateStatus(id: number, status: string, reason: string | null = null): Observable<any> {
    const headers = this.getAuthHeaders();
    return this.httpClient.patch(`${this.baseUrl}/${id}/status`, { 
      status, 
      reason: reason || 'Processed via central administrative desk.' 
    }, { headers });
  }

  // 3. Transmit Form-Data Multi-part Certificate Payloads
  public uploadCertificate(id: number, file: File): Observable<any> {
    const headers = this.getAuthHeaders();
    const formData = new FormData();
    formData.append('file', file);
    return this.httpClient.post(`${this.baseUrl}/${id}/certificate/upload`, formData, { headers });
  }

  // 4. Request Stream Raw Binary Completion PDF Assets
  public downloadCertificate(id: number): Observable<Blob> {
    const headers = this.getAuthHeaders();
    return this.httpClient.get(`${this.baseUrl}/${id}/certificate/download`, { headers, responseType: 'blob' });
  }
}