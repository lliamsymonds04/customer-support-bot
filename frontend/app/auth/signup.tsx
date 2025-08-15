import { useState } from "react";
import {
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@components/ui/card";
import { Button } from "~/components/ui/button";
import { Label } from "@components/ui/label";
import { Input } from "@components/ui/input";
import { Link } from "react-router";
import { Eye, EyeOff, Mail, Lock } from "lucide-react";
import ContinueWith from "./components/continue-with";

export function Signup() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    // Handle login logic here
  }

  return (
    <>
      <CardHeader>
        <CardTitle className="text-2xl text-center">Welcome Back</CardTitle>
        <CardDescription className="text-center">
          Enter your credentials to access your account
        </CardDescription>
      </CardHeader>
    </>
  );
}
