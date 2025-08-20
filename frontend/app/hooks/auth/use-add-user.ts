import { useEffect } from "react"

interface useAddUserProps {
    username: string | null,
    sessionId: string | null
}

export function useAddUser({username, sessionId}: useAddUserProps) {
    useEffect(() => {
        if (username && sessionId) {
            // Logic to add user to the session
            async function addUserToSession() {
                try {
                    const response = await fetch(`${import.meta.env.VITE_API_URL}/auth/add-user`, {
                        method: "POST",
                        credentials: "include",
                    })

                    if (!response.ok) {
                        throw new Error("Network response was not ok");
                    }
                } catch {
                    console.error("Error adding user to session");
                }

            }

            addUserToSession();
        }
    }, [username, sessionId]);
}