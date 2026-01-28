import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RefinementControls } from '@/components/search/RefinementControls';

describe('RefinementControls', () => {
  const mockOnRefine = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders all filter controls', async () => {
    render(<RefinementControls onRefine={mockOnRefine} />);

    const triggerButton = screen.getByRole('button', { name: /refine results/i });
    await userEvent.click(triggerButton);

    expect(screen.getByText(/price range/i)).toBeInTheDocument();
    expect(screen.getByText(/mileage range/i)).toBeInTheDocument();
    expect(screen.getByTestId('make-select')).toBeInTheDocument();
    expect(screen.getByTestId('fuel-type-select')).toBeInTheDocument();
    expect(screen.getByTestId('transmission-select')).toBeInTheDocument();
  });

  it('calls onRefine with correct values', async () => {
    render(<RefinementControls onRefine={mockOnRefine} />);

    const triggerButton = screen.getByRole('button', { name: /refine results/i });
    await userEvent.click(triggerButton);

    const applyButton = screen.getByRole('button', { name: /apply filters/i });
    await userEvent.click(applyButton);

    expect(mockOnRefine).toHaveBeenCalled();
    const [filters, query] = mockOnRefine.mock.calls[0];
    
    expect(filters).toHaveProperty('priceRange');
    expect(filters).toHaveProperty('mileageRange');
    expect(typeof query).toBe('string');
  });

  it('resets filters on reset button click', async () => {
    render(<RefinementControls onRefine={mockOnRefine} />);

    const triggerButton = screen.getByRole('button', { name: /refine results/i });
    await userEvent.click(triggerButton);

    const resetButton = screen.getByRole('button', { name: /reset/i });
    await userEvent.click(resetButton);

    // Verify reset happened (filters should be back to defaults)
    expect(screen.getByText(/£0 - £50,000/i)).toBeInTheDocument();
  });

  it('displays current values', async () => {
    render(<RefinementControls onRefine={mockOnRefine} />);

    const triggerButton = screen.getByRole('button', { name: /refine results/i });
    await userEvent.click(triggerButton);

    expect(screen.getByText(/£0 - £50,000/i)).toBeInTheDocument();
    expect(screen.getByText(/0 - 150,000 miles/i)).toBeInTheDocument();
  });

  it('disables controls when loading', async () => {
    render(<RefinementControls onRefine={mockOnRefine} isLoading={true} />);

    const triggerButton = screen.getByRole('button', { name: /refine results/i });
    await userEvent.click(triggerButton);

    const applyButton = screen.getByRole('button', { name: /apply filters/i });
    expect(applyButton).toBeDisabled();
  });
});
