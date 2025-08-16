import { useState } from 'react';
import {  CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@components/ui/card";
import { Button } from "~/components/ui/button";
import { Label } from "@components/ui/label";
import { Input } from "@components/ui/input";
import { Link } from 'react-router';
import { Eye, EyeOff, Lock, User } from 'lucide-react';
import ContinueWith from './components/continue-with';

export function Login() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [isLoading, setIsLoading] = useState(false);
    const [rememberMe, setRememberMe] = useState(false);

    async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
			event.preventDefault();
			// Handle login logic here
			
			setIsLoading(true);
			try {
				const baseUrl = import.meta.env.VITE_API_URL;
				const response = await fetch(`${baseUrl}/auth/login`, {
					method: 'POST',
					headers: {
						'Content-Type': 'application/json',
					},
					body: JSON.stringify({ username, password, rememberMe }),
					credentials: 'include'
				});

				if (!response.ok) {
					throw new Error('Login failed');
				}

				const data = await response.json();
				console.log('Login successful:', data);

				//nav to home
				if (rememberMe) {
					localStorage.setItem("rememberMe", "true");
				}

				localStorage.setItem("username", username);
				localStorage.setItem("role", data.role);
				// Navigate to home

				window.location.href = '/';
			} catch (error) {
				console.error('Error logging in:', error);
			} finally {
				setIsLoading(false);
			}
    }

    return (
			<>
				<CardHeader>
					<CardTitle className="text-2xl text-center">Welcome Back</CardTitle>
					<CardDescription className="text-center">Enter your credentials to access your account</CardDescription>
				</CardHeader>
				<CardContent className="space-y-4">
					<form onSubmit={handleSubmit} className="space-y-4">
						<div className="space-y-2">
							<Label htmlFor="username">Username</Label>
							<div className="relative">
								<User className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
								<Input
									type="username"
									placeholder="Enter your username"
									value={username}
									onChange={(e) => setUsername(e.target.value)}
									className="pl-10"
									required
									disabled={isLoading}
								/>
							</div>
						</div>

						<div className="space-y-2">
							<Label htmlFor="password">Password</Label>
							<div className="relative">
								<Lock className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
								<Input
									id="password"
									type={showPassword ? 'text' : 'password'}
									placeholder="Enter your password"
									value={password}
									onChange={(e) => setPassword(e.target.value)}
									className="pl-10 pr-10"
									required
									disabled={isLoading}
								/>
								<button
									type="button"
									onClick={() => setShowPassword(!showPassword)}
									className="absolute right-3 top-3 text-gray-400 hover:text-gray-600"
									disabled={isLoading}
								>
									{showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
								</button>
							</div>
						</div>

						<div className="flex items-center justify-between">
							<div className="flex items-center space-x-2">
								<input
									id="remember"
									type="checkbox"
									className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
									checked={rememberMe}
									onChange={(e) => setRememberMe(e.target.checked)}
									disabled={isLoading}
								/>
								<Label htmlFor="remember" className="text-sm text-gray-600 bg-transparent">
									Remember me
								</Label>
							</div>
							<Link to="/auth/forgot-password" className="text-sm text-blue-600 hover:text-blue-800">
								Forgot password?
							</Link>
						</div>

						<Button type="submit" className="w-full" disabled={isLoading}>
							{isLoading ? (
								<div className="flex items-center space-x-2">
									<div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
									<span>Signing in...</span>
								</div>
							) : (
								'Sign in'
							)}
						</Button>
					</form>

					<ContinueWith isLoading={isLoading} />
				</CardContent> 

				<CardFooter className="flex flex-col space-y-4">
						<div className="text-sm text-center text-gray-600">
							Don't have an account?{' '}
							<Link to="/auth/signup" className="text-blue-600 hover:text-blue-800 font-medium">
								Sign up
							</Link>
						</div>
						
						<div className="text-xs text-center text-gray-500">
							<p className="mb-2">Demo credentials:</p>
							<p>Email: demo@example.com</p>
							<p>Password: password</p>
						</div>
				</CardFooter>
			</>
	)
}