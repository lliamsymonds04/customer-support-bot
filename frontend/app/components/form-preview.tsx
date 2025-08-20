import type { Form } from "@/types/Form"
import { FormCategory, FormUrgency, FormState } from "@/types/Form";
import { Card, CardContent } from "~/components/ui/card";
import { Combobox, ConvertEnumToOptions } from "./ui/combobox";


const urgencyColorMap: Record<FormUrgency, string> = {
    [FormUrgency.Low]: "#34D399",
    [FormUrgency.Medium]: "yellow",
    [FormUrgency.High]: "red",
    [FormUrgency.Critical]: "purple",
};


interface FormPreviewProps {
    form: Form;
    role: string | null;
}

export function FormPreview({ form, role }: FormPreviewProps) {

  const FormStateOptions = ConvertEnumToOptions(FormState, "");

  async function updateFormState(newState: string) {
    try {
      form.state = newState as FormState; // Update the form state locally
      const response = await fetch(`${import.meta.env.VITE_API_URL}/forms/${form.id}/state`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(newState),
        credentials: "include"
      });

      if (!response.ok) {
        throw new Error("Failed to update form state");
      }
    } catch (error) {
      if (error instanceof Error) {
        console.error(error.message);
      }
    }
  }
  return (
    <Card>
        <CardContent>
            <div className="flex flex-row items-center gap-4">
                <div className={`rounded-full h-4 w-4`} style={{ backgroundColor: urgencyColorMap[form.urgency] }}></div>
                <h3 className="text-lg "><span className="font-semibold">Urgency: </span>{FormUrgency[form.urgency]} </h3>
                <h3 className="text-lg "><span className="font-semibold">Category: </span>{FormCategory[form.category]}</h3>

            </div>
            <div className="ml-10">
                <p className="text-md text-gray-600 text-wrap">{form.description}</p>
                <p className="text-sm text-gray-500 mt-2">
                    <span className="font-semibold">Date: </span>
                    {form.createdAt ? new Date(form.createdAt).toLocaleString() : "N/A"}
                </p>
                
            </div>
            {role === "Admin" && (
              <div className="w-48 mt-4">
                <Combobox options={FormStateOptions} onChange={updateFormState} placeholder={form.state} />
              </div>
            )}
        </CardContent>
    </Card>
  );
}