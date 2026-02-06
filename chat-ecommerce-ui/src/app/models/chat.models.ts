export interface Product {
  id: number;
  productName: string;
  imageUrl?: string;
  price: number;
  availableStock: number;
  relevanceScore?: number;
}

export interface ChatMessageModel {
  content: string;
  role: 'user' | 'assistant';
}

export interface ChatResponseMessage extends ChatMessageModel {
  type?: 'text' | 'products';
  data?: Product[];
}

export interface ChatRequestModel extends ChatMessageModel {

}
