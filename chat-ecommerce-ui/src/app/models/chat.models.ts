export interface Product {
  id: number;
  productName: string;
  imageUrl?: string;
  price: number;
  availableStock: number;
  relevanceScore?: number;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  type: 'text' | 'products';
  data?: Product[];
}

export interface ChatRequest {
  message: string;
  history: ChatMessage[];
}

export interface ChatResponse {
  message: string;
  type: 'text' | 'products';
  data?: Product[];
}
