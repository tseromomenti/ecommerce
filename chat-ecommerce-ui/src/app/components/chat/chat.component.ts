import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../services/chat.service';
import { ChatMessage, Product } from '../../models/chat.models';
import { ProductCardComponent } from '../product-card/product-card.component';
import { OrderModalComponent } from '../order-modal/order-modal.component';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, ProductCardComponent, OrderModalComponent],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss'
})
export class ChatComponent implements OnInit {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef;

  messages: ChatMessage[] = [];
  userInput = '';
  isLoading = false;
  showOrderModal = false;
  selectedProduct: Product | null = null;

  constructor(private chatService: ChatService) {}

  ngOnInit(): void {
    this.messages.push({
      role: 'assistant',
      content: "Hi! I'm your AI shopping assistant. What are you looking for today? Just type what you need, like \"wireless mouse\" or \"gaming keyboard\".",
      type: 'text'
    });
  }

  sendMessage(): void {
    if (!this.userInput.trim() || this.isLoading) return;

    const userMessage: ChatMessage = {
      role: 'user',
      content: this.userInput,
      type: 'text'
    };
    this.messages.push(userMessage);
    
    const query = this.userInput;
    this.userInput = '';
    this.isLoading = true;

    this.chatService.sendMessage({ message: query, history: [] }).subscribe({
      next: (response) => {
        const assistantMessage: ChatMessage = {
          role: 'assistant',
          content: response.message,
          type: response.type,
          data: response.data
        };
        this.messages.push(assistantMessage);
        this.isLoading = false;
        this.scrollToBottom();
      },
      error: () => {
        this.messages.push({
          role: 'assistant',
          content: 'Sorry, something went wrong. Please try again.',
          type: 'text'
        });
        this.isLoading = false;
      }
    });
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  onBuyProduct(product: Product): void {
    this.selectedProduct = product;
    this.showOrderModal = true;
  }

  onCloseModal(): void {
    this.showOrderModal = false;
    this.selectedProduct = null;
  }

  onConfirmOrder(order: { productId: number; quantity: number }): void {
    this.chatService.createOrder(order.productId, order.quantity).subscribe({
      next: () => {
        this.messages.push({
          role: 'assistant',
          content: `✅ Order confirmed! ${order.quantity}x ${this.selectedProduct?.productName} for $${((this.selectedProduct?.price || 0) * order.quantity).toFixed(2)}`,
          type: 'text'
        });
        this.onCloseModal();
        this.scrollToBottom();
      },
      error: () => {
        this.messages.push({
          role: 'assistant',
          content: "❌ Sorry, we couldn't process your order. Please try again.",
          type: 'text'
        });
        this.onCloseModal();
      }
    });
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.messagesContainer) {
        this.messagesContainer.nativeElement.scrollTop = this.messagesContainer.nativeElement.scrollHeight;
      }
    }, 100);
  }
}
