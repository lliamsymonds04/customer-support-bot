import { useState, useCallback } from 'react';
import { useSessionId } from './use-session-id';
import type { setErrorMessage } from './user-error';

export function useChat(setErrorMessage: setErrorMessage) {
    const [messages, setMessages] = useState([]);
    const [input, setInput] = useState("");
    const [isProcessing, setIsProcessing] = useState(false);
    const sessionId = useSessionId(setErrorMessage);
    const baseUrl = import.meta.env.VITE_API_URL;

    const handleInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        setInput(e.target.value);
    }, []);


    async function handleSubmit(e?: React.FormEvent) {
        if (e) e.preventDefault();
        if (!input) return;
        setIsProcessing(true);
        try {
            const response = await fetch(`${baseUrl}/chat`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    sessionId,
                    prompt: input,
                }),
            });

            if (!response.ok) {
                throw new Error("Failed to send message");
            }

            const data = await response.text();
            console.log(data)

        } catch (error) {
            setErrorMessage("Failed to send message");
        } finally {
            setIsProcessing(false);
            setInput("");
        }
    }

    return {
        messages,
        input,
        handleInputChange,
        handleSubmit,
        isProcessing,

    };
}