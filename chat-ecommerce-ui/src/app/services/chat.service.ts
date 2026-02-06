import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, switchMap, map, catchError } from 'rxjs';
import {
  ChatRequestModel,
  ChatResponseMessage,
  Product,
  QuizAnswerResponse,
  QuizStartResponse
} from '../models/chat.models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = environment.chatServiceApiUrl;
  private sessionId: string | null = null;

  constructor(private http: HttpClient) {}

  sendMessage(message: ChatRequestModel): Observable<ChatResponseMessage> {
    return this.ensureSession().pipe(
      switchMap(sessionId => this.http.post<any>(`${this.apiUrl}/api/v1/chat/sessions/${sessionId}/messages`, {
        content: message.content
      })),
      map(response => ({
        role: 'assistant' as const,
        content: response.content || 'No response received.',
        type: response.type === 'clarification' ? 'text' : response.type,
        data: response.products ?? response.data ?? [],
        suggestedReplies: response.suggestedReplies ?? [],
        clarifyingQuestion: response.clarifyingQuestion,
        nextAction: response.nextAction,
        sessionId: response.sessionId
      })),
      catchError(() => this.sendLegacyMessage(message))
    );
  }

  startQuiz(quizType: string): Observable<QuizStartResponse> {
    return this.ensureSession().pipe(
      switchMap(sessionId => this.http.post<QuizStartResponse>(
        `${this.apiUrl}/api/v1/chat/sessions/${sessionId}/quiz/start?quizType=${encodeURIComponent(quizType)}`,
        {}
      ))
    );
  }

  answerQuiz(questionId: string, answerKey: string): Observable<QuizAnswerResponse> {
    if (!this.sessionId) {
      return of({ completed: false });
    }

    return this.http.post<QuizAnswerResponse>(
      `${this.apiUrl}/api/v1/chat/sessions/${this.sessionId}/quiz/answer`,
      { questionId, answerKey }
    );
  }

  getProductDetails(productId: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/api/chat/product/${productId}`);
  }

  createOrder(productId: number, quantity: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/chat/order`, { productId, quantity });
  }

  private ensureSession(): Observable<string> {
    if (this.sessionId) {
      return of(this.sessionId);
    }

    return this.http.post<{ sessionId: string }>(`${this.apiUrl}/api/v1/chat/sessions`, {}).pipe(
      map(response => {
        this.sessionId = response.sessionId;
        return response.sessionId;
      }),
      catchError(() => {
        // Legacy mode does not require a session.
        this.sessionId = 'legacy';
        return of('legacy');
      })
    );
  }

  private sendLegacyMessage(message: ChatRequestModel): Observable<ChatResponseMessage> {
    const payload = {
      role: message.role || 'user',
      content: message.content
    };

    return this.http.post<any>(`${this.apiUrl}/api/chat/message`, payload).pipe(
      map(response => ({
        role: 'assistant' as const,
        content: response?.content ?? response?.message ?? 'No response received.',
        type: response?.type ?? 'text',
        data: response?.data ?? []
      }))
    );
  }
}
