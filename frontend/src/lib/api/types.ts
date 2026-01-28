// API Types for Vehicle Search

export interface SearchRequest {
  query: string;
  sessionId?: string;
  maxResults?: number;
}

export interface SearchResults {
  results: VehicleResult[];
  totalCount: number;
  searchDuration: string;
  strategy: SearchStrategy;
}

export interface VehicleResult {
  vehicle: VehicleDocument;
  score: number;
  scoreBreakdown?: SearchScoreBreakdown;
  highlights?: string[];
}

export interface VehicleDocument {
  id: string;
  make: string;
  model: string;
  derivative?: string;
  bodyType?: string;
  price: number;
  mileage: number;
  engineSize?: number;
  fuelType: string;
  transmissionType: string;
  colour?: string;
  numberOfDoors?: number;
  registrationDate?: string;
  saleLocation: string;
  channel?: string;
  saleType?: string;
  features?: string[];
  grade?: string;
  serviceHistoryPresent?: boolean;
  numberOfServices?: number;
  lastServiceDate?: string;
  motExpiryDate?: string;
  vatType?: string;
  additionalInfo?: string;
  declarations?: string[];
  capRetailPrice?: number;
  capCleanPrice?: number;
  description?: string;
  processedDate?: string;
}

export interface SearchScoreBreakdown {
  exactMatchScore: number;
  semanticScore: number;
  keywordScore?: number;
  finalScore: number;
}

export interface SearchStrategy {
  name: string;
  description?: string;
}

export interface SessionResponse {
  sessionId: string;
  createdAt: string;
}

export interface ApiError {
  message: string;
  status?: number;
  details?: unknown;
}
