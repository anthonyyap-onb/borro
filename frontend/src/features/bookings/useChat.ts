import * as signalR from '@microsoft/signalr';
import { useCallback, useEffect, useRef, useState } from 'react';
import type { MessageDto } from './bookingApi';

// Empty string = relative origin; Vite proxy forwards /hubs → backend.
const API_URL = '';

export function useChat(bookingId: string) {
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [connected, setConnected] = useState(false);
  const hubRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!bookingId) return;
    const token = localStorage.getItem('borro_token') ?? '';

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/chat`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMessage', (msg: MessageDto) => {
      setMessages(prev => [...prev, msg]);
    });

    connection
      .start()
      .then(() => {
        setConnected(true);
        return connection.invoke('JoinBookingGroup', bookingId);
      })
      .catch(console.error);

    hubRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [bookingId]);

  const sendMessage = useCallback(async (content: string) => {
    if (!hubRef.current || hubRef.current.state !== signalR.HubConnectionState.Connected) return;
    await hubRef.current.invoke('SendMessage', bookingId, content);
  }, [bookingId]);

  return { messages, connected, sendMessage, setMessages };
}
