import { Header } from '@components/header';
import { Card, CardTitle, CardContent, CardHeader } from '@components/ui/card';
import { useUser } from '~/hooks/auth/use-user';
import { Combobox, ConvertEnumToOptions } from '~/components/ui/combobox';
import type { Form } from "@/types/Form";
import { BookOpenText } from 'lucide-react';
import { Separator } from '@components/ui/separator';
import { FormCategory, FormUrgency } from "@/types/Form";

export function Admin() {
  const { username, role } = useUser();
  const urgencyOptions = ConvertEnumToOptions(FormUrgency, "None");
  const categoryOptions = ConvertEnumToOptions(FormCategory, "None");

  return (
    <>
      <Header username={username} role={role} />
      <main className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col items-center">
        <div className='w-full max-w-lg mt-10'>
          <Card className='h-[600px] flex flex-col'>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2 justify-center">
                <BookOpenText className="h-5 w-5 text-blue-500" />
                <span>User submitted Forms</span>
              </CardTitle>
            </CardHeader>

            <CardContent className='flex flex-col flex-1 items-center'>
              <div className='flex flex-grid gap-4'>
                <Combobox options={categoryOptions} placeholder="Category" onChange={(value) => {

                }}/>
                <Combobox options={urgencyOptions} placeholder="Urgency" onChange={(value) => {

                }}/>
              </div>
              <Separator className="my-4" />
            </CardContent>
          </Card>
        </div>
      </main>
    </>
  );
}