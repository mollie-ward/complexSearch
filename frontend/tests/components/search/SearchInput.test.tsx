import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SearchInput } from '@/components/search/SearchInput';

describe('SearchInput', () => {
  it('renders with placeholder', () => {
    render(<SearchInput onSearch={jest.fn()} />);
    const textarea = screen.getByPlaceholderText(/Describe the vehicle/i);
    expect(textarea).toBeInTheDocument();
  });

  it('calls onSearch when form submitted', async () => {
    const onSearch = jest.fn();
    render(<SearchInput onSearch={onSearch} />);

    const textarea = screen.getByRole('textbox');
    const submitButton = screen.getByRole('button', { name: /Search/i });

    await userEvent.type(textarea, 'BMW under £20k');
    fireEvent.click(submitButton);

    expect(onSearch).toHaveBeenCalledWith('BMW under £20k');
  });

  it('clears input after search', async () => {
    const onSearch = jest.fn();
    render(<SearchInput onSearch={onSearch} />);

    const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
    await userEvent.type(textarea, 'Test query');
    
    const submitButton = screen.getByRole('button', { name: /Search/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(textarea.value).toBe('');
    });
  });

  it('shows loading state', () => {
    render(<SearchInput onSearch={jest.fn()} isLoading={true} />);
    
    expect(screen.getByText(/Searching.../i)).toBeInTheDocument();
    expect(screen.getByRole('textbox')).toBeDisabled();
  });

  it('disables during loading', () => {
    render(<SearchInput onSearch={jest.fn()} isLoading={true} />);
    
    const textarea = screen.getByRole('textbox');
    const submitButton = screen.getByRole('button', { name: /Searching/i });

    expect(textarea).toBeDisabled();
    expect(submitButton).toBeDisabled();
  });

  it('example queries are clickable', async () => {
    render(<SearchInput onSearch={jest.fn()} />);
    
    const exampleButton = screen.getByText(/Reliable BMW under £20k/i);
    fireEvent.click(exampleButton);

    await waitFor(() => {
      const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      expect(textarea.value).toBe('Reliable BMW under £20k');
    });
  });

  it('Enter submits, Shift+Enter adds newline', async () => {
    const onSearch = jest.fn();
    render(<SearchInput onSearch={onSearch} />);

    const textarea = screen.getByRole('textbox');
    await userEvent.type(textarea, 'Test query');

    // Regular Enter should submit
    fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: false });
    expect(onSearch).toHaveBeenCalledWith('Test query');

    // Shift+Enter should not submit (just add newline)
    onSearch.mockClear();
    await userEvent.type(textarea, 'Another query');
    fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: true });
    expect(onSearch).not.toHaveBeenCalled();
  });

  it('shows character count when typing', async () => {
    render(<SearchInput onSearch={jest.fn()} />);
    
    const textarea = screen.getByRole('textbox');
    await userEvent.type(textarea, 'Hello');

    expect(screen.getByText(/5 characters/i)).toBeInTheDocument();
  });

  it('does not submit empty query', () => {
    const onSearch = jest.fn();
    render(<SearchInput onSearch={onSearch} />);

    const submitButton = screen.getByRole('button', { name: /Search/i });
    expect(submitButton).toBeDisabled();

    fireEvent.click(submitButton);
    expect(onSearch).not.toHaveBeenCalled();
  });
});
