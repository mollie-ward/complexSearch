'use client';

import { useState, useEffect } from 'react';
import { createSession } from '../api/search';
import { SessionResponse } from '../api/types';

interface UseSessionReturn {
  session: SessionResponse | null;
  isLoading: boolean;
  error: string | null;
  createNewSession: () => Promise<void>;
}

/**
 * Hook to manage session lifecycle
 */
export function useSession(): UseSessionReturn {
  const [session, setSession] = useState<SessionResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const createNewSession = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const newSession = await createSession();
      setSession(newSession);
      // Store in localStorage for persistence
      if (typeof window !== 'undefined') {
        localStorage.setItem('searchSession', JSON.stringify(newSession));
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to create session';
      setError(errorMessage);
      console.error('Session creation failed:', err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    // Try to restore session from localStorage
    if (typeof window !== 'undefined') {
      const stored = localStorage.getItem('searchSession');
      if (stored) {
        try {
          const parsed = JSON.parse(stored);
          setSession(parsed);
          setIsLoading(false);
          return;
        } catch (err) {
          console.error('Failed to parse stored session:', err);
        }
      }
    }

    // Create new session if none exists
    createNewSession();
  }, []);

  return {
    session,
    isLoading,
    error,
    createNewSession,
  };
}
