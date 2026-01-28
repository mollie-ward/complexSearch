import { render, screen } from '@testing-library/react';
import { VehicleCard } from '@/components/search/VehicleCard';
import { VehicleResult } from '@/lib/api/types';
import userEvent from '@testing-library/user-event';

const mockResult: VehicleResult = {
  vehicle: {
    id: 'TEST123',
    make: 'BMW',
    model: '3 Series',
    derivative: '320d M Sport',
    price: 18500,
    mileage: 45000,
    registrationDate: '2020-06-15T00:00:00Z',
    transmissionType: 'Automatic',
    fuelType: 'Diesel',
    saleLocation: 'London',
    features: ['Leather Seats', 'Navigation', 'Parking Sensors', 'Bluetooth', 'Cruise Control', 'Heated Seats'],
    serviceHistoryPresent: true,
    description: 'Excellent condition BMW 3 Series with full service history',
  },
  score: 0.85,
  scoreBreakdown: {
    exactMatchScore: 0.7,
    semanticScore: 0.9,
    keywordScore: 0.8,
    finalScore: 0.85,
  },
};

describe('VehicleCard', () => {
  it('renders all vehicle details', () => {
    render(<VehicleCard result={mockResult} rank={1} />);

    expect(screen.getByText('BMW 3 Series')).toBeInTheDocument();
    expect(screen.getByText('320d M Sport')).toBeInTheDocument();
    expect(screen.getByText(/£18,500/)).toBeInTheDocument();
    expect(screen.getByText(/45,000 mi/)).toBeInTheDocument();
    expect(screen.getByText(/2020/)).toBeInTheDocument();
    expect(screen.getByText(/Automatic/)).toBeInTheDocument();
    expect(screen.getByText(/Diesel/)).toBeInTheDocument();
    expect(screen.getByText(/London/)).toBeInTheDocument();
  });

  it('formats price correctly', () => {
    render(<VehicleCard result={mockResult} rank={1} />);
    expect(screen.getByText('£18,500')).toBeInTheDocument();
  });

  it('shows relevance score', () => {
    render(<VehicleCard result={mockResult} rank={1} />);
    expect(screen.getByText(/Match: 85%/)).toBeInTheDocument();
  });

  it('toggles explanation', async () => {
    render(<VehicleCard result={mockResult} rank={1} />);

    const toggleButton = screen.getByRole('button', { name: /Why this match?/i });
    await userEvent.click(toggleButton);

    expect(screen.getByText(/Match Explanation/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Hide explanation/i })).toBeInTheDocument();
  });

  it('displays features badges', () => {
    render(<VehicleCard result={mockResult} rank={1} />);

    // Should show first 5 features
    expect(screen.getByText('Leather Seats')).toBeInTheDocument();
    expect(screen.getByText('Navigation')).toBeInTheDocument();
    expect(screen.getByText('Parking Sensors')).toBeInTheDocument();
    expect(screen.getByText('Bluetooth')).toBeInTheDocument();
    expect(screen.getByText('Cruise Control')).toBeInTheDocument();
    
    // Should show "+1 more" badge for remaining features
    expect(screen.getByText('+1 more')).toBeInTheDocument();
  });

  it('clamps description to 2 lines', () => {
    render(<VehicleCard result={mockResult} rank={1} />);

    const description = screen.getByText(/Excellent condition BMW/);
    expect(description).toHaveClass('line-clamp-2');
  });

  it('shows rank badge', () => {
    render(<VehicleCard result={mockResult} rank={1} />);
    expect(screen.getByText('#1')).toBeInTheDocument();
  });

  it('shows service history badge', () => {
    render(<VehicleCard result={mockResult} rank={1} />);
    const badges = screen.getAllByText(/Full Service History/i);
    expect(badges.length).toBeGreaterThan(0);
  });

  it('renders without optional fields', () => {
    const minimalResult: VehicleResult = {
      vehicle: {
        id: 'TEST456',
        make: 'Ford',
        model: 'Focus',
        price: 12000,
        mileage: 30000,
        transmissionType: 'Manual',
        fuelType: 'Petrol',
        saleLocation: 'Manchester',
      },
      score: 0.6,
    };

    render(<VehicleCard result={minimalResult} rank={2} />);

    expect(screen.getByText('Ford Focus')).toBeInTheDocument();
    expect(screen.getByText('£12,000')).toBeInTheDocument();
  });
});
