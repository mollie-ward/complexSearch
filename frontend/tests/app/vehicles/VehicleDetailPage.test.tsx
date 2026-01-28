import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import VehicleDetailPage from '@/app/vehicles/[id]/page';
import * as searchApi from '@/lib/api/search';
import { useRouter } from 'next/navigation';

// Mock the API and router
jest.mock('@/lib/api/search');
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}));

const mockVehicle = {
  id: 'test-vehicle-123',
  make: 'BMW',
  model: '3 Series',
  derivative: '320d M Sport',
  price: 18500,
  mileage: 45000,
  registrationDate: '2020-06-15T00:00:00Z',
  transmissionType: 'Automatic',
  fuelType: 'Diesel',
  saleLocation: 'London',
  engineSize: 2.0,
  colour: 'Black',
  serviceHistoryPresent: true,
  features: ['Leather Seats', 'Navigation'],
};

const mockRouter = {
  push: jest.fn(),
  back: jest.fn(),
};

describe('VehicleDetailPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue(mockRouter);
  });

  it('fetches vehicle on mount', async () => {
    (searchApi.getVehicleById as jest.Mock).mockResolvedValue(mockVehicle);

    render(<VehicleDetailPage params={Promise.resolve({ id: 'test-vehicle-123' })} />);

    await waitFor(() => {
      expect(searchApi.getVehicleById).toHaveBeenCalledWith('test-vehicle-123');
    });
  });

  it('shows vehicle when loaded', async () => {
    (searchApi.getVehicleById as jest.Mock).mockResolvedValue(mockVehicle);

    render(<VehicleDetailPage params={Promise.resolve({ id: 'test-vehicle-123' })} />);

    await waitFor(() => {
      expect(screen.getByText('BMW 3 Series')).toBeInTheDocument();
    });
  });

  it('handles not found error', async () => {
    (searchApi.getVehicleById as jest.Mock).mockRejectedValue(
      new Error('Vehicle not found')
    );

    render(<VehicleDetailPage params={Promise.resolve({ id: 'invalid-id' })} />);

    await waitFor(() => {
      expect(screen.getByText(/Vehicle Not Found/i)).toBeInTheDocument();
    });
  });

  it('back button navigates', async () => {
    (searchApi.getVehicleById as jest.Mock).mockResolvedValue(mockVehicle);

    render(<VehicleDetailPage params={Promise.resolve({ id: 'test-vehicle-123' })} />);

    await waitFor(() => {
      expect(screen.getByText('BMW 3 Series')).toBeInTheDocument();
    });

    const backButton = screen.getByRole('button', { name: /back to search/i });
    await userEvent.click(backButton);

    expect(mockRouter.back).toHaveBeenCalled();
  });
});
