'use client';

import { useEffect, useState, useRef } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { MessageSquare, Trash2, User, Bot } from 'lucide-react';
import { getConversationHistory, clearConversation } from '@/lib/api/conversation';
import { ConversationMessage } from '@/lib/api/types';
import { cn } from '@/lib/utils';

interface ConversationHistoryProps {
  sessionId: string;
  className?: string;
}

export function ConversationHistory({ sessionId, className }: ConversationHistoryProps) {
  const [messages, setMessages] = useState<ConversationMessage[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const scrollAreaRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const fetchHistory = async () => {
      try {
        setIsLoading(true);
        const history = await getConversationHistory(sessionId);
        setMessages(history.messages);
      } catch (error) {
        console.error('Failed to fetch conversation history:', error);
      } finally {
        setIsLoading(false);
      }
    };

    fetchHistory();
  }, [sessionId]);

  // Auto-scroll to bottom when messages change
  useEffect(() => {
    if (scrollAreaRef.current) {
      const scrollContainer = scrollAreaRef.current.querySelector('[data-radix-scroll-area-viewport]');
      if (scrollContainer) {
        scrollContainer.scrollTop = scrollContainer.scrollHeight;
      }
    }
  }, [messages]);

  const handleClearHistory = async () => {
    try {
      await clearConversation(sessionId);
      setMessages([]);
      setDialogOpen(false);
    } catch (error) {
      console.error('Failed to clear conversation:', error);
    }
  };

  const formatTime = (timestamp: string) => {
    return new Date(timestamp).toLocaleTimeString('en-GB', {
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="text-lg flex items-center gap-2">
              <MessageSquare className="h-5 w-5" />
              Conversation History
            </CardTitle>
            <CardDescription>
              Session ID: {sessionId.substring(0, 8)}...
            </CardDescription>
          </div>
          {messages.length > 0 && (
            <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
              <DialogTrigger asChild>
                <Button variant="ghost" size="sm">
                  <Trash2 className="h-4 w-4 mr-1" />
                  Clear
                </Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Clear conversation history?</DialogTitle>
                  <DialogDescription>
                    This will permanently delete all messages in this session. This action cannot be undone.
                  </DialogDescription>
                </DialogHeader>
                <DialogFooter>
                  <Button variant="outline" onClick={() => setDialogOpen(false)}>
                    Cancel
                  </Button>
                  <Button variant="destructive" onClick={handleClearHistory}>
                    Clear History
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          )}
        </div>
      </CardHeader>
      <CardContent>
        <ScrollArea ref={scrollAreaRef} className="h-[400px] pr-4">
          {isLoading ? (
            <p className="text-sm text-muted-foreground text-center py-4">
              Loading conversation...
            </p>
          ) : messages.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-4">
              Your search history will appear here
            </p>
          ) : (
            <div className="space-y-4">
              {messages.map((message) => (
                <div
                  key={message.messageId}
                  className={cn(
                    'flex gap-2',
                    message.role === 'User' ? 'justify-end' : 'justify-start'
                  )}
                >
                  {message.role === 'Assistant' && (
                    <div className="flex-shrink-0">
                      <Bot className="h-6 w-6 text-muted-foreground" />
                    </div>
                  )}
                  <div
                    className={cn(
                      'max-w-[80%] rounded-lg px-3 py-2',
                      message.role === 'User'
                        ? 'bg-primary text-primary-foreground'
                        : 'bg-muted'
                    )}
                  >
                    <p className="text-sm">{message.content}</p>
                    {message.results && (
                      <div className="text-xs mt-1 opacity-80">
                        {message.results.count} results • {message.results.strategy}
                      </div>
                    )}
                    <div className="text-xs mt-1 opacity-70">
                      {formatTime(message.timestamp)}
                    </div>
                  </div>
                  {message.role === 'User' && (
                    <div className="flex-shrink-0">
                      <User className="h-6 w-6 text-primary" />
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </ScrollArea>
      </CardContent>
    </Card>
  );
}
