import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatService } from '../../services/chat.service';
import { Product, QuizQuestion, QuizResult } from '../../models/chat.models';
import { ProductCardComponent } from '../product-card/product-card.component';

@Component({
  selector: 'app-quiz',
  standalone: true,
  imports: [CommonModule, ProductCardComponent],
  templateUrl: './quiz.component.html',
  styleUrl: './quiz.component.scss'
})
export class QuizComponent {
  sessionId = '';
  quizType: 'anime' | 'food' = 'anime';
  question: QuizQuestion | null = null;
  result: QuizResult | null = null;
  recommendedProducts: Product[] = [];
  isLoading = false;

  constructor(private chatService: ChatService) {}

  start(quizType: 'anime' | 'food'): void {
    this.quizType = quizType;
    this.result = null;
    this.recommendedProducts = [];
    this.isLoading = true;
    this.chatService.startQuiz(quizType).subscribe({
      next: (response) => {
        this.sessionId = response.sessionId;
        this.question = response.question;
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  answer(answerKey: string): void {
    if (!this.question) {
      return;
    }

    this.isLoading = true;
    this.chatService.answerQuiz(this.question.id, answerKey).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.completed) {
          this.question = null;
          this.result = response.result ?? null;
          this.recommendedProducts = response.recommendedProducts ?? [];
          return;
        }

        this.question = response.nextQuestion ?? null;
      },
      error: () => this.isLoading = false
    });
  }
}
