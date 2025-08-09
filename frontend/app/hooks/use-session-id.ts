import { useEffect, useState, useRef } from "react";
import type { setErrorMessage } from "./user-error";


export function useSessionId(setErrorMessage: setErrorMessage) {
    const [sessionId, setSessionId] = useState<string | null>(null);
    const isChecking = useRef(false);


    

    useEffect(() => {
        const baseUrl = import.meta.env.VITE_API_URL;

        async function checkSessionId(sessionId: string) {
            try {
                const response = await fetch(`${baseUrl}/session/${sessionId}`, {
                    method: "GET",
                });

                return response.ok;
            } catch (error) {
                setErrorMessage("Failed to check session ID");
            }

            return false;
        }

        console.log("getting session")

        async function checkAndSetSession() {
            if (isChecking.current) return; // Prevent multiple checks
            isChecking.current = true;

            const storedSessionId = localStorage.getItem("sessionId");
            let validSession = false;
            if (storedSessionId) {
                setSessionId(storedSessionId);

                //check the sessionId is still valid
                validSession = await checkSessionId(storedSessionId);
            }

            if (!validSession) {
                //create a new session
                try {
                    const response = await fetch(`${baseUrl}/session`, {
                        method: "GET",
                    });

                    if (response.ok) {
                        const newSessionId = await response.text();
                        setSessionId(newSessionId);
                        localStorage.setItem("sessionId", newSessionId);
                    }
                } catch {
                    setErrorMessage("Failed to create session");
                }
                
            }

            isChecking.current = false;
        };

        checkAndSetSession();
    }, []);

    return sessionId;
}
