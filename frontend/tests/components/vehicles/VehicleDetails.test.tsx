import { render, screen } from '@testing-library/react';
import { VehicleDetails } from '@/components/vehicles/VehicleDetails';
import { VehicleDocument } from '@/lib/api/types';

const mockVehicle: VehicleDocument = {
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
  colour: 'Black',
  engineSize: 2.0,
  features: ['Leather Seats', 'Navigation', 'Parking Sensors'],
  serviceHistoryPresent: true,
  numberOfServices: 5,
  description: 'Excellent condition BMW 3 Series with full service history',
  capRetailPrice: 19500,
};

describe('VehicleDetails', () => {
  it('renders all vehicle information', () => {
    render(<VehicleDetails vehicle={mockVehicle} />);

    expect(screen.getByText('BMW 3 Series')).toBeInTheDocument();
    expect(screen.getByText('320d M Sport')).toBeInTheDocument();
    expect(screen.getByText(/London/)).toBeInTheDocument();
    expect(screen.getByText(/Automatic/)).toBeInTheDocument();
    expect(screen.getByText(/Diesel/)).toBeInTheDocument();
  });

  it('formats price correctly', () => {
    render(<VehicleDetails vehicle={mockVehicle} />);

    expect(screen.getByText('£18,500')).toBeInTheDocument();
  });

  it('shows service history indicator', () => {
    render(<VehicleDetails vehicle={mockVehicle} />);

    expect(screen.getByText(/Full Service History/i)).toBeInTheDocument();
    expect(screen.getByText(/5 services/i)).toBeInTheDocument();
  });

  it('displays features list', () => {
    render(<VehicleDetails vehicle={mockVehicle} />);

    expect(screen.getByText('Leather Seats')).toBeInTheDocument();
    expect(screen.getByText('Navigation')).toBeInTheDocument();
    expect(screen.getByText('Parking Sensors')).toBeInTheDocument();
  });

  it('shows description when available', () => {
    render(<VehicleDetails vehicle={mockVehicle} />);

    expect(screen.getByText(/Excellent condition BMW/)).toBeInTheDocument();
  });

  it('displays CAP retail price', () => {
    render(<VehicleDetails vehicle={mockVehicle} />);

    expect(screen.getByText(/CAP Retail/)).toBeInTheDocument();
    expect(screen.getByText(/£19,500/)).toBeInTheDocument();
  });

  it('renders without optional fields', () => {
    const minimalVehicle: VehicleDocument = {
      id: 'TEST456',
      make: 'Ford',
      model: 'Focus',
      price: 12000,
      mileage: 30000,
      transmissionType: 'Manual',
      fuelType: 'Petrol',
      saleLocation: 'Manchester',
    };

    render(<VehicleDetails vehicle={minimalVehicle} />);

    expect(screen.getByText('Ford Focus')).toBeInTheDocument();
    expect(screen.getByText('£12,000')).toBeInTheDocument();
  });
});
