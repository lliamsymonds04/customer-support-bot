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
                    const response = await fetch(`${import.meta.env.VITE_API_URL}/session/add-user`, {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json"
                        },
                        body: JSON.stringify(sessionId),
                        credentials: "include",
                    })

                    if (!response.ok) {
                        throw new Error("Network response was not ok");
                    }

                    console.log("User added to session successfully");
                } catch {
                    console.error("Error adding user to session");
                }

            }

            addUserToSession();
        }
    }, [username, sessionId]);
}