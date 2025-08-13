import type { Form } from "~/hooks/use-forms"
import { FormCategory, FormUrgency } from "~/hooks/use-forms";
import { Card, CardTitle, CardContent, CardHeader } from "~/components/ui/card";


const urgencyColorMap: Record<FormUrgency, string> = {
    [FormUrgency.Low]: "#34D399",
    [FormUrgency.Medium]: "yellow",
    [FormUrgency.High]: "red",
    [FormUrgency.Critical]: "purple",
};

export function FormPreview({ form }: { form: Form }) {
  return (
    <Card>
        <CardContent>
            <div className="flex flex-row items-center gap-4">
                <div className={`rounded-full h-4 w-4`} style={{ backgroundColor: urgencyColorMap[form.urgency] }}></div>
                <h3 className="text-lg "><span className="font-semibold">Urgency: </span>{FormUrgency[form.urgency]} </h3>
                <h3 className="text-lg "><span className="font-semibold">Category: </span>{FormCategory[form.category]}</h3>

            </div>
            <div className="flex flex-col ml-10">
                <p className="text-md text-gray-600 text-wrap">{form.description}</p>
                <p className="text-sm text-gray-500 mt-2">
                    <span className="font-semibold">Date: </span>
                    {form.createdAt ? new Date(form.createdAt).toLocaleString() : "N/A"}
                </p>
            </div>
            
        </CardContent>
    </Card>
  );
}