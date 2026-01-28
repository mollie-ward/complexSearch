'use client';

import { useEffect, useState } from 'react';
import { VehicleDocument } from '@/lib/api/types';
import { getSimilarVehicles } from '@/lib/api/search';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import Link from 'next/link';

interface SimilarVehiclesProps {
  vehicleId: string;
  limit?: number;
}

export function SimilarVehicles({ vehicleId, limit = 3 }: SimilarVehiclesProps) {
  const [vehicles, setVehicles] = useState<VehicleDocument[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchSimilar = async () => {
      try {
        setIsLoading(true);
        const similar = await getSimilarVehicles(vehicleId, limit);
        setVehicles(similar);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load similar vehicles');
      } finally {
        setIsLoading(false);
      }
    };

    fetchSimilar();
  }, [vehicleId, limit]);

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('en-GB', {
      style: 'currency',
      currency: 'GBP',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(price);
  };

  const formatMileage = (mileage: number) => {
    return new Intl.NumberFormat('en-GB').format(mileage);
  };

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Similar Vehicles</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {[1, 2, 3].map((i) => (
            <Skeleton key={i} className="h-24 w-full" />
          ))}
        </CardContent>
      </Card>
    );
  }

  if (error || vehicles.length === 0) {
    return null;
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Similar Vehicles</CardTitle>
        <CardDescription>Based on make, model, and price</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {vehicles.map((vehicle) => (
          <Card key={vehicle.id} className="overflow-hidden">
            <CardContent className="p-4">
              <div className="flex justify-between items-start mb-2">
                <div>
                  <h4 className="font-semibold">
                    {vehicle.make} {vehicle.model}
                  </h4>
                  {vehicle.derivative && (
                    <p className="text-sm text-muted-foreground">{vehicle.derivative}</p>
                  )}
                </div>
                <div className="text-right">
                  <div className="font-bold">{formatPrice(vehicle.price)}</div>
                </div>
              </div>
              <div className="flex gap-2 text-sm text-muted-foreground mb-3">
                <span>{formatMileage(vehicle.mileage)} mi</span>
                <span>•</span>
                <span>{vehicle.transmissionType}</span>
                <span>•</span>
                <span>{vehicle.fuelType}</span>
              </div>
              {vehicle.serviceHistoryPresent && (
                <Badge variant="secondary" className="mb-3">
                  Full Service History
                </Badge>
              )}
              <Link href={`/vehicles/${vehicle.id}`}>
                <Button variant="outline" size="sm" className="w-full">
                  View Details
                </Button>
              </Link>
            </CardContent>
          </Card>
        ))}
      </CardContent>
    </Card>
  );
}
