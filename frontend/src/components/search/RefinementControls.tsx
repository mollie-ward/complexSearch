'use client';

import { useState } from 'react';
import { Sheet, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Slider } from '@/components/ui/slider';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Filter } from 'lucide-react';
import { RefinementFilters } from '@/lib/api/types';

interface RefinementControlsProps {
  onRefine: (filters: RefinementFilters, query: string) => void;
  isLoading?: boolean;
}

const MAKES = [
  'All Makes',
  'Audi',
  'BMW',
  'Ford',
  'Mercedes-Benz',
  'Toyota',
  'Volkswagen',
  'Volvo',
];

const FUEL_TYPES = [
  'All Fuel Types',
  'Petrol',
  'Diesel',
  'Electric',
  'Hybrid',
];

const TRANSMISSIONS = [
  'All Transmissions',
  'Manual',
  'Automatic',
];

const YEARS = Array.from({ length: 25 }, (_, i) => new Date().getFullYear() - i);

export function RefinementControls({ onRefine, isLoading }: RefinementControlsProps) {
  const [open, setOpen] = useState(false);
  const [priceRange, setPriceRange] = useState<[number, number]>([0, 50000]);
  const [mileageRange, setMileageRange] = useState<[number, number]>([0, 150000]);
  const [make, setMake] = useState<string>('All Makes');
  const [fuelType, setFuelType] = useState<string>('All Fuel Types');
  const [transmission, setTransmission] = useState<string>('All Transmissions');
  const [yearMin, setYearMin] = useState<string>('Any Year');

  const formatPrice = (value: number) => {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: 'GBP',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(value);
  };

  const formatMileage = (value: number) => {
    return new Intl.NumberFormat('en-GB').format(value);
  };

  const handleApply = () => {
    const filters: RefinementFilters = {
      priceRange: priceRange,
      mileageRange: mileageRange,
    };

    if (make !== 'All Makes') {
      filters.make = make;
    }

    if (fuelType !== 'All Fuel Types') {
      filters.fuelType = fuelType;
    }

    if (transmission !== 'All Transmissions') {
      filters.transmission = transmission;
    }

    if (yearMin !== 'Any Year') {
      filters.yearMin = parseInt(yearMin);
    }

    // Build natural language query
    const parts: string[] = [];
    if (make !== 'All Makes') parts.push(make);
    if (fuelType !== 'All Fuel Types') parts.push(fuelType);
    if (transmission !== 'All Transmissions') parts.push(transmission);
    
    parts.push(`under ${formatPrice(priceRange[1])}`);
    parts.push(`with less than ${formatMileage(mileageRange[1])} miles`);
    
    if (yearMin !== 'Any Year') {
      parts.push(`from ${yearMin} or newer`);
    }

    const query = parts.join(' ');
    onRefine(filters, query);
    setOpen(false);
  };

  const handleReset = () => {
    setPriceRange([0, 50000]);
    setMileageRange([0, 150000]);
    setMake('All Makes');
    setFuelType('All Fuel Types');
    setTransmission('All Transmissions');
    setYearMin('Any Year');
  };

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button variant="outline" size="sm">
          <Filter className="h-4 w-4 mr-2" />
          Refine Results
        </Button>
      </SheetTrigger>
      <SheetContent side="right" className="w-full sm:max-w-md overflow-y-auto">
        <SheetHeader>
          <SheetTitle>Refine Search</SheetTitle>
          <SheetDescription>
            Adjust filters to narrow down your search results
          </SheetDescription>
        </SheetHeader>

        <div className="space-y-6 py-6">
          {/* Price Range */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Price Range: {formatPrice(priceRange[0])} - {formatPrice(priceRange[1])}
            </label>
            <Slider
              data-testid="price-slider"
              value={priceRange}
              onValueChange={(value) => setPriceRange(value as [number, number])}
              min={0}
              max={50000}
              step={1000}
              className="w-full"
            />
          </div>

          {/* Mileage Range */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Mileage Range: {formatMileage(mileageRange[0])} - {formatMileage(mileageRange[1])} miles
            </label>
            <Slider
              data-testid="mileage-slider"
              value={mileageRange}
              onValueChange={(value) => setMileageRange(value as [number, number])}
              min={0}
              max={150000}
              step={5000}
              className="w-full"
            />
          </div>

          {/* Make */}
          <div className="space-y-2">
            <label className="text-sm font-medium">Make</label>
            <Select value={make} onValueChange={setMake}>
              <SelectTrigger data-testid="make-select">
                <SelectValue placeholder="Select make" />
              </SelectTrigger>
              <SelectContent>
                {MAKES.map((m) => (
                  <SelectItem key={m} value={m}>
                    {m}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Fuel Type */}
          <div className="space-y-2">
            <label className="text-sm font-medium">Fuel Type</label>
            <Select value={fuelType} onValueChange={setFuelType}>
              <SelectTrigger data-testid="fuel-type-select">
                <SelectValue placeholder="Select fuel type" />
              </SelectTrigger>
              <SelectContent>
                {FUEL_TYPES.map((f) => (
                  <SelectItem key={f} value={f}>
                    {f}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Transmission */}
          <div className="space-y-2">
            <label className="text-sm font-medium">Transmission</label>
            <Select value={transmission} onValueChange={setTransmission}>
              <SelectTrigger data-testid="transmission-select">
                <SelectValue placeholder="Select transmission" />
              </SelectTrigger>
              <SelectContent>
                {TRANSMISSIONS.map((t) => (
                  <SelectItem key={t} value={t}>
                    {t}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Year Minimum */}
          <div className="space-y-2">
            <label className="text-sm font-medium">Minimum Year</label>
            <Select value={yearMin} onValueChange={setYearMin}>
              <SelectTrigger data-testid="year-select">
                <SelectValue placeholder="Select year" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Any Year">Any Year</SelectItem>
                {YEARS.map((year) => (
                  <SelectItem key={year} value={year.toString()}>
                    {year}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        <SheetFooter className="gap-2">
          <Button variant="outline" onClick={handleReset} disabled={isLoading}>
            Reset
          </Button>
          <Button onClick={handleApply} disabled={isLoading}>
            Apply Filters
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}
