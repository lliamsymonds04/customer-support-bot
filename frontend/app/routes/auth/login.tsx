import type { Route } from "../auth/+types/login";
import { Login } from "../../auth/login";

export function meta({}: Route.MetaArgs) {
  return [
    { title: "Login" },
    { name: "description", content: "Welcome to the Login page!" },
  ];
}

export default function Main() {
  return <Login />;
}
