'use client';

import { VehicleDocument } from '@/lib/api/types';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface VehicleSpecificationsProps {
  vehicle: VehicleDocument;
}

export function VehicleSpecifications({ vehicle }: VehicleSpecificationsProps) {
  const formatDate = (date?: string) => {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-GB', {
      year: 'numeric',
      month: 'long',
    });
  };

  const specifications = [
    { label: 'Make', value: vehicle.make },
    { label: 'Model', value: vehicle.model },
    { label: 'Derivative', value: vehicle.derivative || 'N/A' },
    { label: 'Body Type', value: vehicle.bodyType || 'N/A' },
    { label: 'Engine Size', value: vehicle.engineSize ? `${vehicle.engineSize}L` : 'N/A' },
    { label: 'Transmission', value: vehicle.transmissionType },
    { label: 'Fuel Type', value: vehicle.fuelType },
    { label: 'Colour', value: vehicle.colour || 'N/A' },
    { label: 'Number of Doors', value: vehicle.numberOfDoors?.toString() || 'N/A' },
    { label: 'Grade', value: vehicle.grade || 'N/A' },
    { label: 'MOT Expiry', value: formatDate(vehicle.motExpiryDate) },
    { label: 'Last Service', value: formatDate(vehicle.lastServiceDate) },
    { label: 'VAT Type', value: vehicle.vatType || 'N/A' },
    { label: 'Sale Type', value: vehicle.saleType || 'N/A' },
    { label: 'Channel', value: vehicle.channel || 'N/A' },
  ];

  return (
    <Card>
      <CardHeader>
        <CardTitle>Technical Specifications</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {specifications.map((spec) => (
            <div key={spec.label} className="border-b pb-2">
              <div className="text-sm text-muted-foreground">{spec.label}</div>
              <div className="font-medium">{spec.value}</div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
