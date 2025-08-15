import type { Route } from "./+types/logout";
import { Logout } from "../../auth/logout";

export function meta({}: Route.MetaArgs) {
  return [
    { title: "Logout" },
    { name: "description", content: "Welcome to the Logout page!" },
  ];
}

export default function Main() {
  return <Logout />;
}

