import type { Route } from "./+types/admin";
import { Admin } from "../admin/admin";

export function meta({}: Route.MetaArgs) {
  return [
    { title: "Admin Panel" },
    { name: "description", content: "Welcome to the Admin Panel!" },
  ];
}

export default function Main() {
  return <Admin />;
}
