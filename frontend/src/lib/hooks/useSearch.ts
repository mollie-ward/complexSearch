'use client';

import { useState, useCallback } from 'react';
import { searchVehicles } from '../api/search';
import { SearchResults } from '../api/types';

interface UseSearchReturn {
  results: SearchResults | null;
  isLoading: boolean;
  error: string | null;
  search: (query: string, maxResults?: number) => Promise<void>;
  clearResults: () => void;
}

/**
 * Hook to manage search state and operations
 */
export function useSearch(sessionId?: string): UseSearchReturn {
  const [results, setResults] = useState<SearchResults | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const search = useCallback(async (query: string, maxResults = 10) => {
    if (!query.trim()) {
      setError('Please enter a search query');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const searchResults = await searchVehicles({
        query,
        sessionId,
        maxResults,
      });
      
      setResults(searchResults);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Search failed';
      setError(errorMessage);
      console.error('Search error:', err);
    } finally {
      setIsLoading(false);
    }
  }, [sessionId]);

  const clearResults = useCallback(() => {
    setResults(null);
    setError(null);
  }, []);

  return {
    results,
    isLoading,
    error,
    search,
    clearResults,
  };
}
