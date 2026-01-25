import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ChatMessage, ChatResponse, Product } from '../models/chat.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = environment.chatServiceApiUrl;

  constructor(private http: HttpClient) {}

  sendMessage(message: ChatMessage): Observable<ChatMessage> {
    return this.http.post<ChatMessage>(`${this.apiUrl}/api/chat/message`, message);
  }

  getProductDetails(productId: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/api/chat/product/${productId}`);
  }

  createOrder(productId: number, quantity: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/chat/order`, { productId, quantity });
  }
}
