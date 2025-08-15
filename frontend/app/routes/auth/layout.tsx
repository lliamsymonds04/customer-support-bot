import { Outlet, Link, useLocation } from "react-router";
import { Card } from "@components/ui/card";
import { Sparkles } from "lucide-react";

export default function AuthLayout() {
  const { pathname } = useLocation();
  const heading = pathname.endsWith("/login")
    ? "Log In to Your Account"
    : pathname.endsWith("/signup")
    ? "Create a New Account"
    : "";

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-md">
        {/* Header */}
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex items-center space-x-2 text-2xl font-bold text-gray-900 hover:text-blue-600 transition-colors">
            <div className="bg-gradient-to-r from-blue-600 to-indigo-600 p-2 rounded-lg">
              <Sparkles className="h-6 w-6 text-white" />
            </div>
            <span>AI Support Bot</span>
          </Link>
          <p className="text-gray-600 mt-2">{heading}</p>
        </div>

          <Card className="shadow-lg">
            <Outlet />
          </Card>
        </div>
    </div>
  );
}