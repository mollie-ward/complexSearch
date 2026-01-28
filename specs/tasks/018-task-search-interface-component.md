# Task: Search Interface Component

**Task ID:** 018  
**Feature:** Frontend Search Interface  
**Type:** Frontend Implementation  
**Priority:** Critical  
**Estimated Complexity:** Medium  
**FRD Reference:** All FRDs (user-facing interface)  
**GitHub Issue:** [#37](https://github.com/mollie-ward/complexSearch/issues/37)

---

## Description

Implement the main search interface component with natural language input, real-time search, result display with explanations, and conversational refinement capabilities.

---

## Dependencies

**Depends on:**
- Task 002: Frontend Scaffolding
- Task 014: Search Strategy Selection (backend API)
- Task 015: Result Ranking (for ranked results)

**Blocks:**
- Task 019: Results Display & Interaction
- Task 020: End-to-End Integration Tests

---

## Technical Requirements

### Component Structure

```typescript
// app/search/page.tsx
'use client';

import { useState, useEffect } from 'react';
import { SearchInput } from '@/components/search/SearchInput';
import { ResultsList } from '@/components/search/ResultsList';
import { ConversationHistory } from '@/components/search/ConversationHistory';
import { useSearch } from '@/lib/hooks/useSearch';
import { useSession } from '@/lib/hooks/useSession';

export default function SearchPage() {
  const { session, createSession } = useSession();
  const { search, results, isLoading, error } = useSearch(session?.sessionId);
  
  useEffect(() => {
    if (!session) {
      createSession();
    }
  }, [session, createSession]);
  
  const handleSearch = async (query: string) => {
    await search(query);
  };
  
  return (
    <div className="container mx-auto px-4 py-8">
      <header className="mb-8">
        <h1 className="text-4xl font-bold mb-2">Vehicle Search</h1>
        <p className="text-muted-foreground">
          Find your perfect vehicle using natural language
        </p>
      </header>
      
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main search area */}
        <div className="lg:col-span-2 space-y-6">
          <SearchInput
            onSearch={handleSearch}
            isLoading={isLoading}
            placeholder="e.g., reliable BMW under £20k with low mileage"
          />
          
          {error && (
            <div className="bg-destructive/15 text-destructive px-4 py-3 rounded-md">
              {error}
            </div>
          )}
          
          {results && (
            <ResultsList
              results={results.results}
              totalCount={results.totalCount}
              searchDuration={results.searchDuration}
            />
          )}
        </div>
        
        {/* Sidebar: conversation history */}
        <div className="lg:col-span-1">
          {session && (
            <ConversationHistory sessionId={session.sessionId} />
          )}
        </div>
      </div>
    </div>
  );
}
```

### Search Input Component

```typescript
// components/search/SearchInput.tsx
'use client';

import { useState, useRef, useEffect } from 'react';
import { Search, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';

interface SearchInputProps {
  onSearch: (query: string) => Promise<void>;
  isLoading: boolean;
  placeholder?: string;
}

export function SearchInput({ onSearch, isLoading, placeholder }: SearchInputProps) {
  const [query, setQuery] = useState('');
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim() && !isLoading) {
      await onSearch(query.trim());
      setQuery('');  // Clear input after search
    }
  };
  
  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    // Submit on Enter (but not Shift+Enter for multiline)
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e as any);
    }
  };
  
  // Auto-resize textarea
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  }, [query]);
  
  return (
    <form onSubmit={handleSubmit} className="relative">
      <div className="relative">
        <Textarea
          ref={textareaRef}
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder || "Describe the vehicle you're looking for..."}
          disabled={isLoading}
          className="min-h-[100px] max-h-[300px] pr-12 resize-none"
          rows={3}
        />
        <Button
          type="submit"
          size="icon"
          disabled={!query.trim() || isLoading}
          className="absolute right-2 bottom-2"
        >
          {isLoading ? (
            <Loader2 className="h-5 w-5 animate-spin" />
          ) : (
            <Search className="h-5 w-5" />
          )}
        </Button>
      </div>
      
      {/* Example queries */}
      <div className="mt-2 flex flex-wrap gap-2">
        <span className="text-sm text-muted-foreground">Try:</span>
        {exampleQueries.map((example, i) => (
          <button
            key={i}
            type="button"
            onClick={() => setQuery(example)}
            disabled={isLoading}
            className="text-sm text-primary hover:underline disabled:opacity-50"
          >
            {example}
          </button>
        ))}
      </div>
    </form>
  );
}

const exampleQueries = [
  "Reliable BMW under £20k",
  "Economical family car",
  "Sporty convertible with low mileage"
];
```

### Results List Component

```typescript
// components/search/ResultsList.tsx
'use client';

import { VehicleCard } from './VehicleCard';
import { SearchMetadata } from './SearchMetadata';
import type { SearchResults } from '@/lib/api/types';

interface ResultsListProps {
  results: SearchResults['results'];
  totalCount: number;
  searchDuration: string;
}

export function ResultsList({ results, totalCount, searchDuration }: ResultsListProps) {
  if (!results || results.length === 0) {
    return (
      <div className="text-center py-12 border rounded-lg bg-muted/50">
        <p className="text-muted-foreground">No vehicles found matching your criteria.</p>
        <p className="text-sm text-muted-foreground mt-2">
          Try adjusting your search or being less specific.
        </p>
      </div>
    );
  }
  
  return (
    <div className="space-y-4">
      <SearchMetadata
        count={results.length}
        totalCount={totalCount}
        duration={searchDuration}
      />
      
      <div className="space-y-4">
        {results.map((result, index) => (
          <VehicleCard
            key={result.vehicle.id}
            vehicle={result.vehicle}
            relevanceScore={result.relevanceScore}
            scoreBreakdown={result.scoreBreakdown}
            rank={index + 1}
          />
        ))}
      </div>
    </div>
  );
}
```

### Vehicle Card Component

```typescript
// components/search/VehicleCard.tsx
'use client';

import { useState } from 'react';
import { Car, Gauge, Calendar, MapPin, Info } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import type { VehicleResult } from '@/lib/api/types';

interface VehicleCardProps {
  vehicle: VehicleResult['vehicle'];
  relevanceScore: number;
  scoreBreakdown: VehicleResult['scoreBreakdown'];
  rank: number;
}

export function VehicleCard({ vehicle, relevanceScore, scoreBreakdown, rank }: VehicleCardProps) {
  const [showExplanation, setShowExplanation] = useState(false);
  
  return (
    <Card className="hover:shadow-lg transition-shadow">
      <CardHeader>
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <div className="flex items-center gap-2 mb-1">
              <Badge variant="secondary">#{rank}</Badge>
              <CardTitle className="text-xl">
                {vehicle.make} {vehicle.model}
              </CardTitle>
            </div>
            {vehicle.derivative && (
              <CardDescription>{vehicle.derivative}</CardDescription>
            )}
          </div>
          
          <div className="text-right">
            <p className="text-2xl font-bold text-primary">
              £{vehicle.price.toLocaleString()}
            </p>
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <div className="text-sm text-muted-foreground flex items-center gap-1">
                    <span>Match: {Math.round(relevanceScore * 100)}%</span>
                    <Info className="h-3 w-3" />
                  </div>
                </TooltipTrigger>
                <TooltipContent>
                  <div className="space-y-1 text-xs">
                    <p>Semantic: {Math.round(scoreBreakdown.semanticScore * 100)}%</p>
                    <p>Exact Match: {Math.round(scoreBreakdown.exactMatchScore * 100)}%</p>
                  </div>
                </TooltipContent>
              </Tooltip>
            </TooltipProvider>
          </div>
        </div>
      </CardHeader>
      
      <CardContent>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
          <div className="flex items-center gap-2 text-sm">
            <Gauge className="h-4 w-4 text-muted-foreground" />
            <span>{vehicle.mileage?.toLocaleString()} miles</span>
          </div>
          <div className="flex items-center gap-2 text-sm">
            <Calendar className="h-4 w-4 text-muted-foreground" />
            <span>{new Date(vehicle.registrationDate).getFullYear()}</span>
          </div>
          <div className="flex items-center gap-2 text-sm">
            <Car className="h-4 w-4 text-muted-foreground" />
            <span>{vehicle.transmission}</span>
          </div>
          <div className="flex items-center gap-2 text-sm">
            <MapPin className="h-4 w-4 text-muted-foreground" />
            <span>{vehicle.saleLocation}</span>
          </div>
        </div>
        
        {vehicle.description && (
          <p className="text-sm text-muted-foreground mb-4 line-clamp-2">
            {vehicle.description}
          </p>
        )}
        
        {vehicle.features && vehicle.features.length > 0 && (
          <div className="flex flex-wrap gap-2 mb-4">
            {vehicle.features.slice(0, 5).map((feature, i) => (
              <Badge key={i} variant="outline" className="text-xs">
                {feature}
              </Badge>
            ))}
            {vehicle.features.length > 5 && (
              <Badge variant="outline" className="text-xs">
                +{vehicle.features.length - 5} more
              </Badge>
            )}
          </div>
        )}
        
        <div className="flex gap-2">
          <Button variant="default" className="flex-1">
            View Details
          </Button>
          <Button
            variant="outline"
            onClick={() => setShowExplanation(!showExplanation)}
          >
            {showExplanation ? 'Hide' : 'Why this match?'}
          </Button>
        </div>
        
        {showExplanation && (
          <div className="mt-4 p-4 bg-muted rounded-md text-sm space-y-2">
            <p className="font-semibold">Match Explanation:</p>
            <ul className="space-y-1 list-disc list-inside">
              {scoreBreakdown.exactMatchScore > 0 && (
                <li>Meets your exact criteria (make, price, etc.)</li>
              )}
              {scoreBreakdown.semanticScore > 0.7 && (
                <li>Strongly matches your conceptual requirements</li>
              )}
              {vehicle.serviceHistoryPresent && (
                <li>Full service history available</li>
              )}
              {vehicle.mileage < 50000 && (
                <li>Low mileage for added reliability</li>
              )}
            </ul>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
```

### Search Hook

```typescript
// lib/hooks/useSearch.ts
'use client';

import { useState, useCallback } from 'react';
import { searchVehicles } from '@/lib/api/search';
import type { SearchResults } from '@/lib/api/types';

export function useSearch(sessionId?: string) {
  const [results, setResults] = useState<SearchResults | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const search = useCallback(async (query: string) => {
    if (!sessionId) {
      setError('No active session');
      return;
    }
    
    setIsLoading(true);
    setError(null);
    
    try {
      const data = await searchVehicles({ query, sessionId });
      setResults(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Search failed');
      setResults(null);
    } finally {
      setIsLoading(false);
    }
  }, [sessionId]);
  
  return { search, results, isLoading, error };
}
```

### API Client

```typescript
// lib/api/search.ts
import { apiClient } from './client';
import type { SearchRequest, SearchResults } from './types';

export async function searchVehicles(request: SearchRequest): Promise<SearchResults> {
  const response = await apiClient.post<SearchResults>('/search', request);
  return response.data;
}

export async function getVehicleById(id: string): Promise<Vehicle> {
  const response = await apiClient.get<Vehicle>(`/vehicles/${id}`);
  return response.data;
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Search Input:**
- [ ] Natural language input working
- [ ] Auto-resize textarea
- [ ] Enter to submit (Shift+Enter for newline)
- [ ] Example queries clickable
- [ ] Loading state shown

✅ **Results Display:**
- [ ] All vehicle details shown
- [ ] Relevance scores displayed
- [ ] Match explanations available
- [ ] Responsive layout (mobile/desktop)

✅ **Interaction:**
- [ ] "View Details" navigates to detail page
- [ ] "Why this match?" toggles explanation
- [ ] Conversation history visible
- [ ] Real-time search updates

### Technical Criteria

✅ **Performance:**
- [ ] Initial load <2 seconds
- [ ] Search results render <500ms
- [ ] Smooth scrolling and interactions

✅ **Accessibility:**
- [ ] Keyboard navigation works
- [ ] Screen reader compatible
- [ ] ARIA labels present
- [ ] Color contrast passes WCAG AA

---

## Testing Requirements

### Component Tests

**Test Coverage:** ≥80%

**Test Cases:**
```typescript
describe('SearchInput', () => {
  it('calls onSearch when form is submitted', async () => {
    // Test
  });
  
  it('clears input after successful search', async () => {
    // Test
  });
  
  it('shows loading state during search', () => {
    // Test
  });
});

describe('VehicleCard', () => {
  it('renders vehicle details correctly', () => {
    // Test
  });
  
  it('toggles explanation on button click', () => {
    // Test
  });
});
```

### E2E Tests (with Playwright)

```typescript
test('complete search flow', async ({ page }) => {
  await page.goto('/search');
  await page.fill('textarea', 'BMW under £20k');
  await page.click('button[type="submit"]');
  await expect(page.locator('.vehicle-card')).toBeVisible();
});
```

---

## Definition of Done

- [ ] Search interface component implemented
- [ ] Search input with examples working
- [ ] Results list with cards implemented
- [ ] Match explanations functional
- [ ] Conversation history visible
- [ ] API integration complete
- [ ] All component tests pass (≥80% coverage)
- [ ] E2E tests pass
- [ ] Responsive design works (mobile/tablet/desktop)
- [ ] Accessibility audit passed
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `app/search/page.tsx`
- `components/search/SearchInput.tsx`
- `components/search/ResultsList.tsx`
- `components/search/VehicleCard.tsx`
- `components/search/SearchMetadata.tsx`
- `components/search/ConversationHistory.tsx`
- `lib/hooks/useSearch.ts`
- `lib/api/search.ts`
- `__tests__/components/search/SearchInput.test.tsx`

**References:**
- All FRDs (frontend is user-facing interface for all features)
- Task 002: Frontend scaffolding
