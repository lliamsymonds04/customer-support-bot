import { useEffect } from "react";
import { CardHeader, CardTitle } from "~/components/ui/card";


export function OAuth() {
    useEffect(() => {
        //get role from params
        const urlParams = new URLSearchParams(window.location.search);
        const role = urlParams.get("role");
        const username = urlParams.get("username");

        localStorage.setItem("role", role || "User");
        localStorage.setItem("username", username || "Guest");
        localStorage.setItem("rememberMe", "true");

        window.location.href = "/";
    }, []);

    return (
        <CardHeader>
            <CardTitle> Finishing OAuth </CardTitle>
        </CardHeader>
    )

}