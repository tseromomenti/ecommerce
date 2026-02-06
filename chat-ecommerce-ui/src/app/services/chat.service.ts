import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ChatRequestModel, ChatResponseMessage, Product } from '../models/chat.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = environment.chatServiceApiUrl;

  constructor(private http: HttpClient) {}

  sendMessage(message: ChatRequestModel): Observable<ChatResponseMessage> {
    // Send in Microsoft.Extensions.AI.ChatMessage format
    const payload = {
      role: message.role || 'user',
      content: message.content 
    };
    
    return this.http.post<any>(`${this.apiUrl}/api/chat/message`, payload).pipe(
      map(response => {
        // Check if it's a ChatResponse with products (has message/type/data)
        if (response?.content !== undefined && response?.type !== undefined) {
          return {
            role: 'assistant' as const,
            content: response.content,
            type: response.type,
            data: response.data
          };
        }
        // Otherwise it's Microsoft.Extensions.AI.ChatResponse structure
        const assistantMsg = response?.messages?.find((m: any) => m.role === 'assistant');
        const textContent = assistantMsg?.contents?.find((c: any) => c.text)?.text || 'No response received.';
        return {
          role: 'assistant' as const,
          content: textContent,
          type: 'text' as const
        };
      })
    );
  }

  getProductDetails(productId: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/api/chat/product/${productId}`);
  }

  createOrder(productId: number, quantity: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/chat/order`, { productId, quantity });
  }
}
