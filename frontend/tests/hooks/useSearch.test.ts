import { renderHook, act, waitFor } from '@testing-library/react';
import { useSearch } from '@/lib/hooks/useSearch';
import * as searchApi from '@/lib/api/search';

// Mock the search API
jest.mock('@/lib/api/search');

const mockSearchVehicles = searchApi.searchVehicles as jest.MockedFunction<
  typeof searchApi.searchVehicles
>;

describe('useSearch', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('manages loading state', async () => {
    mockSearchVehicles.mockImplementation(
      () => new Promise((resolve) => setTimeout(() => resolve({
        results: [],
        totalCount: 0,
        searchDuration: '100ms',
        strategy: { name: 'hybrid' },
      }), 100))
    );

    const { result } = renderHook(() => useSearch('test-session'));

    expect(result.current.isLoading).toBe(false);

    act(() => {
      result.current.search('test query');
    });

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });
  });

  it('handles successful search', async () => {
    const mockResults = {
      results: [
        {
          vehicle: {
            id: 'TEST1',
            make: 'BMW',
            model: '3 Series',
            price: 18500,
            mileage: 45000,
            transmissionType: 'Automatic',
            fuelType: 'Diesel',
            saleLocation: 'London',
          },
          score: 0.85,
        },
      ],
      totalCount: 1,
      searchDuration: '150ms',
      strategy: { name: 'hybrid' },
    };

    mockSearchVehicles.mockResolvedValue(mockResults);

    const { result } = renderHook(() => useSearch('test-session'));

    await act(async () => {
      await result.current.search('BMW under £20k');
    });

    expect(result.current.results).toEqual(mockResults);
    expect(result.current.error).toBeNull();
  });

  it('handles API errors', async () => {
    const errorMessage = 'Search failed';
    mockSearchVehicles.mockRejectedValue(new Error(errorMessage));

    const { result } = renderHook(() => useSearch('test-session'));

    await act(async () => {
      await result.current.search('test query');
    });

    expect(result.current.error).toBe(errorMessage);
    expect(result.current.results).toBeNull();
  });

  it('requires session ID', async () => {
    const { result } = renderHook(() => useSearch());

    await act(async () => {
      await result.current.search('test query');
    });

    expect(mockSearchVehicles).toHaveBeenCalledWith(
      expect.objectContaining({
        query: 'test query',
        sessionId: undefined,
      })
    );
  });

  it('clears results', () => {
    const { result } = renderHook(() => useSearch('test-session'));

    act(() => {
      // Set some initial state
      result.current.search('test');
    });

    act(() => {
      result.current.clearResults();
    });

    expect(result.current.results).toBeNull();
    expect(result.current.error).toBeNull();
  });

  it('does not search with empty query', async () => {
    const { result } = renderHook(() => useSearch('test-session'));

    await act(async () => {
      await result.current.search('');
    });

    expect(mockSearchVehicles).not.toHaveBeenCalled();
    expect(result.current.error).toBe('Please enter a search query');
  });

  it('trims whitespace from query', async () => {
    mockSearchVehicles.mockResolvedValue({
      results: [],
      totalCount: 0,
      searchDuration: '50ms',
      strategy: { name: 'hybrid' },
    });

    const { result } = renderHook(() => useSearch('test-session'));

    await act(async () => {
      await result.current.search('   ');
    });

    expect(mockSearchVehicles).not.toHaveBeenCalled();
    expect(result.current.error).toBe('Please enter a search query');
  });
});
