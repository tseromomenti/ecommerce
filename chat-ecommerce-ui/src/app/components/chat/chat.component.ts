import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../services/chat.service';
import { CartResponse, OrderService } from '../../services/order.service';
import { ChatRequestModel, ChatResponseMessage, Product } from '../../models/chat.models';
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

  messages: ChatResponseMessage[] = [];
  userInput = '';
  isLoading = false;
  showOrderModal = false;
  selectedProduct: Product | null = null;
  cart: CartResponse | null = null;
  isCheckoutLoading = false;

  constructor(private chatService: ChatService, private orderService: OrderService) {}

  ngOnInit(): void {
    this.messages.push({
      role: 'assistant',
      content: "Hi! I'm your AI shopping assistant. What are you looking for today? Just type what you need, like \"wireless mouse\" or \"gaming keyboard\".",
    });
    this.refreshCart();
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
          role: response.role,
          type: response.type,
          data: response.data,
          suggestedReplies: response.suggestedReplies,
          clarifyingQuestion: response.clarifyingQuestion,
          nextAction: response.nextAction
        };
        this.messages.push(assistantMessage);
        if (response.clarifyingQuestion) {
          this.messages.push({
            content: response.clarifyingQuestion,
            role: 'assistant'
          });
        }
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

  onQuickReply(reply: string): void {
    this.userInput = reply;
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
    if (!this.selectedProduct) {
      return;
    }

    this.orderService.addCartItem(this.selectedProduct, order.quantity).subscribe({
      next: () => {
        this.messages.push({
          content: `Added ${order.quantity}x ${this.selectedProduct?.productName} to cart.`,
          role: 'assistant'
        });
        this.onCloseModal();
        this.refreshCart();
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

  removeCartItem(itemId: string): void {
    this.orderService.removeCartItem(itemId).subscribe({
      next: () => this.refreshCart()
    });
  }

  checkout(): void {
    this.isCheckoutLoading = true;
    this.orderService.checkout().subscribe({
      next: (response) => {
        this.isCheckoutLoading = false;
        this.refreshCart();
        this.messages.push({
          role: 'assistant',
          content: response.checkoutUrl
            ? `Checkout created. Complete payment here: ${response.checkoutUrl}`
            : `Order ${response.orderId} is pending payment.`
        });
        this.scrollToBottom();
      },
      error: () => {
        this.isCheckoutLoading = false;
        this.messages.push({
          role: 'assistant',
          content: 'Checkout failed. Please try again.'
        });
      }
    });
  }

  get cartItemCount(): number {
    return this.cart?.items?.reduce((acc, item) => acc + item.quantity, 0) ?? 0;
  }

  private refreshCart(): void {
    this.orderService.getCart().subscribe({
      next: (cart) => this.cart = cart,
      error: () => this.cart = null
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
