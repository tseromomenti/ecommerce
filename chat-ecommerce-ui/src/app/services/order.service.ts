import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../models/chat.models';
import { environment } from '../../environments/environment';

export interface CartItem {
  itemId: string;
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface CartResponse {
  userId: string;
  items: CartItem[];
  subtotal: number;
  currencyCode: string;
}

export interface CheckoutResponse {
  orderId: string;
  status: string;
  checkoutUrl?: string;
  paymentId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private apiUrl = environment.orderServiceApiUrl;

  constructor(private http: HttpClient) {}

  addCartItem(product: Product, quantity: number): Observable<CartResponse> {
    return this.http.post<CartResponse>(`${this.apiUrl}/api/v1/cart/items`, {
      productId: product.id,
      productName: product.productName,
      unitPrice: product.price,
      quantity
    });
  }

  getCart(): Observable<CartResponse> {
    return this.http.get<CartResponse>(`${this.apiUrl}/api/v1/cart`);
  }

  removeCartItem(itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/api/v1/cart/items/${itemId}`);
  }

  checkout(): Observable<CheckoutResponse> {
    return this.http.post<CheckoutResponse>(`${this.apiUrl}/api/v1/orders/checkout`, {
      currencyCode: 'USD',
      successUrl: `${window.location.origin}/chat`,
      cancelUrl: `${window.location.origin}/chat`
    });
  }
}
