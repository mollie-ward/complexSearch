# Task: Results Display & Interaction

**Task ID:** 019  
**Feature:** Frontend Search Interface  
**Type:** Frontend Implementation  
**Priority:** High  
**Estimated Complexity:** Medium  
**FRD Reference:** All FRDs (user interaction)

---

## Description

Implement interactive results display with vehicle detail views, comparison functionality, refinement controls, and conversation management features.

---

## Dependencies

**Depends on:**
- Task 018: Search Interface Component
- Task 012: Session Context Storage (for conversation)
- Task 013: Reference Resolution (for refinement)

**Blocks:**
- Task 020: End-to-End Integration Tests

---

## Technical Requirements

### Vehicle Detail Page

```typescript
// app/vehicles/[id]/page.tsx
'use client';

import { use, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Share2, Heart } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { VehicleDetails } from '@/components/vehicles/VehicleDetails';
import { VehicleImages } from '@/components/vehicles/VehicleImages';
import { VehicleSpecifications } from '@/components/vehicles/VehicleSpecifications';
import { SimilarVehicles } from '@/components/vehicles/SimilarVehicles';
import { getVehicleById } from '@/lib/api/search';

interface VehiclePageProps {
  params: Promise<{ id: string }>;
}

export default function VehiclePage({ params }: VehiclePageProps) {
  const { id } = use(params);
  const router = useRouter();
  const [vehicle, setVehicle] = useState<Vehicle | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  
  useEffect(() => {
    getVehicleById(id)
      .then(setVehicle)
      .catch(console.error)
      .finally(() => setIsLoading(false));
  }, [id]);
  
  if (isLoading) return <div>Loading...</div>;
  if (!vehicle) return <div>Vehicle not found</div>;
  
  return (
    <div className="container mx-auto px-4 py-8">
      <Button
        variant="ghost"
        onClick={() => router.back()}
        className="mb-4"
      >
        <ArrowLeft className="mr-2 h-4 w-4" />
        Back to results
      </Button>
      
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Main content */}
        <div className="lg:col-span-2 space-y-6">
          <VehicleImages images={vehicle.images || []} />
          <VehicleDetails vehicle={vehicle} />
          <VehicleSpecifications vehicle={vehicle} />
        </div>
        
        {/* Sidebar */}
        <div className="lg:col-span-1 space-y-6">
          <PriceCard price={vehicle.price} />
          <ContactCard location={vehicle.saleLocation} />
          <SimilarVehicles vehicleId={vehicle.id} />
        </div>
      </div>
    </div>
  );
}
```

### Conversation History Component

```typescript
// components/search/ConversationHistory.tsx
'use client';

import { useState, useEffect } from 'react';
import { MessageCircle, Trash2 } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { getConversationHistory, clearConversation } from '@/lib/api/conversation';
import type { ConversationHistory } from '@/lib/api/types';

interface ConversationHistoryProps {
  sessionId: string;
}

export function ConversationHistory({ sessionId }: ConversationHistoryProps) {
  const [history, setHistory] = useState<ConversationHistory | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  
  useEffect(() => {
    loadHistory();
  }, [sessionId]);
  
  const loadHistory = async () => {
    try {
      const data = await getConversationHistory(sessionId);
      setHistory(data);
    } catch (error) {
      console.error('Failed to load history:', error);
    } finally {
      setIsLoading(false);
    }
  };
  
  const handleClear = async () => {
    if (confirm('Clear conversation history?')) {
      await clearConversation(sessionId);
      setHistory({ sessionId, messages: [], totalMessages: 0 });
    }
  };
  
  if (isLoading) return <div>Loading history...</div>;
  if (!history || history.messages.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MessageCircle className="h-5 w-5" />
            Conversation
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            Your search history will appear here
          </p>
        </CardContent>
      </Card>
    );
  }
  
  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center gap-2">
            <MessageCircle className="h-5 w-5" />
            Conversation ({history.totalMessages})
          </CardTitle>
          <Button
            variant="ghost"
            size="icon"
            onClick={handleClear}
            title="Clear history"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        <ScrollArea className="h-[400px] pr-4">
          <div className="space-y-4">
            {history.messages.map((message, index) => (
              <div
                key={message.messageId}
                className={`flex ${
                  message.role === 'User' ? 'justify-end' : 'justify-start'
                }`}
              >
                <div
                  className={`rounded-lg px-4 py-2 max-w-[80%] ${
                    message.role === 'User'
                      ? 'bg-primary text-primary-foreground'
                      : 'bg-muted'
                  }`}
                >
                  <p className="text-sm">{message.content}</p>
                  {message.results && (
                    <p className="text-xs mt-1 opacity-80">
                      {message.results.count} results
                    </p>
                  )}
                </div>
              </div>
            ))}
          </div>
        </ScrollArea>
      </CardContent>
    </Card>
  );
}
```

