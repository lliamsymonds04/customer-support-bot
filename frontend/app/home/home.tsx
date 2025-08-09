import {Card, CardContent, CardHeader, CardTitle } from '@components/ui/card';
import { Bot, Sparkles } from 'lucide-react'
import { Input } from '@components/ui/input';
import { useChat } from '~/hooks/use-chat';
import { Button } from '@components/ui/button';
import { useError } from '~/hooks/user-error';
import BeatLoader  from 'react-spinners/BeatLoader';
import { ScrollArea } from '@radix-ui/react-scroll-area';
import { Badge } from '@components/ui/badge';

export function Home() {
  const { error, setErrorMessage } = useError(3000);
  const { messages, input, handleInputChange, handleSubmit, isProcessing} = useChat(setErrorMessage);

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
              {error && (
                <div className="mb-4 p-3 bg-red-100 border border-red-300 rounded-lg text-red-700 text-sm">
                  Error: {error}
                </div>
              )}

              <ScrollArea className='flex-1 pr-4'>
                <div className='space-y-4'>
                  {messages.length === 0 && (
                    <div className="text-center py-8">
                      <Bot className='h-12 w-12 text-gray-400 mx-auto mb-4' />
                      <p className="text-gray-600 mb-2">Hi! I'm an AI assistant here to help.</p>
                      <p className="text-sm text-gray-500">Tell me what problem you're facing.</p>
                      <div className="mt-4 space-y-2">
                        <Badge variant="outline" className="mr-2">Issues</Badge>
                        <Badge variant="outline" className="mr-2">Bugs</Badge>
                        <Badge variant="outline">Feedback</Badge>
                      </div>
                    </div>
                  )}

                  {isProcessing && (
                    <div className="flex justify-start">
                      <div className="flex items-center space-x-2">
                        <div className="p-2 rounded-full bg-gray-200">
                          <Bot className="h-4 w-4 text-gray-600" />
                        </div>
                        <div className="p-2 rounded-lg bg-gray-100">
                          <BeatLoader size={8} color="blue" />
                        </div>
                      </div>
                    </div>
                  )}

                </div>
              </ScrollArea>

              <form className='mt-4 flex space-x-2 items-center' onSubmit={handleSubmit}>
                <Input
                  type='text'
                  placeholder='Type your message here...'
                  className='flex-1'
                  value={input}
                  onChange={handleInputChange}
                />

                <Button
                  type="submit"
                  className={`px-4 ${input.length === 0 || isProcessing ? 'bg-gray-300 text-gray-500 cursor-not-allowed' : 'bg-blue-600 text-white hover:bg-blue-700 cursor-pointer'}`}
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