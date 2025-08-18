import { useEffect, useState, useRef } from 'react';
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

const pageSize = 10

export function Admin() {
  const { username, role } = useUser();
  const [forms, setForms] = useState<Form[]>([]);
  const [urgency, setUrgency] = useState<FormUrgency | null>(null);
  const [category, setCategory] = useState<FormCategory | null>(null);
  const [formState, setFormState] = useState<FormState | null>(null);
  const [keyword, setKeyword] = useState("");
  const [debouncedKeyword, setDebouncedKeyword] = useState(keyword);
  const [page, setPage] = useState(1);
  const moreFormsExist = useRef(true);
  const fetchingForms = useRef(false);
  
  const bottomRef = useRef<HTMLDivElement | null>(null);


  //combobox options
  const urgencyOptions = ConvertEnumToOptions(FormUrgency, "None");
  const categoryOptions = ConvertEnumToOptions(FormCategory, "None");
  const formStateOptions = ConvertEnumToOptions(FormState, "None");

  function playNotification() {
    const audio = new Audio('/sounds/notification.mp3');
    audio.play();
  }

  async function fetchForms() {
    if (fetchingForms.current) return; // Prevent multiple fetches
    fetchingForms.current = true;
    const apiBase = import.meta.env.VITE_API_URL;
    try {
      const params = new URLSearchParams();
      if (urgency !== null && urgency !== undefined) params.append("urgency", String(urgency));
      if (formState !== null && formState !== undefined) params.append("state", String(formState));
      if (category !== null && category !== undefined) params.append("category", String(category));
      // use debounced keyword to avoid firing requests on every keystroke
      if (debouncedKeyword && debouncedKeyword.trim().length > 0) params.append("keyword", debouncedKeyword.trim());
      params.append("page", String(page));
      params.append("pageSize", String(pageSize));

      const response = await fetch(`${apiBase}/forms/admin?${params.toString()}`, {
        method: "GET",
        credentials: "include"
      });
      if (!response.ok) {
        throw new Error("Failed to fetch forms");
      }
      const data = await response.json();
      console.log("Fetched forms:", data);
      moreFormsExist.current = data.length === pageSize;
      if (page === 1) {
        setForms(data);
      } else {
        setForms((prevForms) => [...prevForms, ...data]);
      }
    } catch (error) {
      console.error("Error fetching forms:", error);
    } finally {
      fetchingForms.current = false;
    }
  }

  useEffect(() => {
    if (role?.toLowerCase() !== "admin") return;

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
          playNotification();
          // add the new form to the front
          setForms((prevForms) => [form, ...prevForms]);
        });
      })
      .catch(err => console.error("SignalR connection error:", err));

    return () => {
      connection.stop();
    };
  }, [role]);

  useEffect(() => {
    if (role?.toLowerCase() == "admin") {
      fetchForms();
    } else {
      setForms([]);
      setPage(1);
    }
  }, [page, role, debouncedKeyword, category, urgency, formState]);

  useEffect(() => {
    if (!bottomRef.current) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && moreFormsExist.current && !fetchingForms.current) {
          setPage((prevPage) => prevPage + 1);
        }
      },
      {
        root: null, // viewport
        rootMargin: "100px", // start loading a bit before it's fully in view
        threshold: 0,
      }
    );

    observer.observe(bottomRef.current);

    return () => {
      if (bottomRef.current) observer.unobserve(bottomRef.current);
    };
  }, [bottomRef.current]);

  // debounce the keyword input
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedKeyword(keyword);
    }, 300); // 300ms debounce

    return () => clearTimeout(handler);
  }, [keyword]);

  // when the debounced keyword changes, reset to page 1 and fetch
  useEffect(() => {
    if (role?.toLowerCase() === "admin") {
      setPage(1);
      fetchForms();
    }
  }, [debouncedKeyword]);

  return (
    <>
      <Header username={username} role={role} />
      <main className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col items-center">
        <div className='w-full max-w-lg mt-10'>
          <Card className='h-[800px] flex flex-col w-full'>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2 justify-center">
                <BookOpenText className="h-5 w-5 text-blue-500" />
                <span>User submitted Forms</span>
              </CardTitle>
            </CardHeader>

            <CardContent className='flex flex-col flex-1 items-center w-full'>
              <div className='flex flex-wrap gap-4 justify-center'>
                <Input placeholder="Key term" value={keyword} onChange={(e: any) => {
                  // only update raw keyword here; fetch will happen after debounce
                  setKeyword(e.target.value);
                }}/>
                <Combobox options={categoryOptions} placeholder="Category" onChange={(value) => {
                  setCategory(value as FormCategory | null);
                  setPage(1);
                }}/>
                <Combobox options={urgencyOptions} placeholder="Urgency" onChange={(value) => {
                  setUrgency(value as FormUrgency | null);
                  setPage(1);
                }}/>
                <Combobox options={formStateOptions} placeholder="State" onChange={(value) => {
                  setFormState(value as FormState | null);
                  setPage(1);
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

                <div ref={bottomRef} className="h-1" />
              </ScrollArea>
            </CardContent>
          </Card>
        </div>
      </main>
    </>
  );
}