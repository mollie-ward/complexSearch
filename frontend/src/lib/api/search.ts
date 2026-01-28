import { SearchRequest, SearchResults, VehicleDocument, SessionResponse, ApiError } from './types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

class SearchApiError extends Error implements ApiError {
  status?: number;
  details?: unknown;

  constructor(message: string, status?: number, details?: unknown) {
    super(message);
    this.name = 'SearchApiError';
    this.status = status;
    this.details = details;
  }
}

/**
 * Search for vehicles using natural language query
 */
export async function searchVehicles(request: SearchRequest): Promise<SearchResults> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/search`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        query: request.query,
        maxResults: request.maxResults || 10,
        filters: {},
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new SearchApiError(
        errorData.message || `Search failed: ${response.statusText}`,
        response.status,
        errorData
      );
    }

    const data = await response.json();
    
    // Transform the response to match our interface
    return {
      results: data.results || [],
      totalCount: data.totalCount || 0,
      searchDuration: formatDuration(data.searchDuration),
      strategy: data.strategy || { name: 'unknown' },
    };
  } catch (error) {
    if (error instanceof SearchApiError) {
      throw error;
    }
    throw new SearchApiError(
      error instanceof Error ? error.message : 'An unknown error occurred',
      undefined,
      error
    );
  }
}

/**
 * Get a vehicle by ID
 */
export async function getVehicleById(id: string): Promise<VehicleDocument> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/vehicles/${id}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new SearchApiError(
        errorData.message || `Failed to fetch vehicle: ${response.statusText}`,
        response.status,
        errorData
      );
    }

    return await response.json();
  } catch (error) {
    if (error instanceof SearchApiError) {
      throw error;
    }
    throw new SearchApiError(
      error instanceof Error ? error.message : 'An unknown error occurred',
      undefined,
      error
    );
  }
}

/**
 * Create a new session
 */
export async function createSession(): Promise<SessionResponse> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/sessions`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({}),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new SearchApiError(
        errorData.message || `Failed to create session: ${response.statusText}`,
        response.status,
        errorData
      );
    }

    return await response.json();
  } catch (error) {
    if (error instanceof SearchApiError) {
      throw error;
    }
    throw new SearchApiError(
      error instanceof Error ? error.message : 'An unknown error occurred',
      undefined,
      error
    );
  }
}

/**
 * Format a TimeSpan duration to a readable string
 */
function formatDuration(duration: string | { totalMilliseconds?: number }): string {
  if (typeof duration === 'string') {
    return duration;
  }
  
  if (duration && typeof duration === 'object' && 'totalMilliseconds' in duration) {
    const ms = duration.totalMilliseconds || 0;
    if (ms < 1000) {
      return `${ms.toFixed(0)}ms`;
    }
    return `${(ms / 1000).toFixed(2)}s`;
  }
  
  return '0ms';
}
