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
                <h3 className="text-lg font-semibold">Urgency: {FormUrgency[form.urgency]} </h3>
                <h3 className="text-lg font-semibold">Category: {FormCategory[form.category]}</h3>

            </div>
            <p className="ml-10 text-md text-gray-600 text-wrap">{form.description}</p>
        </CardContent>
    </Card>
  );
}