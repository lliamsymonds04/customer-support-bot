import { useEffect } from "react";

export function useAutoScroll(
  ref: React.RefObject<HTMLElement | null>,
  deps: React.DependencyList = []
) {
  useEffect(() => {
    const scrollToBottom = () => {
      if (ref.current) {
        ref.current.scrollIntoView({
          behavior: "smooth",
          block: "end",
        });
      }
    };

    const timeoutId = setTimeout(scrollToBottom, 100);

    return () => clearTimeout(timeoutId);
  }, deps); // deps could be [messages, isProcessing], etc.
}