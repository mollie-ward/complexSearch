import { ConversationHistory } from './types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

class ConversationApiError extends Error {
  status?: number;
  details?: unknown;

  constructor(message: string, status?: number, details?: unknown) {
    super(message);
    this.name = 'ConversationApiError';
    this.status = status;
    this.details = details;
  }
}

/**
 * Get conversation history for a session
 */
export async function getConversationHistory(sessionId: string): Promise<ConversationHistory> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/sessions/${sessionId}/history`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      // If endpoint doesn't exist yet, return empty history
      if (response.status === 404) {
        return {
          sessionId,
          messages: [],
          totalMessages: 0,
        };
      }
      const errorData = await response.json().catch(() => ({}));
      throw new ConversationApiError(
        errorData.message || `Failed to fetch conversation history: ${response.statusText}`,
        response.status,
        errorData
      );
    }

    return await response.json();
  } catch (error) {
    if (error instanceof ConversationApiError) {
      throw error;
    }
    // Return empty history on error
    console.warn('Failed to fetch conversation history:', error);
    return {
      sessionId,
      messages: [],
      totalMessages: 0,
    };
  }
}

/**
 * Clear conversation history for a session
 */
export async function clearConversation(sessionId: string): Promise<void> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/sessions/${sessionId}/history`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new ConversationApiError(
        errorData.message || `Failed to clear conversation: ${response.statusText}`,
        response.status,
        errorData
      );
    }
  } catch (error) {
    if (error instanceof ConversationApiError) {
      throw error;
    }
    throw new ConversationApiError(
      error instanceof Error ? error.message : 'An unknown error occurred',
      undefined,
      error
    );
  }
}
