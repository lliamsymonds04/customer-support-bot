"use client"

import * as React from "react"
import { useMemo } from "react"
import { Check, ChevronsUpDown } from "lucide-react"

import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"


export interface ComboboxOption {
    value: string;
    label: string;
}

interface ComboboxProps {
    options: ComboboxOption[];
    placeholder?: string;
    onChange?: (value: string) => void;
}

export function Combobox({ options, placeholder = "Select option...", onChange }: ComboboxProps) {
    const [open, setOpen] = React.useState(false);
    const [value, setValue] = React.useState("");

    const handleSelect = (currentValue: string) => {
        const newValue = currentValue === value ? "" : currentValue;
        setValue(newValue);
        setOpen(false);
        if (onChange) onChange(newValue);
    };

    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
                <Button
                    variant="outline"
                    role="combobox"
                    aria-expanded={open}
                    className="w-[200px] justify-between"
                >
                    {value
                        ? options.find((option) => option.value === value)?.label
                        : placeholder}
                    <ChevronsUpDown className="opacity-50" />
                </Button>
            </PopoverTrigger>
            <PopoverContent className="w-[200px] p-0">
                <Command>
                    <CommandInput placeholder={`Search ${placeholder.toLowerCase()}`} className="h-9" />
                    <CommandList>
                        <CommandEmpty>No option found.</CommandEmpty>
                        <CommandGroup>
                            {options.map((option) => (
                                <CommandItem
                                    key={option.value}
                                    value={option.value}
                                    onSelect={handleSelect}
                                >
                                    {option.label}
                                    <Check
                                        className={cn(
                                            "ml-auto",
                                            value === option.value ? "opacity-100" : "opacity-0"
                                        )}
                                    />
                                </CommandItem>
                            ))}
                        </CommandGroup>
                    </CommandList>
                </Command>
            </PopoverContent>
        </Popover>
    );
}

export function ConvertEnumToOptions<T extends object>(enumObj: T, noneConfig?: string): ComboboxOption[] {
    return useMemo(() => {
        const obj = Object.entries(enumObj)
            .filter(([key]) => isNaN(Number(key))) // Filter out numeric keys (for reverse-mapped enums)
            .map(([key, value]) => ({
                value: String(value),
                label: key,
            }))

        return noneConfig ? [...obj, { value: "", label: noneConfig }] : obj;
    }, [enumObj, noneConfig])
}