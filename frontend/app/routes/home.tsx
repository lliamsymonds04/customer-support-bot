import type { Route } from "./+types/home";
import { Home } from "../home/home";

export function meta({}: Route.MetaArgs) {
  return [
    { title: "Support Bot" },
    { name: "description", content: "Welcome to the Support Bot!" },
  ];
}

export default function Main() {
  return <Home />;
}
