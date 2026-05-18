import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductDto } from '../services/cashier-api.service';

@Component({
  selector: 'app-products-pane',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="h-full flex flex-col bg-white">
      
      <!-- Top Filters -->
      <div class="p-4 border-b border-slate-200 shrink-0 space-y-3">
        <div class="relative">
          <input 
            type="text" 
            [(ngModel)]="searchTerm"
            (ngModelChange)="filterProducts()"
            placeholder="بحث عن منتج أو باركود..." 
            class="input-field pl-10"
          >
          <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
            <!-- Icon Search -->
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
          </span>
        </div>
      </div>

      <!-- Products Grid -->
      <div class="flex-1 overflow-y-auto p-4 bg-slate-50">
        <div *ngIf="isLoading" class="flex justify-center py-8">
          <svg class="animate-spin h-8 w-8 text-primary" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
          </svg>
        </div>

        <div *ngIf="!isLoading && filteredProducts.length === 0" class="text-center py-8 text-slate-500">
          لا توجد منتجات مطابقة للبحث
        </div>

        <div *ngIf="!isLoading && filteredProducts.length > 0" class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
          <div 
            *ngFor="let p of filteredProducts" 
            (click)="onProductClick(p)"
            class="bg-white border border-slate-200 rounded-lg p-3 cursor-pointer hover:border-primary hover:shadow-sm transition-all text-center flex flex-col"
          >
            <div class="text-xs text-slate-400 mb-1 line-clamp-1">{{ p.categoryName }}</div>
            <div class="font-bold text-slate-800 text-sm mb-2 flex-1 line-clamp-2">{{ p.name }}</div>
            <div class="text-primary font-semibold text-sm">{{ p.priceUsd | currency:'USD' }}</div>
          </div>
        </div>
      </div>
      
    </div>
  `
})
export class ProductsPaneComponent {
  private _allProducts: ProductDto[] = [];

  @Input() set products(val: ProductDto[]) {
    this._allProducts = val || [];
    this.filterProducts();
  }
  @Input() isLoading = false;
  
  @Output() productSelected = new EventEmitter<number>();
  @Output() barcodeEntered = new EventEmitter<string>(); // Optional if we detect full barcode in search

  searchTerm = '';
  filteredProducts: ProductDto[] = [];

  filterProducts() {
    const term = this.searchTerm.trim().toLowerCase();
    
    // Check if it's exactly a known barcode and could auto-add (optional UX, but let's stick to simple filter first)
    // Here we just filter the list
    if (!term) {
      this.filteredProducts = this._allProducts;
      return;
    }
    
    this.filteredProducts = this._allProducts.filter(p => 
      p.name.toLowerCase().includes(term) || p.barcode.toLowerCase() === term
    );
  }

  onProductClick(p: ProductDto) {
    this.productSelected.emit(p.id);
  }
}
