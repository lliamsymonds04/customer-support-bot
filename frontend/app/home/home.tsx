import { useRef } from 'react';
import {Card, CardContent, CardHeader, CardTitle } from '@components/ui/card';
import { Bot, Sparkles, User} from 'lucide-react'
import { Input } from '@components/ui/input';
import { Button } from '@components/ui/button';
import {ScrollArea} from '@components/ui/scroll-area';
import { Badge } from '@components/ui/badge';
import { FormPreview } from '@components/form-preview';
import { Link } from "react-router";
import Markdown from 'react-markdown';
import BeatLoader  from 'react-spinners/BeatLoader';

//hooks
import { useChat } from '~/hooks/use-chat';
import { useError } from '~/hooks/util/user-error';
import { useForms } from '~/hooks/use-forms';
import { useSessionId } from '~/hooks/auth/use-session-id';
import { useFormsHub } from '~/hooks/use-forms-hub';
import { useAutoScroll } from '~/hooks/util/use-autoscroll';
import { useUser } from '~/hooks/auth/use-user';

export function Home() {
  const { error, setErrorMessage } = useError(3000);
  const sessionId = useSessionId(setErrorMessage);
  const { messages, input, handleInputChange, handleSubmit, isProcessing} = useChat(setErrorMessage, sessionId);
  const { formsConnectionRef, isConnectedToFormsHub } = useFormsHub(sessionId);
  const { forms } = useForms({sessionId, formsConnectionRef, isConnectedToFormsHub});
  const { username, role } = useUser();

  //ref for scroll
  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const formsEndRef = useRef<HTMLDivElement | null>(null);

  // Auto-scroll to bottom when messages change or when processing
  useAutoScroll(messagesEndRef, [messages, isProcessing])
  useAutoScroll(formsEndRef, [forms]);

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
            {username == null ? (
              <Link to="/auth/login">
                <Button variant="outline" className="flex items-center space-x-2 bg-transparent cursor-pointer">
                  <User className="h-4 w-4" />
                  <span>Login</span>
                </Button>
              </Link>
            ) : (
              <div className='flex flex-row items-center gap-4'>
                <User className="h-6 w-6 text-gray-600" />
                <span className="text-md text-gray-600">{username}: ({role})</span>
                <Link to="/auth/logout">
                  <Button variant="outline" className="flex items-center space-x-2 bg-transparent cursor-pointer">
                    <span>Logout</span>
                  </Button>
                </Link>
              </div>
            )}
          </div>
        </div>
      </header>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          {/* Chat Interface */}
          <Card className="h-[600px] flex flex-col">
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <Bot className="h-5 w-5 text-blue-500" />
                <span>Chat With AI Assistant</span>
              </CardTitle>
            </CardHeader>

            <CardContent className='flex-1 flex flex-col min-h-0'>
              {error && (
                <div className="mb-4 p-3 bg-red-100 border border-red-300 rounded-lg text-red-700 text-sm">
                  Error: {error}
                </div>
              )}

              <div className='flex-1 flex flex-col min-h-0' >
                <ScrollArea className='flex-1 min-h-0 space-y-4 pr-4'>
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

                    {messages.map((message) => (
                      <div key={message.id} className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                        <div className={`flex items-start space-x-2 max-w-[80%] ${message.role === 'user' ? 'flex-row-reverse space-x-reverse' : ''}`}>
                          <div className={`p-2 rounded-full ${message.role === 'user' ? 'bg-blue-600' : 'bg-gray-200'}`}>
                            {message.role === 'user' ? (
                              <User className="h-4 w-4 text-white" />
                            ) : (
                              <Bot className="h-4 w-4 text-gray-600" />
                            )}
                          </div>
                          <div className={`p-2 rounded-lg ${message.role === 'user' ? 'bg-blue-600 text-white' : 'bg-gray-100'}`}>
                            <Markdown >
                              {message.text}
                            </Markdown>
                          </div>
                        </div>
                      </div>
                    ))}

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

                  {/* Invisible element to scroll to */}
                  <div ref={messagesEndRef} className="h-1" />
                </ScrollArea>
              </div>

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

          <Card className="h-[600px] flex flex-col">
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <Sparkles className="h-5 w-5 text-blue-500" />
                <span>Generated Forms</span>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ScrollArea className="h-[500px]">
                {forms.length === 0 ? (
                  <div className="text-center py-16">
                    <div className="bg-gray-100 rounded-full p-4 w-16 h-16 mx-auto mb-4">
                      <Sparkles className="h-8 w-8 text-gray-400" />
                    </div>
                    <p className="text-gray-600 mb-2">No forms created yet</p>
                    <p className="text-sm text-gray-500">Start chatting to create your first form!</p>
                  </div>
                ) : (
                  <div className="space-y-6">
                    {forms.map((form) => (
                      <FormPreview key={form.id} form={form} />
                    ))}
                  </div>
                )}

                <div ref={formsEndRef} className="h-1" />
              </ScrollArea>
            </CardContent>
          </Card>


        </div>
      </div>

    </main>
  );
}