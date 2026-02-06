import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../services/chat.service';
import { ChatMessageModel, ChatRequestModel, ChatResponseMessage, Product } from '../../models/chat.models';
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

  messages: ChatMessageModel[] = [];
  userInput = '';
  isLoading = false;
  showOrderModal = false;
  selectedProduct: Product | null = null;

  constructor(private chatService: ChatService) {}

  ngOnInit(): void {
    this.messages.push({
      role: 'assistant',
      content: "Hi! I'm your AI shopping assistant. What are you looking for today? Just type what you need, like \"wireless mouse\" or \"gaming keyboard\".",
    });
  }

  sendMessage(): void {
    if (!this.userInput.trim() || this.isLoading) return;

    const userMessage: ChatRequestModel = {
      role: 'user',
      content: this.userInput,
    };
    this.messages.push(userMessage);
    
    const query = this.userInput;
    this.userInput = '';
    this.isLoading = true;

    this.chatService.sendMessage(userMessage).subscribe({
      next: (response) => {
        const assistantMessage: ChatResponseMessage = {
          content: response.content,
          role: response.role
        };
        this.messages.push(assistantMessage);
        this.isLoading = false;
        this.scrollToBottom();
      },
      error: () => {
        this.messages.push({
          content: 'Sorry, something went wrong. Please try again.',
          role: 'assistant'
        });
        this.isLoading = false;
      }
    });
  }

  onEnter(): void {
    this.sendMessage();
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
          content: `✅ Order confirmed! ${order.quantity}x ${this.selectedProduct?.productName} for $${((this.selectedProduct?.price || 0) * order.quantity).toFixed(2)}`,
          role: 'assistant'
        });
        this.onCloseModal();
        this.scrollToBottom();
      },
      error: () => {
        this.messages.push({
          content: "❌ Sorry, we couldn't process your order. Please try again.",
          role: 'assistant'
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
