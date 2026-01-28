'use client';

import { VehicleResult } from '@/lib/api/types';
import { VehicleCard } from './VehicleCard';
import { SearchMetadata } from './SearchMetadata';

interface ResultsListProps {
  results: VehicleResult[];
  totalCount: number;
  searchDuration: string;
  className?: string;
}

export function ResultsList({ results, totalCount, searchDuration, className }: ResultsListProps) {
  if (results.length === 0) {
    return (
      <div className={className}>
        <div className="text-center py-12 px-4 border border-border rounded-lg bg-card">
          <p className="text-lg text-muted-foreground mb-2">No vehicles found</p>
          <p className="text-sm text-muted-foreground">
            Try adjusting your search criteria or using different keywords
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className={className}>
      <SearchMetadata
        totalCount={totalCount}
        searchDuration={searchDuration}
        className="mb-4"
      />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {results.map((result, index) => (
          <VehicleCard
            key={result.vehicle.id}
            result={result}
            rank={index + 1}
          />
        ))}
      </div>
    </div>
  );
}
