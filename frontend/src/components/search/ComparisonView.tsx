'use client';

import { useState } from 'react';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { X, CheckCircle, XCircle } from 'lucide-react';
import { useComparison } from '@/lib/context/ComparisonContext';
import { ScrollArea } from '@/components/ui/scroll-area';

export function ComparisonView() {
  const { selectedVehicles, toggleVehicle, clearComparison } = useComparison();
  const [open, setOpen] = useState(false);

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

  const getYear = (date?: string) => {
    if (!date) return 'N/A';
    return new Date(date).getFullYear().toString();
  };

  const getBestValue = (attr: 'price' | 'mileage' | 'year') => {
    if (selectedVehicles.length === 0) return null;

    if (attr === 'price') {
      return Math.min(...selectedVehicles.map((v) => v.price));
    } else if (attr === 'mileage') {
      return Math.min(...selectedVehicles.map((v) => v.mileage));
    } else if (attr === 'year') {
      const years = selectedVehicles
        .map((v) => (v.registrationDate ? new Date(v.registrationDate).getFullYear() : 0))
        .filter((y) => y > 0);
      return years.length > 0 ? Math.max(...years) : null;
    }
  };

  const isBestValue = (vehicle: any, attr: 'price' | 'mileage' | 'year') => {
    const best = getBestValue(attr);
    if (!best) return false;

    if (attr === 'price') {
      return vehicle.price === best;
    } else if (attr === 'mileage') {
      return vehicle.mileage === best;
    } else if (attr === 'year') {
      return vehicle.registrationDate && new Date(vehicle.registrationDate).getFullYear() === best;
    }
  };

  if (selectedVehicles.length < 2) {
    return null;
  }

  return (
    <>
      {/* Compare Button - Fixed at bottom */}
      <div className="fixed bottom-4 right-4 z-50">
        <Button size="lg" onClick={() => setOpen(true)} className="shadow-lg">
          Compare ({selectedVehicles.length})
        </Button>
      </div>

      {/* Comparison Sheet */}
      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent side="bottom" className="h-[80vh]">
          <SheetHeader>
            <div className="flex items-center justify-between">
              <SheetTitle>Vehicle Comparison</SheetTitle>
              <Button variant="outline" size="sm" onClick={clearComparison}>
                Clear All
              </Button>
            </div>
          </SheetHeader>

          <ScrollArea className="h-full mt-4">
            <div className="overflow-x-auto">
              <table className="w-full border-collapse" data-testid="comparison-table">
                <thead>
                  <tr className="border-b">
                    <th className="text-left p-3 font-medium">Attribute</th>
                    {selectedVehicles.map((vehicle) => (
                      <th key={vehicle.id} className="p-3 min-w-[200px]">
                        <div className="space-y-2">
                          <div className="font-semibold text-left">
                            {vehicle.make} {vehicle.model}
                          </div>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => toggleVehicle(vehicle)}
                            className="w-full"
                          >
                            <X className="h-4 w-4 mr-1" />
                            Remove
                          </Button>
                        </div>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {/* Price */}
                  <tr className="border-b">
                    <td className="p-3 font-medium">Price</td>
                    {selectedVehicles.map((vehicle) => (
                      <td key={vehicle.id} className="p-3">
                        <div className="flex items-center gap-2">
                          {formatPrice(vehicle.price)}
                          {isBestValue(vehicle, 'price') && (
                            <Badge variant="default" className="bg-green-600">
                              Best
                            </Badge>
                          )}
                        </div>
                      </td>
                    ))}
                  </tr>

                  {/* Mileage */}
                  <tr className="border-b">
                    <td className="p-3 font-medium">Mileage</td>
                    {selectedVehicles.map((vehicle) => (
                      <td key={vehicle.id} className="p-3">
                        <div className="flex items-center gap-2">
                          {formatMileage(vehicle.mileage)} mi
                          {isBestValue(vehicle, 'mileage') && (
                            <Badge variant="default" className="bg-green-600">
                              Best
                            </Badge>
                          )}
                        </div>
                      </td>
                    ))}
                  </tr>

                  {/* Year */}
                  <tr className="border-b">
                    <td className="p-3 font-medium">Year</td>
                    {selectedVehicles.map((vehicle) => (
                      <td key={vehicle.id} className="p-3">
                        <div className="flex items-center gap-2">
                          {getYear(vehicle.registrationDate)}
                          {isBestValue(vehicle, 'year') && (
                            <Badge variant="default" className="bg-green-600">
                              Newest
                            </Badge>
                          )}
                        </div>
                      </td>
                    ))}
                  </tr>

                  {/* Fuel Type */}
                  <tr className="border-b">
                    <td className="p-3 font-medium">Fuel Type</td>
                    {selectedVehicles.map((vehicle) => (
                      <td key={vehicle.id} className="p-3">
                        {vehicle.fuelType}
                      </td>
                    ))}
                  </tr>

                  {/* Transmission */}
                  <tr className="border-b">
                    <td className="p-3 font-medium">Transmission</td>
                    {selectedVehicles.map((vehicle) => (
                      <td key={vehicle.id} className="p-3">
                        {vehicle.transmissionType}
                      </td>
                    ))}
                  </tr>

                  {/* Engine Size */}
                  <tr className="border-b">
                    <td className="p-3 font-medium">Engine Size</td>
                    {selectedVehicles.map((vehicle) => (
                      <td key={vehicle.id} className="p-3">
                        {vehicle.engineSize ? `${vehicle.engineSize}L` : 'N/A'}
                      </td>
                    ))}
                  </tr>

                  {/* Service History */}
                  <tr className="border-b">
                    <td className="p-3 font-medium">Service History</td>
                    {selectedVehicles.map((vehicle) => (
                      <td key={vehicle.id} className="p-3">
                        {vehicle.serviceHistoryPresent ? (
                          <div className="flex items-center gap-1 text-green-600">
                            <CheckCircle className="h-4 w-4" />
                            Full
                          </div>
                        ) : (
                          <div className="flex items-center gap-1 text-muted-foreground">
                            <XCircle className="h-4 w-4" />
                            No
                          </div>
                        )}
                      </td>
                    ))}
                  </tr>

                  {/* Features */}
                  <tr className="border-b">
                    <td className="p-3 font-medium">Features</td>
                    {selectedVehicles.map((vehicle) => (
                      <td key={vehicle.id} className="p-3">
                        {vehicle.features && vehicle.features.length > 0 ? (
                          <div className="flex flex-wrap gap-1">
                            {vehicle.features.slice(0, 3).map((feature, idx) => (
                              <Badge key={idx} variant="outline" className="text-xs">
                                {feature}
                              </Badge>
                            ))}
                            {vehicle.features.length > 3 && (
                              <Badge variant="outline" className="text-xs">
                                +{vehicle.features.length - 3} more
                              </Badge>
                            )}
                          </div>
                        ) : (
                          'N/A'
                        )}
                      </td>
                    ))}
                  </tr>
                </tbody>
              </table>
            </div>
          </ScrollArea>
        </SheetContent>
      </Sheet>
    </>
  );
}
