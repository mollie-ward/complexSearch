import { render, screen } from '@testing-library/react';
import { ResultsList } from '@/components/search/ResultsList';
import { VehicleResult } from '@/lib/api/types';
import { ComparisonProvider } from '@/lib/context/ComparisonContext';

const mockResults: VehicleResult[] = [
  {
    vehicle: {
      id: 'TEST1',
      make: 'BMW',
      model: '3 Series',
      price: 18500,
      mileage: 45000,
      registrationDate: '2020-06-15T00:00:00Z',
      transmissionType: 'Automatic',
      fuelType: 'Diesel',
      saleLocation: 'London',
    },
    score: 0.85,
  },
  {
    vehicle: {
      id: 'TEST2',
      make: 'Audi',
      model: 'A4',
      price: 19500,
      mileage: 38000,
      registrationDate: '2021-03-10T00:00:00Z',
      transmissionType: 'Automatic',
      fuelType: 'Petrol',
      saleLocation: 'Birmingham',
    },
    score: 0.80,
  },
];

const renderWithProvider = (ui: React.ReactElement) => {
  return renderWithProvider(<ComparisonProvider>{ui}</ComparisonProvider>);
};

describe('ResultsList', () => {
  it('renders all results', () => {
    renderWithProvider(
      <ResultsList
        results={mockResults}
        totalCount={2}
        searchDuration="120ms"
      />
    );

    expect(screen.getByText('BMW 3 Series')).toBeInTheDocument();
    expect(screen.getByText('Audi A4')).toBeInTheDocument();
  });

  it('shows empty state when no results', () => {
    renderWithProvider(
      <ResultsList
        results={[]}
        totalCount={0}
        searchDuration="50ms"
      />
    );

    expect(screen.getByText(/No vehicles found/i)).toBeInTheDocument();
    expect(screen.getByText(/Try adjusting your search criteria/i)).toBeInTheDocument();
  });

  it('displays search metadata', () => {
    renderWithProvider(
      <ResultsList
        results={mockResults}
        totalCount={2}
        searchDuration="120ms"
      />
    );

    expect(screen.getByText(/2 results found/i)).toBeInTheDocument();
    expect(screen.getByText('120ms')).toBeInTheDocument();
  });

  it('uses responsive layout', () => {
    const { container } = renderWithProvider(
      <ResultsList
        results={mockResults}
        totalCount={2}
        searchDuration="120ms"
      />
    );

    // Check for grid layout classes
    const grid = container.querySelector('.grid');
    expect(grid).toHaveClass('grid-cols-1', 'md:grid-cols-2');
  });

  it('shows correct count in metadata', () => {
    renderWithProvider(
      <ResultsList
        results={mockResults}
        totalCount={100}
        searchDuration="200ms"
      />
    );

    // Should show total count, not just results array length
    expect(screen.getByText(/100 results found/i)).toBeInTheDocument();
  });
});
