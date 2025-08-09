import {Card, CardContent, CardHeader, CardTitle } from '@components/ui/card';
import { Bot, Sparkles } from 'lucide-react'
import { Input } from '@components/ui/input';
import { useChat } from '~/hooks/use-chat';
import { Button } from '@components/ui/button';

export function Home() {
  const { messages, input, handleInputChange } = useChat();

  return (
    <main className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      {/* Header */}
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex items-center justify-between w-full">
            <div className="flex items-center space-x-3">
              <div className="bg-gradient-to-r from-blue-600 to-indigo-600 p-2 rounded-lg">
                <Sparkles className="h-6 w-6 text-white" />
              </div>
              <div>
                <h1 className="text-2xl font-bold text-gray-800">Welcome to the Support Bot</h1>
                <p className="text-gray-600">Your AI-powered customer support assistant.</p>
              </div>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          <Card className="bg-white shadow rounded-lg p-6">
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <Bot className="h-5 w-5 text-blue-500" />
                <span>Chat With AI Assistant</span>
              </CardTitle>
            </CardHeader>

            <CardContent>
              <p className="text-gray-600">Start a conversation with our AI assistant.</p>

              <form className='mt-4 flex space-x-2'>
                <Input
                  type='text'
                  placeholder='Type your message here...'
                  className='flex-1'
                  value={input}
                  onChange={handleInputChange}
                />

                <Button
                  type="submit"
                  className={`px-4 ${input.length === 0 ? 'bg-gray-300 text-gray-500 cursor-not-allowed' : 'bg-blue-600 text-white hover:bg-blue-700 cursor-pointer'}`}
                  disabled={input.length === 0}
                >
                  Send
                </Button>
              </form>
            </CardContent>

          </Card>


        </div>
      </div>

    </main>
  );
}