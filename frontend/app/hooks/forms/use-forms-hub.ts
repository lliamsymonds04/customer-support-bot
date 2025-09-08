import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

export function useFormsHub(sessionId: string | null) {
    const formsConnectionRef = useRef<signalR.HubConnection | null>(null);
    const [isConnectedToFormsHub, setIsConnectedToFormsHub] = useState(false);

    useEffect(() => {
        if (!sessionId) return;

        let hubBase = import.meta.env.VITE_HUB_BASE
        if (!hubBase) {
            const apiBase = import.meta.env.VITE_API_URL;
            hubBase = apiBase.replace(/\/api\/?$/, "");
        }
        
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${hubBase}/hubs/forms?sessionId=${encodeURIComponent(sessionId)}`, {
                withCredentials: true,
                transport: signalR.HttpTransportType.LongPolling
            })
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