import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RefinementSuggestions } from '@/components/search/RefinementSuggestions';

describe('RefinementSuggestions', () => {
  const mockOnSuggest = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders all suggestions', () => {
    render(<RefinementSuggestions onSuggest={mockOnSuggest} />);

    expect(screen.getByText('Show me cheaper ones')).toBeInTheDocument();
    expect(screen.getByText('With lower mileage')).toBeInTheDocument();
    expect(screen.getByText('Only automatic')).toBeInTheDocument();
  });

  it('calls onSuggest with correct text on click', async () => {
    render(<RefinementSuggestions onSuggest={mockOnSuggest} />);

    const suggestion = screen.getByText('Show me cheaper ones');
    await userEvent.click(suggestion);

    expect(mockOnSuggest).toHaveBeenCalledWith('Show me cheaper ones');
  });

  it('does not call onSuggest when loading', async () => {
    render(<RefinementSuggestions onSuggest={mockOnSuggest} isLoading={true} />);

    const suggestion = screen.getByText('Show me cheaper ones');
    await userEvent.click(suggestion);

    expect(mockOnSuggest).not.toHaveBeenCalled();
  });

  it('handles multiple suggestion clicks', async () => {
    render(<RefinementSuggestions onSuggest={mockOnSuggest} />);

    await userEvent.click(screen.getByText('Show me cheaper ones'));
    await userEvent.click(screen.getByText('With lower mileage'));

    expect(mockOnSuggest).toHaveBeenCalledTimes(2);
    expect(mockOnSuggest).toHaveBeenNthCalledWith(1, 'Show me cheaper ones');
    expect(mockOnSuggest).toHaveBeenNthCalledWith(2, 'With lower mileage');
  });
});
