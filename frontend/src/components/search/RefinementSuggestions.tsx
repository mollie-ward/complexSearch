'use client';

import { Badge } from '@/components/ui/badge';
import { Sparkles } from 'lucide-react';

interface RefinementSuggestionsProps {
  onSuggest: (query: string) => void;
  isLoading?: boolean;
}

const SUGGESTIONS = [
  'Show me cheaper ones',
  'With lower mileage',
  'Only automatic',
  'From 2020 or newer',
  'With leather seats',
  'In London',
  'Electric only',
  'Under £15,000',
];

export function RefinementSuggestions({ onSuggest, isLoading }: RefinementSuggestionsProps) {
  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <Sparkles className="h-4 w-4" />
        <span>Quick Refinements</span>
      </div>
      <div className="flex flex-wrap gap-2">
        {SUGGESTIONS.map((suggestion) => (
          <Badge
            key={suggestion}
            variant="secondary"
            className="cursor-pointer hover:bg-primary hover:text-primary-foreground transition-colors"
            onClick={() => !isLoading && onSuggest(suggestion)}
          >
            {suggestion}
          </Badge>
        ))}
      </div>
    </div>
  );
}
