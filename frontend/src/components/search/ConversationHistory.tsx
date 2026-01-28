'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { MessageSquare, Trash2 } from 'lucide-react';

interface ConversationHistoryProps {
  sessionId: string;
  className?: string;
}

export function ConversationHistory({ sessionId, className }: ConversationHistoryProps) {
  // This is a placeholder for v1 - actual history tracking would require backend support
  const hasHistory = false;

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <MessageSquare className="h-5 w-5" />
          Conversation History
        </CardTitle>
        <CardDescription>
          Session ID: {sessionId.substring(0, 8)}...
        </CardDescription>
      </CardHeader>
      <CardContent>
        {!hasHistory ? (
          <p className="text-sm text-muted-foreground text-center py-4">
            Your search history will appear here
          </p>
        ) : (
          <div className="space-y-2">
            {/* Future: map through conversation history */}
            <div className="flex justify-end">
              <Button variant="ghost" size="sm">
                <Trash2 className="h-4 w-4 mr-1" />
                Clear history
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
