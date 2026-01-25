import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Product } from '../../models/chat.models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.scss'
})
export class ProductCardComponent {
  @Input() product!: Product;
  @Output() buyClicked = new EventEmitter<Product>();

  onBuyClick(): void {
    this.buyClicked.emit(this.product);
  }

  getImageUrl(product: Product): string {
    if (product.imageUrl) {
      if (product.imageUrl.startsWith('http')) {
        return product.imageUrl;
      }

      return `${environment.chatServiceApiUrl}${product.imageUrl}`;
    }

    const name = product.productName.toLowerCase();

    if (name.includes('mouse')) {
      return '/assets/products/mouse.svg';
    }

    if (name.includes('keyboard')) {
      return '/assets/products/keyboard.svg';
    }

    if (name.includes('monitor') || name.includes('display') || name.includes('screen')) {
      return '/assets/products/monitor.svg';
    }

    return '/assets/products/placeholder.svg';
  }
}
