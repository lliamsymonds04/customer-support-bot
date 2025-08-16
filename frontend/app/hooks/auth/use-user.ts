import { useEffect, useState } from "react";

export function useUser() {
    const [username, setUsername] = useState<string | null>(null);
    const [role, setRole] = useState<string | null>(null);

    useEffect(() => {
        const baseUrl = import.meta.env.VITE_API_URL;

        async function check() {
            const response = await fetch(`${baseUrl}/auth/check`, {
                method: "GET",
                credentials: "include",
            });
            if (!response.ok) throw new Error("Failed to check user authentication");
        }

        async function refresh() {
            const response = await fetch(`${baseUrl}/auth/refresh`, {
                method: "GET",
                credentials: "include",
            });
            if (!response.ok) throw new Error("Failed to refresh token");
        }

        function loadUser() {
            const storedUsername = localStorage.getItem("username");
            const storedRole = localStorage.getItem("role");

            if (!storedUsername) return;

            check()
                .then(() => {
                    setUsername(storedUsername);
                    setRole(storedRole);
                })
                .catch(() => {
                    const rememberMe = localStorage.getItem("rememberMe") === "true";
                    if (rememberMe) {
                        refresh()
                            .then(() => {
                                setUsername(storedUsername);
                                setRole(storedRole);
                            })
                            .catch(console.error);
                    } else {
                        setUsername(null);
                        setRole(null);
                    }
                });
        }

        loadUser();

        // Ping every 5 minutes (300000 ms)
        const intervalId = setInterval(loadUser, 300000);

        // Cleanup
        return () => clearInterval(intervalId);
    }, []);

    return { username, role };
}