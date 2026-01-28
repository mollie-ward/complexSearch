'use client';

import { Card, CardContent } from '@/components/ui/card';
import { ImageIcon } from 'lucide-react';

interface VehicleImagesProps {
  vehicleId: string;
  make: string;
  model: string;
}

export function VehicleImages({ vehicleId, make, model }: VehicleImagesProps) {
  // Placeholder implementation - images would come from backend
  return (
    <Card>
      <CardContent className="p-0">
        <div className="aspect-video bg-muted flex items-center justify-center rounded-t-lg">
          <div className="text-center">
            <ImageIcon className="h-16 w-16 text-muted-foreground mx-auto mb-2" />
            <p className="text-muted-foreground">
              Image for {make} {model}
            </p>
            <p className="text-sm text-muted-foreground">ID: {vehicleId}</p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
