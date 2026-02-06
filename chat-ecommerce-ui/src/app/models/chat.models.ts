export interface Product {
  id: number;
  sku?: string;
  productName: string;
  description?: string;
  category?: string;
  subcategory?: string;
  brand?: string;
  tags?: string;
  currencyCode?: string;
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
  suggestedReplies?: string[];
  clarifyingQuestion?: string;
  nextAction?: string;
  sessionId?: string;
}

export interface ChatRequestModel extends ChatMessageModel {

}

export interface QuizQuestionOption {
  key: string;
  label: string;
}

export interface QuizQuestion {
  id: string;
  prompt: string;
  options: QuizQuestionOption[];
}

export interface QuizStartResponse {
  sessionId: string;
  quizType: string;
  question: QuizQuestion;
}

export interface QuizResult {
  quizType: string;
  personaKey: string;
  personaLabel: string;
  confidence: number;
  recommendedTags: string[];
}

export interface QuizAnswerResponse {
  completed: boolean;
  nextQuestion?: QuizQuestion;
  result?: QuizResult;
  recommendedProducts?: Product[];
}
