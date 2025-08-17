import { useEffect, useState } from 'react';
import { Header } from '@components/header';
import { Card, CardTitle, CardContent, CardHeader } from '@components/ui/card';
import { useUser } from '~/hooks/auth/use-user';
import { Combobox, ConvertEnumToOptions } from '~/components/ui/combobox';
import { BookOpenText } from 'lucide-react';
import { Separator } from '@components/ui/separator';
import { Input } from '~/components/ui/input';
import { Sparkles } from 'lucide-react';
import * as signalR from "@microsoft/signalr"

//types
import type { Form } from "@/types/Form";
import { FormCategory, FormState, FormUrgency } from "@/types/Form";
import { ScrollArea } from '~/components/ui/scroll-area';
import { FormPreview } from '~/components/form-preview';

export function Admin() {
  const { username, role } = useUser();
  const [forms, setForms] = useState<Form[]>([]);

  //combobox options
  const urgencyOptions = ConvertEnumToOptions(FormUrgency, "None");
  const categoryOptions = ConvertEnumToOptions(FormCategory, "None");
  const formStateOptions = ConvertEnumToOptions(FormState, "None");

  useEffect(() => {
    const apiBase = import.meta.env.VITE_API_URL;
    const hubBase = apiBase.replace(/\/api\/?$/, "");

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${hubBase}/hubs/admin`, {
        withCredentials: true
      })
      .withAutomaticReconnect()
      .build();

    connection.start()
      .then(() => {
        console.log("Connected to admin hub");
        connection.on("AdminReceiveForm", (form) => {
          console.log("Received form:", form);
          setForms((prevForms) => [...prevForms, form]);
        });
      })
      .catch(err => console.error("SignalR connection error:", err));

    return () => {
      connection.stop();
    };
  }, []);

  return (
    <>
      <Header username={username} role={role} />
      <main className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col items-center">
        <div className='w-full max-w-lg mt-10'>
          <Card className='h-[800px] flex flex-col'>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2 justify-center">
                <BookOpenText className="h-5 w-5 text-blue-500" />
                <span>User submitted Forms</span>
              </CardTitle>
            </CardHeader>

            <CardContent className='flex flex-col flex-1 items-center'>
              <div className='flex flex-wrap gap-4 justify-center'>
                <Input placeholder="Key term" onChange={(value) => {

                }}/>
                <Combobox options={categoryOptions} placeholder="Category" onChange={(value) => {

                }}/>
                <Combobox options={urgencyOptions} placeholder="Urgency" onChange={(value) => {

                }}/>
                <Combobox options={formStateOptions} placeholder="State" onChange={(value) => {

                }}/>
                
              </div>
              <Separator className="my-4" />
              <ScrollArea className="h-[500px]">
                {forms.length === 0 ? (
                  <div className="text-center py-16">
                    <div className="bg-gray-100 rounded-full p-4 w-16 h-16 mx-auto mb-4">
                      <Sparkles className="h-8 w-8 text-gray-400" />
                    </div>
                    <p className="text-gray-600 mb-2">Loading forms...</p>
                    <p className="text-sm text-gray-500">Please wait a moment</p>
                  </div>
                ) : (
                  <div className="space-y-6">
                    {forms.map((form) => (
                      <FormPreview key={form.id} form={form} />
                    ))}
                  </div>
                )}

                {/* <div ref={formsEndRef} className="h-1" /> */}
              </ScrollArea>
            </CardContent>
          </Card>
        </div>
      </main>
    </>
  );
}