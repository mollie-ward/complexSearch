import { render, screen } from '@testing-library/react';
import { VehicleSpecifications } from '@/components/vehicles/VehicleSpecifications';
import { VehicleDocument } from '@/lib/api/types';

const mockVehicle: VehicleDocument = {
  id: 'TEST123',
  make: 'BMW',
  model: '3 Series',
  derivative: '320d M Sport',
  bodyType: 'Saloon',
  price: 18500,
  mileage: 45000,
  registrationDate: '2020-06-15T00:00:00Z',
  transmissionType: 'Automatic',
  fuelType: 'Diesel',
  saleLocation: 'London',
  colour: 'Black',
  engineSize: 2.0,
  numberOfDoors: 4,
  grade: 'Premium',
  vatType: 'Qualifying',
  saleType: 'Retail',
  channel: 'Online',
  motExpiryDate: '2025-06-15T00:00:00Z',
  lastServiceDate: '2024-01-15T00:00:00Z',
};

describe('VehicleSpecifications', () => {
  it('renders all specifications', () => {
    render(<VehicleSpecifications vehicle={mockVehicle} />);

    expect(screen.getByText('Technical Specifications')).toBeInTheDocument();
    expect(screen.getByText('BMW')).toBeInTheDocument();
    expect(screen.getByText('3 Series')).toBeInTheDocument();
    expect(screen.getByText('Automatic')).toBeInTheDocument();
    expect(screen.getByText('Diesel')).toBeInTheDocument();
  });

  it('displays engine size with unit', () => {
    render(<VehicleSpecifications vehicle={mockVehicle} />);

    expect(screen.getByText('2L')).toBeInTheDocument();
  });

  it('shows N/A for missing optional fields', () => {
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

    render(<VehicleSpecifications vehicle={minimalVehicle} />);

    const naElements = screen.getAllByText('N/A');
    expect(naElements.length).toBeGreaterThan(0);
  });

  it('formats dates correctly', () => {
    render(<VehicleSpecifications vehicle={mockVehicle} />);

    // Check for formatted date strings (should include month and year)
    expect(screen.getByText(/June 2025/i)).toBeInTheDocument();
  });
});