### Refinement Controls

```typescript
// components/search/RefinementControls.tsx
'use client';

import { useState } from 'react';
import { SlidersHorizontal } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet';
import { Label } from '@/components/ui/label';
import { Slider } from '@/components/ui/slider';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

interface RefinementControlsProps {
  onRefine: (filters: RefinementFilters) => void;
}

export interface RefinementFilters {
  priceRange?: [number, number];
  mileageRange?: [number, number];
  make?: string;
  fuelType?: string;
  transmission?: string;
  yearMin?: number;
}

export function RefinementControls({ onRefine }: RefinementControlsProps) {
  const [filters, setFilters] = useState<RefinementFilters>({});
  
  const handleApply = () => {
    onRefine(filters);
  };
  
  return (
    <Sheet>
      <SheetTrigger asChild>
        <Button variant="outline" size="sm">
          <SlidersHorizontal className="mr-2 h-4 w-4" />
          Refine Results
        </Button>
      </SheetTrigger>
      <SheetContent>
        <SheetHeader>
          <SheetTitle>Refine Your Search</SheetTitle>
        </SheetHeader>
        
        <div className="space-y-6 mt-6">
          {/* Price Range */}
          <div className="space-y-2">
            <Label>Price Range</Label>
            <Slider
              min={0}
              max={50000}
              step={1000}
              value={filters.priceRange || [0, 50000]}
              onValueChange={(value) => setFilters({ ...filters, priceRange: value as [number, number] })}
            />
            <div className="flex justify-between text-sm text-muted-foreground">
              <span>£{(filters.priceRange?.[0] || 0).toLocaleString()}</span>
              <span>£{(filters.priceRange?.[1] || 50000).toLocaleString()}</span>
            </div>
          </div>
          
          {/* Mileage Range */}
          <div className="space-y-2">
            <Label>Mileage Range</Label>
            <Slider
              min={0}
              max={150000}
              step={5000}
              value={filters.mileageRange || [0, 150000]}
              onValueChange={(value) => setFilters({ ...filters, mileageRange: value as [number, number] })}
            />
            <div className="flex justify-between text-sm text-muted-foreground">
              <span>{(filters.mileageRange?.[0] || 0).toLocaleString()} mi</span>
              <span>{(filters.mileageRange?.[1] || 150000).toLocaleString()} mi</span>
            </div>
          </div>
          
          {/* Make */}
          <div className="space-y-2">
            <Label>Make</Label>
            <Select value={filters.make} onValueChange={(value) => setFilters({ ...filters, make: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Any make" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Any make</SelectItem>
                <SelectItem value="BMW">BMW</SelectItem>
                <SelectItem value="Mercedes-Benz">Mercedes-Benz</SelectItem>
                <SelectItem value="Audi">Audi</SelectItem>
                <SelectItem value="Toyota">Toyota</SelectItem>
                {/* Add more makes */}
              </SelectContent>
            </Select>
          </div>
          
          {/* Fuel Type */}
          <div className="space-y-2">
            <Label>Fuel Type</Label>
            <Select value={filters.fuelType} onValueChange={(value) => setFilters({ ...filters, fuelType: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Any fuel type" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Any fuel type</SelectItem>
                <SelectItem value="Petrol">Petrol</SelectItem>
                <SelectItem value="Diesel">Diesel</SelectItem>
                <SelectItem value="Electric">Electric</SelectItem>
                <SelectItem value="Hybrid">Hybrid</SelectItem>
              </SelectContent>
            </Select>
          </div>
          
          {/* Transmission */}
          <div className="space-y-2">
            <Label>Transmission</Label>
            <Select value={filters.transmission} onValueChange={(value) => setFilters({ ...filters, transmission: value })}>
              <SelectTrigger>
                <SelectValue placeholder="Any transmission" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Any transmission</SelectItem>
                <SelectItem value="Manual">Manual</SelectItem>
                <SelectItem value="Automatic">Automatic</SelectItem>
              </SelectContent>
            </Select>
          </div>
          
          <Button onClick={handleApply} className="w-full">
            Apply Filters
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}
```

### Vehicle Comparison

