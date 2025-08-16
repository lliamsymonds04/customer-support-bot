import { useState, useEffect } from "react";
import type { Form } from "@/types/Form";
import * as signalR from "@microsoft/signalr";

interface useFormsType {
    sessionId: string | null;
    formsConnectionRef: React.RefObject<signalR.HubConnection | null>;
    isConnectedToFormsHub: boolean;
}

export function useForms({sessionId, formsConnectionRef, isConnectedToFormsHub}: useFormsType) {
    const [forms, setForms] = useState<Form[]>([]);

    const baseUrl = import.meta.env.VITE_API_URL;
    useEffect(() => {
        if (!sessionId) return;

        async function fetchForms() {
            const response = await fetch(`${baseUrl}/forms/session/${sessionId}`, {
                method: "GET",
                headers: {
                    "Content-Type": "application/json",
                },
            });

            if (!response.ok) {
                console.error("Failed to fetch forms");
                return;
            }

            const data = await response.json();
            setForms(data);
        }

        fetchForms();
    }, [sessionId])

    useEffect(() => {
        const conn = formsConnectionRef.current;
        if (!conn || !isConnectedToFormsHub) return;
        console.log("listening to form webhook")

        const handleNewForm = (form: Form) => {
            setForms((prevForms) => [...prevForms, form]);
        };

        conn.on("ReceiveUserForm", handleNewForm);

        return () => {
            conn.off("ReceiveUserForm", handleNewForm);
        };
    }, [isConnectedToFormsHub, formsConnectionRef]);

    return {
        forms,
    };
}