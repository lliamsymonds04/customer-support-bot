import { useState } from "react";


export enum FormCategory
{
    General,
    Technical,
    Billing,
    Feedback,
    Account,
}

export enum FormUrgency
{
    Low,
    Medium,
    High,
    Critical
}

interface Form
{
    title: string;
    description: string;
    category: FormCategory;
    urgency: FormUrgency;
    createdAt: Date;
}

export function useForms() {
  const [forms, setForms] = useState<Form[]>([]);

  return {
    forms,
  };
}