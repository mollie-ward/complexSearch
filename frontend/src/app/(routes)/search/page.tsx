'use client';

import { SearchInput } from '@/components/search/SearchInput';
import { ResultsList } from '@/components/search/ResultsList';
import { ConversationHistory } from '@/components/search/ConversationHistory';
import { useSearch } from '@/lib/hooks/useSearch';
import { useSession } from '@/lib/hooks/useSession';
import { Skeleton } from '@/components/ui/skeleton';
import { AlertCircle } from 'lucide-react';

export default function SearchPage() {
  const { session, isLoading: sessionLoading, error: sessionError } = useSession();
  const { search, results, isLoading, error } = useSearch(session?.sessionId);

  const handleSearch = async (query: string) => {
    await search(query);
  };

  if (sessionLoading) {
    return (
      <main className="container mx-auto px-4 py-8">
        <div className="space-y-4">
          <Skeleton className="h-12 w-64" />
          <Skeleton className="h-32 w-full" />
        </div>
      </main>
    );
  }

  if (sessionError) {
    return (
      <main className="container mx-auto px-4 py-8">
        <div className="flex items-center gap-2 p-4 bg-destructive/15 text-destructive rounded-md">
          <AlertCircle className="h-5 w-5" />
          <p>Failed to create session: {sessionError}</p>
        </div>
      </main>
    );
  }

  return (
    <main className="container mx-auto px-4 py-8">
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
            <div className="flex items-center gap-2 p-4 bg-destructive/15 text-destructive rounded-md">
              <AlertCircle className="h-5 w-5" />
              <p>{error}</p>
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
          {session && <ConversationHistory sessionId={session.sessionId} />}
        </div>
      </div>
    </main>
  );
}