```typescript
// components/search/ComparisonView.tsx
'use client';

import { useState } from 'react';
import { X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import type { Vehicle } from '@/lib/api/types';

interface ComparisonViewProps {
  vehicles: Vehicle[];
  onRemove: (id: string) => void;
  onClose: () => void;
}

export function ComparisonView({ vehicles, onRemove, onClose }: ComparisonViewProps) {
  const attributes = [
    { key: 'price', label: 'Price', format: (v: number) => `£${v.toLocaleString()}` },
    { key: 'mileage', label: 'Mileage', format: (v: number) => `${v.toLocaleString()} mi` },
    { key: 'year', label: 'Year', format: (v: number) => v.toString() },
    { key: 'fuelType', label: 'Fuel Type', format: (v: string) => v },
    { key: 'transmission', label: 'Transmission', format: (v: string) => v },
    { key: 'engineSize', label: 'Engine', format: (v: number) => `${v}L` },
  ];
  
  return (
    <Card className="fixed bottom-0 left-0 right-0 z-50 rounded-t-lg shadow-lg max-h-[400px] overflow-auto">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Compare Vehicles ({vehicles.length})</CardTitle>
        <Button variant="ghost" size="icon" onClick={onClose}>
          <X className="h-4 w-4" />
        </Button>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto">
          <table className="w-full border-collapse">
            <thead>
              <tr>
                <th className="text-left p-2 border-b">Attribute</th>
                {vehicles.map((vehicle) => (
                  <th key={vehicle.id} className="text-left p-2 border-b min-w-[200px]">
                    <div className="flex items-start justify-between">
                      <div>
                        <p className="font-semibold">{vehicle.make} {vehicle.model}</p>
                        <p className="text-sm text-muted-foreground">{vehicle.derivative}</p>
                      </div>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => onRemove(vehicle.id)}
                      >
                        <X className="h-4 w-4" />
                      </Button>
                    </div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {attributes.map((attr) => (
                <tr key={attr.key}>
                  <td className="p-2 border-b font-medium">{attr.label}</td>
                  {vehicles.map((vehicle) => (
                    <td key={vehicle.id} className="p-2 border-b">
                      {attr.format(vehicle[attr.key as keyof Vehicle] as any)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}
```

### Quick Refinement Suggestions

```typescript
// components/search/RefinementSuggestions.tsx
'use client';

import { Badge } from '@/components/ui/badge';

interface RefinementSuggestionsProps {
  onSuggest: (suggestion: string) => void;
}

export function RefinementSuggestions({ onSuggest }: RefinementSuggestionsProps) {
  const suggestions = [
    'Show me cheaper ones',
    'With lower mileage',
    'Only automatic',
    'From 2020 or newer',
    'With leather seats',
    'In Manchester'
  ];
  
  return (
    <div className="flex flex-wrap gap-2">
      <span className="text-sm text-muted-foreground">Refine:</span>
      {suggestions.map((suggestion, i) => (
        <Badge
          key={i}
          variant="secondary"
          className="cursor-pointer hover:bg-secondary/80"
          onClick={() => onSuggest(suggestion)}
        >
          {suggestion}
        </Badge>
      ))}
    </div>
  );
}
```

---

## Acceptance Criteria

### Functional Criteria

✅ **Vehicle Details:**
- [ ] Full vehicle information displayed
- [ ] Image gallery functional
- [ ] Specifications table shown
- [ ] Similar vehicles suggested

✅ **Conversation:**
- [ ] History shows all messages
- [ ] User/Assistant roles distinguished
- [ ] Clear button works
- [ ] Auto-scrolls to latest

✅ **Refinement:**
- [ ] Slider controls work
- [ ] Dropdowns update filters
- [ ] Apply filters triggers search
- [ ] Quick suggestions clickable

✅ **Comparison:**
- [ ] Up to 3 vehicles compared
- [ ] Side-by-side table view
- [ ] Remove vehicle works
- [ ] Close comparison works

### Technical Criteria

✅ **Performance:**
- [ ] Detail page loads <1 second
- [ ] Smooth scrolling
- [ ] No layout shifts

✅ **Accessibility:**
- [ ] Keyboard navigation
- [ ] Screen reader compatible
- [ ] Focus indicators visible

---

## Testing Requirements

### Component Tests

**Test Coverage:** ≥80%

**Test Cases:**
```typescript
describe('ConversationHistory', () => {
  it('renders messages in correct order', () => {
    // Test
  });
  
  it('clears history when clear button clicked', async () => {
    // Test
  });
});

describe('RefinementControls', () => {
  it('applies filters when button clicked', () => {
    // Test
  });
});
```

---

## Definition of Done

- [ ] Vehicle detail page implemented
- [ ] Conversation history working
- [ ] Refinement controls functional
- [ ] Comparison view working
- [ ] Quick suggestions implemented
- [ ] All component tests pass (≥80% coverage)
- [ ] E2E tests pass
- [ ] Responsive design works
- [ ] Accessibility audit passed
- [ ] Documentation updated
- [ ] Code reviewed and approved

---

## Related Files

**Created/Modified:**
- `app/vehicles/[id]/page.tsx`
- `components/search/ConversationHistory.tsx`
- `components/search/RefinementControls.tsx`
- `components/search/ComparisonView.tsx`
- `components/search/RefinementSuggestions.tsx`
- `components/vehicles/VehicleDetails.tsx`
- `lib/api/conversation.ts`

**References:**
- Task 018: Search interface
- Task 012: Session storage
- Task 013: Reference resolution
