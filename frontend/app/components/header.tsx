import { Sparkles, User, House } from 'lucide-react'
import { Button } from '@components/ui/button';
import { Link, useLocation } from "react-router";

interface HeaderProps {
  username: string | null;
  role: string | null;
}

export function Header({username, role}: HeaderProps) {
  const location = useLocation();
  const isAdminPage = location.pathname.startsWith("/admin");
  const isHomePage = location.pathname === "/";

  return (
    <header className="bg-white shadow-sm border-b">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div className="flex items-center justify-between w-full">
          <div className="flex items-center space-x-3">
            <div className="bg-gradient-to-r from-blue-600 to-indigo-600 p-2 rounded-lg">
              <Sparkles className="h-6 w-6 text-white" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-gray-800">
                Welcome to the Support Bot
              </h1>
              <p className="text-gray-600">
                Your AI-powered customer support assistant.
              </p>
            </div>
          </div>
          {username == null ? (
            <Link to="/auth/login">
              <Button
                variant="outline"
                className="flex items-center space-x-2 bg-transparent cursor-pointer"
              >
                <User className="h-4 w-4" />
                <span>Login</span>
              </Button>
            </Link>
          ) : (
            <div className="flex flex-row items-center">
              <User className="h-6 w-6 text-gray-600" />
              <span className="text-md mr-4">
                ({role}): {username}
              </span>
              {(role == "Admin" && !isAdminPage) && (
                <Link to="/admin">
                  <Button
                    variant="outline"
                    className="flex items-center space-x-2 bg-transparent cursor-pointer mr-4"
                  >
                    <span>Admin Panel</span>
                  </Button>
                </Link>
              )}
              

              <Link to="/auth/logout">
                <Button
                  variant="outline"
                  className="flex items-center space-x-2 bg-transparent cursor-pointer"
                >
                  <span>Logout</span>
                </Button>
              </Link>

              {!isHomePage && (
                <Link to="/" className='ml-4'>
                  <Button
                    variant="outline"
                    className="flex items-center space-x-2 bg-transparent cursor-pointer mr-4"
                  >
                    <House className="h-4 w-4" />
                  </Button>
                </Link>
              )}
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
