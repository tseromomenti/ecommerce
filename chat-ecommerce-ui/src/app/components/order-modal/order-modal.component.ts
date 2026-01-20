import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Product } from '../../models/chat.models';

@Component({
  selector: 'app-order-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './order-modal.component.html',
  styleUrl: './order-modal.component.scss'
})
export class OrderModalComponent {
  @Input() product: Product | null = null;
  @Input() isVisible = false;
  @Output() close = new EventEmitter<void>();
  @Output() confirm = new EventEmitter<{ productId: number; quantity: number }>();

  quantity = 1;

  onClose(): void {
    this.close.emit();
  }

  onConfirm(): void {
    if (this.product && this.quantity > 0 && this.quantity <= this.product.availableStock) {
      this.confirm.emit({ productId: this.product.id, quantity: this.quantity });
    }
  }
}
