import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product } from '../models/chat.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private apiUrl = environment.inventoryServiceApiUrl;

  constructor(private http: HttpClient) {}

  getProductDetails(productId: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/api/v1/inventory/products/${productId}`);
  }

  getAdminProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.apiUrl}/api/v1/admin/products`);
  }

  createProduct(product: Product): Observable<Product> {
    return this.http.post<Product>(`${this.apiUrl}/api/v1/admin/products`, product);
  }

  updateProduct(productId: number, product: Product): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/api/v1/admin/products/${productId}`, product);
  }

  deleteProduct(productId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/api/v1/admin/products/${productId}`);
  }
}
