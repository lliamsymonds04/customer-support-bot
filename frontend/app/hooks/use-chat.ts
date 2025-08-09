import { useState, useCallback } from 'react';

const baseUrl = "http://localhost:5153"
const apiUrl = '/api/ask';

export function useChat() {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState("");

  const handleInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    setInput(e.target.value);
  }, []);

  return {
    messages,
    input,
    handleInputChange,
  };
}