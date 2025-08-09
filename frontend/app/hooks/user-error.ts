import { useEffect, useState } from "react";

export type setErrorMessage = (msg: string) => void;

export function useError(timeoutMs: number) {
    const [error, setError] = useState<string | null>(null);

    function setErrorMessage(msg: string) {
        setError(msg);
        const timer = setTimeout(() => {
            setError(null);
        }, timeoutMs);
    }


    return { error, setErrorMessage };
}