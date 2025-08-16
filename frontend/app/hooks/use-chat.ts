import { useState, useCallback, useRef } from 'react';
import type { setErrorMessage } from './util/user-error';

export type Message = {
    id: string;
    text: string;
    role: 'user' | 'bot';
};

export function useChat(setErrorMessage: setErrorMessage, sessionId: string | null) {
    const [messages, setMessages] = useState<Message[]>([]);
    const [input, setInput] = useState("");
    const [isProcessing, setIsProcessing] = useState(false);
    const submittedInput = useRef<string | null>(null);
    const baseUrl = import.meta.env.VITE_API_URL;

    const handleInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        setInput(e.target.value);
    }, []);


    async function handleSubmit(e?: React.FormEvent) {
        if (e) e.preventDefault();
        if (!input || !sessionId) return;
        setIsProcessing(true);

        submittedInput.current = input;
        setInput("");

        // add the user input to messages array
        const userMessage: Message = {
            id: Date.now().toString(),
            text: submittedInput.current,
            role: 'user',
        };

        setMessages((prev) => [...prev, userMessage]);

        try {
            const response = await fetch(`${baseUrl}/chat`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    sessionId,
                    prompt: submittedInput.current,
                }),
                credentials:"include",
            });

            if (!response.ok) {
                throw new Error("Failed to send message");
            }

            const data = await response.text();

            const botResponse: Message = {
                id: Date.now().toString(),
                text: data,
                role: 'bot',
            };
            setMessages((prev) => [...prev, botResponse]);

        } catch (error) {
            setErrorMessage("Failed to send message");
        } finally {
            setIsProcessing(false);
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