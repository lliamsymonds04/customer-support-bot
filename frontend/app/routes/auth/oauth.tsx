import type { Route } from "./+types/oauth";
import { OAuth } from "../../auth/oauth";

export function meta({}: Route.MetaArgs) {
  return [
    { title: "..." },
    { name: "description", content: "Finalizing OAuth" },
  ];
}

export default function Main() {
  return <OAuth />;
}

