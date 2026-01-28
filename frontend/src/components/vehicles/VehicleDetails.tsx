'use client';

import { VehicleDocument } from '@/lib/api/types';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle, XCircle } from 'lucide-react';

interface VehicleDetailsProps {
  vehicle: VehicleDocument;
}

export function VehicleDetails({ vehicle }: VehicleDetailsProps) {
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

  const formatDate = (date?: string) => {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-GB', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-3xl">
          {vehicle.make} {vehicle.model}
        </CardTitle>
        {vehicle.derivative && (
          <CardDescription className="text-lg">{vehicle.derivative}</CardDescription>
        )}
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Price Section */}
        <div className="border-b pb-4">
          <div className="text-4xl font-bold text-primary">{formatPrice(vehicle.price)}</div>
          {vehicle.capRetailPrice && (
            <div className="text-sm text-muted-foreground mt-1">
              CAP Retail: {formatPrice(vehicle.capRetailPrice)}
            </div>
          )}
        </div>

        {/* Key Information */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <div className="text-sm text-muted-foreground">Mileage</div>
            <div className="font-semibold">{formatMileage(vehicle.mileage)} miles</div>
          </div>
          <div>
            <div className="text-sm text-muted-foreground">Registration Date</div>
            <div className="font-semibold">{formatDate(vehicle.registrationDate)}</div>
          </div>
          <div>
            <div className="text-sm text-muted-foreground">Transmission</div>
            <div className="font-semibold">{vehicle.transmissionType}</div>
          </div>
          <div>
            <div className="text-sm text-muted-foreground">Fuel Type</div>
            <div className="font-semibold">{vehicle.fuelType}</div>
          </div>
          <div>
            <div className="text-sm text-muted-foreground">Location</div>
            <div className="font-semibold">{vehicle.saleLocation}</div>
          </div>
          {vehicle.colour && (
            <div>
              <div className="text-sm text-muted-foreground">Colour</div>
              <div className="font-semibold">{vehicle.colour}</div>
            </div>
          )}
        </div>

        {/* Service History */}
        {vehicle.serviceHistoryPresent !== undefined && (
          <div className="flex items-center gap-2">
            {vehicle.serviceHistoryPresent ? (
              <>
                <CheckCircle className="h-5 w-5 text-green-600" />
                <span className="font-semibold">Full Service History</span>
                {vehicle.numberOfServices && (
                  <Badge variant="secondary">{vehicle.numberOfServices} services</Badge>
                )}
              </>
            ) : (
              <>
                <XCircle className="h-5 w-5 text-muted-foreground" />
                <span className="text-muted-foreground">No full service history</span>
              </>
            )}
          </div>
        )}

        {/* Features */}
        {vehicle.features && vehicle.features.length > 0 && (
          <div>
            <h3 className="font-semibold mb-2">Features</h3>
            <div className="flex flex-wrap gap-2">
              {vehicle.features.map((feature, index) => (
                <Badge key={`${vehicle.id}-feature-${index}`} variant="outline">
                  {feature}
                </Badge>
              ))}
            </div>
          </div>
        )}

        {/* Description */}
        {vehicle.description && (
          <div>
            <h3 className="font-semibold mb-2">Description</h3>
            <p className="text-muted-foreground">{vehicle.description}</p>
          </div>
        )}

        {/* Additional Information */}
        {vehicle.additionalInfo && (
          <div>
            <h3 className="font-semibold mb-2">Additional Information</h3>
            <p className="text-sm text-muted-foreground">{vehicle.additionalInfo}</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
