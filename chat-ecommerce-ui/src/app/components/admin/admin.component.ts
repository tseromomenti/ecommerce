import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InventoryService } from '../../services/inventory.service';
import { Product } from '../../models/chat.models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss'
})
export class AdminComponent implements OnInit {
  products: Product[] = [];
  draft: Product = {
    id: 0,
    productName: '',
    price: 0,
    availableStock: 0,
    description: '',
    category: 'clothing',
    brand: '',
    currencyCode: 'USD'
  };
  isLoading = false;
  error = '';

  constructor(private inventoryService: InventoryService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.inventoryService.getAdminProducts().subscribe({
      next: (products) => {
        this.products = products;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Unable to load products.';
        this.isLoading = false;
      }
    });
  }

  addProduct(): void {
    this.inventoryService.createProduct(this.draft).subscribe({
      next: () => {
        this.draft = {
          id: 0,
          productName: '',
          price: 0,
          availableStock: 0,
          description: '',
          category: 'clothing',
          brand: '',
          currencyCode: 'USD'
        };
        this.load();
      },
      error: () => this.error = 'Failed to add product.'
    });
  }

  deleteProduct(productId: number): void {
    this.inventoryService.deleteProduct(productId).subscribe({
      next: () => this.load(),
      error: () => this.error = 'Failed to delete product.'
    });
  }
}
