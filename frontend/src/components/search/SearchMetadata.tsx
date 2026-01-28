'use client';

import { Clock, Search } from 'lucide-react';

interface SearchMetadataProps {
  totalCount: number;
  searchDuration: string;
  className?: string;
}

export function SearchMetadata({ totalCount, searchDuration, className }: SearchMetadataProps) {
  return (
    <div className={`flex items-center gap-4 text-sm text-muted-foreground ${className || ''}`}>
      <div className="flex items-center gap-1">
        <Search className="h-4 w-4" />
        <span>
          {totalCount} {totalCount === 1 ? 'result' : 'results'} found
        </span>
      </div>
      <div className="flex items-center gap-1">
        <Clock className="h-4 w-4" />
        <span>{searchDuration}</span>
      </div>
    </div>
  );
}
