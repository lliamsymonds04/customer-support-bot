import { useEffect } from "react";
import { CardHeader, CardTitle } from "~/components/ui/card";

export function Logout() {

    useEffect(() => {
        const baseUrl = import.meta.env.VITE_API_URL;

        async function logout() {
            const response = await fetch(`${baseUrl}/auth/logout`, {
                method: "POST",
                credentials: "include",
            });
            if (!response.ok) throw new Error("Failed to logout");

            localStorage.removeItem("username");
            localStorage.removeItem("role");

            window.location.href = "/";
        }

        logout();
    });

    return (
        <CardHeader>
            <CardTitle className="text-2xl text-center">Logging out...</CardTitle>
        </CardHeader>
    );
}