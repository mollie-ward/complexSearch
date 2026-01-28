import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ConversationHistory } from '@/components/search/ConversationHistory';
import * as conversationApi from '@/lib/api/conversation';

// Mock the API
jest.mock('@/lib/api/conversation');

const mockHistory = {
  sessionId: 'test-session-123',
  messages: [
    {
      messageId: '1',
      role: 'User' as const,
      content: 'BMW under £20k',
      timestamp: '2024-01-28T10:00:00Z',
    },
    {
      messageId: '2',
      role: 'Assistant' as const,
      content: 'Here are 5 BMW vehicles under £20,000',
      timestamp: '2024-01-28T10:00:05Z',
      results: {
        count: 5,
        strategy: 'Hybrid',
      },
    },
    {
      messageId: '3',
      role: 'User' as const,
      content: 'Show me cheaper ones',
      timestamp: '2024-01-28T10:01:00Z',
    },
  ],
  totalMessages: 3,
};

describe('ConversationHistory', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders empty state when no messages', async () => {
    (conversationApi.getConversationHistory as jest.Mock).mockResolvedValue({
      sessionId: 'test-session-123',
      messages: [],
      totalMessages: 0,
    });

    render(<ConversationHistory sessionId="test-session-123" />);

    await waitFor(() => {
      expect(screen.getByText(/Your search history will appear here/i)).toBeInTheDocument();
    });
  });

  it('renders messages in correct order', async () => {
    (conversationApi.getConversationHistory as jest.Mock).mockResolvedValue(mockHistory);

    render(<ConversationHistory sessionId="test-session-123" />);

    await waitFor(() => {
      expect(screen.getByText('BMW under £20k')).toBeInTheDocument();
      expect(screen.getByText('Here are 5 BMW vehicles under £20,000')).toBeInTheDocument();
      expect(screen.getByText('Show me cheaper ones')).toBeInTheDocument();
    });
  });

  it('distinguishes user vs assistant messages', async () => {
    (conversationApi.getConversationHistory as jest.Mock).mockResolvedValue(mockHistory);

    const { container } = render(<ConversationHistory sessionId="test-session-123" />);

    await waitFor(() => {
      const userMessages = container.querySelectorAll('.bg-primary');
      const assistantMessages = container.querySelectorAll('.bg-muted');
      
      expect(userMessages.length).toBeGreaterThan(0);
      expect(assistantMessages.length).toBeGreaterThan(0);
    });
  });

  it('shows result counts', async () => {
    (conversationApi.getConversationHistory as jest.Mock).mockResolvedValue(mockHistory);

    render(<ConversationHistory sessionId="test-session-123" />);

    await waitFor(() => {
      expect(screen.getByText(/5 results/i)).toBeInTheDocument();
    });
  });

  it('clears history on button click', async () => {
    (conversationApi.getConversationHistory as jest.Mock).mockResolvedValue(mockHistory);
    (conversationApi.clearConversation as jest.Mock).mockResolvedValue(undefined);

    render(<ConversationHistory sessionId="test-session-123" />);

    await waitFor(() => {
      expect(screen.getByText('BMW under £20k')).toBeInTheDocument();
    });

    const clearButton = screen.getByRole('button', { name: /clear/i });
    await userEvent.click(clearButton);

    // Confirm dialog
    const confirmButton = screen.getByRole('button', { name: /clear history/i });
    await userEvent.click(confirmButton);

    await waitFor(() => {
      expect(conversationApi.clearConversation).toHaveBeenCalledWith('test-session-123');
    });
  });

  it('displays session ID', async () => {
    (conversationApi.getConversationHistory as jest.Mock).mockResolvedValue(mockHistory);

    render(<ConversationHistory sessionId="test-session-123" />);

    await waitFor(() => {
      expect(screen.getByText(/test-sess/i)).toBeInTheDocument();
    });
  });
});
