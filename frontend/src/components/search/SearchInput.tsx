'use client';

import { useState, useRef, useEffect, KeyboardEvent } from 'react';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { Loader2 } from 'lucide-react';

interface SearchInputProps {
  onSearch: (query: string) => void;
  isLoading?: boolean;
  placeholder?: string;
  className?: string;
}

const EXAMPLE_QUERIES = [
  'Reliable BMW under £20k',
  'Economical family car',
  'Sporty convertible with low mileage',
];

export function SearchInput({ onSearch, isLoading = false, placeholder, className }: SearchInputProps) {
  const [query, setQuery] = useState('');
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Auto-resize textarea
  useEffect(() => {
    const textarea = textareaRef.current;
    if (!textarea) return;

    textarea.style.height = 'auto';
    const newHeight = Math.min(Math.max(textarea.scrollHeight, 100), 300);
    textarea.style.height = `${newHeight}px`;
  }, [query]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim() && !isLoading) {
      onSearch(query.trim());
      setQuery('');
    }
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    // Enter submits, Shift+Enter adds newline
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  const handleExampleClick = (example: string) => {
    setQuery(example);
    // Focus textarea after setting query
    setTimeout(() => textareaRef.current?.focus(), 0);
  };

  return (
    <div className={className}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="relative">
          <Textarea
            ref={textareaRef}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={placeholder || 'Describe the vehicle you\'re looking for...'}
            className="resize-none min-h-[100px] max-h-[300px]"
            disabled={isLoading}
            aria-label="Search query input"
          />
          {query.length > 0 && (
            <div className="absolute bottom-2 right-2 text-xs text-muted-foreground">
              {query.length} characters
            </div>
          )}
        </div>

        <div className="flex items-center justify-between gap-4">
          <div className="flex flex-wrap gap-2">
            <span className="text-sm text-muted-foreground">Try:</span>
            {EXAMPLE_QUERIES.map((example) => (
              <button
                key={example}
                type="button"
                onClick={() => handleExampleClick(example)}
                className="text-sm text-primary hover:underline disabled:opacity-50 disabled:cursor-not-allowed"
                disabled={isLoading}
              >
                {example}
              </button>
            ))}
          </div>

          <Button type="submit" disabled={!query.trim() || isLoading}>
            {isLoading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Searching...
              </>
            ) : (
              'Search'
            )}
          </Button>
        </div>
      </form>
    </div>
  );
}
