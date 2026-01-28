'use client';

import { use, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { getVehicleById } from '@/lib/api/search';
import { VehicleDocument } from '@/lib/api/types';
import { VehicleDetails } from '@/components/vehicles/VehicleDetails';
import { VehicleSpecifications } from '@/components/vehicles/VehicleSpecifications';
import { VehicleImages } from '@/components/vehicles/VehicleImages';
import { SimilarVehicles } from '@/components/vehicles/SimilarVehicles';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { ArrowLeft, AlertCircle } from 'lucide-react';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default function VehicleDetailPage({ params }: PageProps) {
  const router = useRouter();
  const { id } = use(params);
  const [vehicle, setVehicle] = useState<VehicleDocument | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchVehicle = async () => {
      try {
        setIsLoading(true);
        const data = await getVehicleById(id);
        setVehicle(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load vehicle');
      } finally {
        setIsLoading(false);
      }
    };

    fetchVehicle();
  }, [id]);

  if (isLoading) {
    return (
      <main className="container mx-auto px-4 py-8">
        <div className="mb-6">
          <Skeleton className="h-10 w-32" />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <Skeleton className="h-64 w-full" />
            <Skeleton className="h-96 w-full" />
            <Skeleton className="h-96 w-full" />
          </div>
          <div className="lg:col-span-1">
            <Skeleton className="h-96 w-full" />
          </div>
        </div>
      </main>
    );
  }

  if (error || !vehicle) {
    return (
      <main className="container mx-auto px-4 py-8">
        <div className="mb-6">
          <Button variant="ghost" onClick={() => router.back()}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to search
          </Button>
        </div>
        <div className="flex flex-col items-center justify-center py-12">
          <AlertCircle className="h-12 w-12 text-destructive mb-4" />
          <h2 className="text-2xl font-bold mb-2">Vehicle Not Found</h2>
          <p className="text-muted-foreground mb-6">
            {error || 'The vehicle you are looking for does not exist or has been removed.'}
          </p>
          <Button onClick={() => router.push('/search')}>
            Return to Search
          </Button>
        </div>
      </main>
    );
  }

  return (
    <main className="container mx-auto px-4 py-8">
      {/* Back Button */}
      <div className="mb-6">
        <Button variant="ghost" onClick={() => router.back()}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to search
        </Button>
      </div>

      {/* Main Content */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column - Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Images */}
          <VehicleImages 
            vehicleId={vehicle.id} 
            make={vehicle.make} 
            model={vehicle.model} 
          />

          {/* Vehicle Details */}
          <VehicleDetails vehicle={vehicle} />

          {/* Technical Specifications */}
          <VehicleSpecifications vehicle={vehicle} />
        </div>

        {/* Right Column - Sidebar */}
        <div className="lg:col-span-1 space-y-6">
          {/* Similar Vehicles */}
          <SimilarVehicles vehicleId={vehicle.id} limit={3} />
        </div>
      </div>
    </main>
  );
}
