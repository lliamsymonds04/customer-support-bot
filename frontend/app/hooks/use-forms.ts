import { useState, useEffect } from "react";
import { useSessionId } from "./use-session-id";


export enum FormCategory
{
    General,
    Technical,
    Billing,
    Feedback,
    Account,
}

export enum FormUrgency
{
    Low,
    Medium,
    High,
    Critical
}

interface Form
{
    title: string;
    description: string;
    category: FormCategory;
    urgency: FormUrgency;
    createdAt: Date;
}

export function useForms() {
    const [forms, setForms] = useState<Form[]>([]);
    const sessionId = useSessionId();

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
            console.log("Fetched forms:", data);
            setForms(data);
        }

        fetchForms();
    }, [sessionId])

    return {
        forms,
    };
}