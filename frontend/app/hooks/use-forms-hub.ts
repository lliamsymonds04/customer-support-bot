import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

export function useFormsHub(sessionId: string | null) {
    const formsConnectionRef = useRef<signalR.HubConnection | null>(null);
    const [isConnectedToFormsHub, setIsConnectedToFormsHub] = useState(false);

    useEffect(() => {
        if (!sessionId) return;

        const apiBase = import.meta.env.VITE_API_URL;
        const hubBase = apiBase.replace(/\/api\/?$/, "");
        
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${hubBase}/hubs/forms?sessionId=${encodeURIComponent(sessionId)}`)
            .withAutomaticReconnect()
            .build();

        connection.start()
            .then(() => {
                console.log("Connected to hub");
                setIsConnectedToFormsHub(true);
            })
            .catch(err => console.error("SignalR connection error:", err));

        formsConnectionRef.current = connection;

        return () => {
            setIsConnectedToFormsHub(false);
            connection.stop();
        };
    }, [sessionId]);

    return { formsConnectionRef, isConnectedToFormsHub };
}