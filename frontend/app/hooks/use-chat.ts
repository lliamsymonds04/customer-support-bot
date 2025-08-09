import { useState, useCallback } from 'react';
import { useSessionId } from './use-session-id';
import type { setErrorMessage } from './user-error';

export function useChat(setErrorMessage: setErrorMessage) {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState("");
  const sessionId = useSessionId(setErrorMessage);

  const handleInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setInput(e.target.value);
  }, []);

  return {
    messages,
    input,
    handleInputChange,
  };
}