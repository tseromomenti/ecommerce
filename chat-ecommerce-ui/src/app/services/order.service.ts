import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../models/chat.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private apiUrl = environment.orderServiceApiUrl;

  constructor(private http: HttpClient) {}

  createOrder(productId: number, quantity: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/order`, { productId, quantity });
  }
}