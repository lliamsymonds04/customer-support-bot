import type { Route } from "../auth/+types/signup";
import { Signup } from "../../auth/signup";

export function meta({}: Route.MetaArgs) {
  return [
    { title: "Signup" },
    { name: "description", content: "Signing out..." },
  ];
}

export default function Main() {
  return <Signup />;
}

