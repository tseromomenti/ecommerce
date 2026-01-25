import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ChatRequest, ChatResponse, Product } from '../models/chat.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private apiUrl = environment.inventoryServiceApiUrl;

  constructor(private http: HttpClient) {}

  getProductDetails(productId: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/api/inventory//${productId}`);
  }
}