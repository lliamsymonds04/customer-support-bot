export enum FormCategory
{
    General = "General",
    Technical = "Technical",
    Billing = "Billing",
    Feedback = "Feedback",
    Account = "Account",
    Request = "Request",
}

export enum FormUrgency
{
    Low = "Low",
    Medium = "Medium",
    High = "High",
    Critical = "Critical"
}

export enum FormState
{
    Open = "Open",
    InProgress = "In Progress",
    Closed = "Closed",
}

export interface Form
{
    id: number;
    title: string;
    description: string;
    category: FormCategory;
    urgency: FormUrgency;
    createdAt: Date;
}