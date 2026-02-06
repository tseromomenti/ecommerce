import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, tap, map, catchError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, LoginRequest, MeResponse, RegisterRequest } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly accessTokenKey = 'ecom_access_token';
  private readonly refreshTokenKey = 'ecom_refresh_token';
  private readonly userKey = 'ecom_user';
  private readonly apiUrl = environment.authServiceApiUrl;

  private readonly userSubject = new BehaviorSubject<MeResponse | null>(this.loadUserFromStorage());
  user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {}

  login(payload: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/v1/auth/login`, payload).pipe(
      tap((response) => this.storeAuth(response))
    );
  }

  register(payload: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/v1/auth/register`, payload).pipe(
      tap((response) => this.storeAuth(response))
    );
  }

  refresh(): Observable<AuthResponse | null> {
    const refreshToken = localStorage.getItem(this.refreshTokenKey);
    if (!refreshToken) {
      return of(null);
    }

    return this.http.post<AuthResponse>(`${this.apiUrl}/api/v1/auth/refresh`, { refreshToken }).pipe(
      tap((response) => this.storeAuth(response)),
      catchError(() => {
        this.logout();
        return of(null);
      })
    );
  }

  loadMe(): Observable<MeResponse | null> {
    if (!this.isAuthenticated()) {
      return of(null);
    }

    return this.http.get<MeResponse>(`${this.apiUrl}/api/v1/auth/me`).pipe(
      tap((me) => {
        this.userSubject.next(me);
        localStorage.setItem(this.userKey, JSON.stringify(me));
      }),
      map((me) => me),
      catchError(() => of(null))
    );
  }

  logout(): void {
    const refreshToken = localStorage.getItem(this.refreshTokenKey);
    if (refreshToken && this.isAuthenticated()) {
      this.http.post(`${this.apiUrl}/api/v1/auth/logout`, { refreshToken }).subscribe({ error: () => {} });
    }

    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem(this.userKey);
    this.userSubject.next(null);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.accessTokenKey);
  }

  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  isAdmin(): boolean {
    const user = this.userSubject.value;
    return !!user?.roles?.includes('Admin');
  }

  private storeAuth(response: AuthResponse): void {
    localStorage.setItem(this.accessTokenKey, response.accessToken);
    localStorage.setItem(this.refreshTokenKey, response.refreshToken);

    const me: MeResponse = {
      userId: response.userId,
      email: response.email,
      displayName: response.displayName,
      roles: response.roles
    };

    localStorage.setItem(this.userKey, JSON.stringify(me));
    this.userSubject.next(me);
  }

  private loadUserFromStorage(): MeResponse | null {
    const raw = localStorage.getItem(this.userKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as MeResponse;
    } catch {
      return null;
    }
  }
}
